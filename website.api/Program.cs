using website.api.Services;
using website.api.Middleware;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using DotNetEnv;

// Load .env file for local development
// Load .env file for local development
if (File.Exists(".env"))
{
    Console.WriteLine("Loading .env file...");
    Env.Load();
    var token = Env.GetString("DISCORD_BOT_TOKEN");
    var channel = Env.GetString("DISCORD_CHANNEL_ID");
    Console.WriteLine($"Loaded .env file. Token starts with: {(token?.Length > 5 ? token.Substring(0, 5) : "null")}, Channel: {channel}");
}
else
{
    Console.WriteLine("WARNING: .env file not found!");
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
builder.Services.AddSingleton<IMcpService, McpService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();
builder.Services.AddHttpClient<DeepSeekAgentService>(); // typed client: no AddScoped, which would replace it
builder.Services.AddHttpClient<DiscordService>();
builder.Services.AddSingleton<IDiscordService, DiscordService>();
builder.Services.AddHttpClient<OllamaService>();
builder.Services.AddScoped<OllamaService>();
builder.Services.AddSingleton<IAccessStateService, AccessStateService>();

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

// Configure JWT Bearer authentication for OAuth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // JWT configuration will be handled by our custom OAuth service
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                // Skip default JWT challenge - we handle OAuth challenges in middleware
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });

// Configure CORS for Blazor WebAssembly
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWasm", policy =>
    {
        policy.WithOrigins("http://localhost:5195", "https://localhost:7112", "http://100.88.183.40:5195")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    // CORS policy for MCP clients
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

// Apply MCP CORS policy globally since this is a dedicated MCP server
app.UseCors("AllowMcpClients");

// Add comprehensive request logging for MCP debugging
app.UseRequestLogging(); // Custom request logging middleware

// Add additional headers for AI bot accessibility
app.Use(async (context, next) =>
{
    // Add headers to help AI bots and ensure API accessibility
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers.Append("X-Robots-Tag", "all");
        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        context.Response.Headers.Append("Access-Control-Allow-Headers", "*");
        context.Response.Headers.Append("Access-Control-Max-Age", "86400");
    }
    await next();
});

// Use authentication and our custom MCP OAuth middleware
app.UseAuthentication();
app.UseMcpOAuth(); // Custom OAuth middleware for MCP
app.UseAuthorization();
app.UseAntiforgery();

// Fingerprinted assets are cached forever; everything else (e.g. the CV) revalidates via ETag
app.MapStaticAssets();

app.MapHealthChecks("/health");
app.MapControllers();
// Synergy's page moved from its old working title; keep old links (CV, search results) working
app.MapGet("/projects/workflow-graph-platform", () => Results.Redirect("/projects/synergy", permanent: true));
// The Ethos page was folded into a few lines on About ("how I work")
app.MapGet("/ethos", () => Results.Redirect("/about#how-i-work", permanent: true));
// TaskFlow was an experiment and has been taken down; old links land on all projects
app.MapGet("/projects/taskflow", () => Results.Redirect("/projects", permanent: true));

app.MapRazorComponents<website.api.Components.App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(website._Imports).Assembly);

app.Run();
