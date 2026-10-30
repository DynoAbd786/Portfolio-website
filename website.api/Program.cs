using website.api.Mcp;
using website.api.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics;
using DotNetEnv;

// Local development keeps its keys in .env (Render sets them as environment variables)
if (File.Exists(".env"))
{
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Containers set ASPNETCORE_URLS (Render: port 10000); local runs default to 5154
if (string.IsNullOrEmpty(builder.Configuration["urls"]))
{
    builder.WebHost.UseUrls("http://0.0.0.0:5154");
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register custom services
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddHttpClient<IEmailService, EmailService>(); // typed client, like DeepSeek below

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ContactLimiter>();
builder.Services.AddHttpClient<Analytics>(); // Umami events only the server sees (typed client)
builder.Services.AddScoped<PortfolioTools>(); // what the MCP server and the assistant can do
builder.Services.AddHttpClient<DeepSeekAgentService>(); // typed client: no AddScoped, which would replace it

// The MCP server at /api/mcp: the official C# SDK, Streamable HTTP, stateless (the SDK's default
// from the 2026-07-28 spec). Tools and resources are in Mcp/PortfolioMcp.cs.
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new() { Name = "mkkai", Title = "Muhammad Kashif-Khan's portfolio", Version = "2.0.0", WebsiteUrl = "https://mkkai.dev" };
    options.ServerInstructions = "Information about Muhammad Kashif-Khan, a software engineer, from his portfolio at mkkai.dev. " +
        "Use search_projects and the portfolio:// resources to answer questions about his work. " +
        "submit_contact emails him: ask the person for their name, email, a subject and the message first.";
})
    .WithHttpTransport()
    .WithTools<PortfolioMcpTools>()
    .WithResources<PortfolioMcpResources>();

// Pages render on the server (static SSR); a few components run in the browser via WebAssembly
// Render's health check (dashboard setting) polls /health
builder.Services.AddHealthChecks();

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddScoped<website.Data.IProjectSource, ApiProjectSource>();
builder.Services.AddScoped<website.Data.IContactSender, EmailContactSender>();
builder.Services.AddScoped<website.Data.ProjectService>();
// Interactive components that inject HttpClient are prerendered here too. They only call it once
// running in the browser, so the default HttpClient that AddHttpClient registers is enough.

// MCP clients may call from any origin (browser-based agents and inspectors)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMcpClients", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure forwarded headers for proxy/CDN scenarios in production
if (builder.Environment.IsProduction())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();

// Configure forwarded headers for proxy scenarios (must be early in pipeline)
app.UseForwardedHeaders();

// Pages and API GET endpoints only match GET, so link checkers and uptime monitors sending
// HEAD would get 405. Run HEAD as GET and drop the body (headers stay as GET would send them).
// Before the status code pages, whose re-executed request does its own routing.
app.Use(async (context, next) =>
{
    if (!HttpMethods.IsHead(context.Request.Method))
    {
        await next();
        return;
    }

    context.Request.Method = HttpMethods.Get;
    var body = context.Response.Body;
    context.Response.Body = Stream.Null;
    try
    {
        await next();
    }
    finally
    {
        context.Response.Body = body;
        context.Request.Method = HttpMethods.Head;
    }
});

// Unknown page URLs render the site's not-found page; API 404s keep their own responses.
// Registered early so the re-executed request runs through auth and antiforgery again.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") &&
        context.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages)
    {
        statusCodePages.Enabled = false;
    }
    await next();
});

// Routing runs after the HEAD rewrite so endpoints are matched as GET
app.UseRouting();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Skip HTTPS redirection - Render handles SSL termination at proxy level
// app.UseHttpsRedirection();

app.UseCors("AllowMcpClients");

// The API may be crawled and used by AI agents (CORS headers come from the policy above)
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
        context.Response.Headers.Append("X-Robots-Tag", "all");
    await next();
});

app.UseAuthorization();
app.UseAntiforgery();

// Fingerprinted assets are cached forever; everything else (e.g. the CV) revalidates via ETag
app.MapStaticAssets();

app.MapHealthChecks("/health");
app.MapMcp("/api/mcp");
// Stateless, so there's no standalone SSE stream to open: the spec says to answer GET with 405 (the
// SDK leaves it unmapped, which would be a 404, and clients such as the MCP Inspector disconnect)
app.MapGet("/api/mcp", (HttpContext context) =>
{
    context.Response.Headers.Allow = "POST, DELETE";
    return Results.StatusCode(StatusCodes.Status405MethodNotAllowed);
});
app.MapControllers();
// Synergy's page moved from its old working title; keep old links (CV, search results) working
app.MapGet("/projects/workflow-graph-platform", () => Results.Redirect("/projects/synergy", permanent: true));
// The Ethos page was folded into a few lines on About ("how I work")
app.MapGet("/ethos", () => Results.Redirect("/about#how-i-work", permanent: true));
// TaskFlow was an experiment and has been taken down; old links land on all projects
app.MapGet("/projects/taskflow", () => Results.Redirect("/projects", permanent: true));
// Short links for the CV and LinkedIn; the tag tells Umami which one a visit came from
app.MapGet("/cv", () => Results.Redirect("/?utm_source=cv"));
app.MapGet("/in", () => Results.Redirect("/?utm_source=linkedin"));

app.MapRazorComponents<website.api.Components.App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(website._Imports).Assembly);

app.Run();
