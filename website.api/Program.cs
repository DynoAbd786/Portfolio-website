using website.api.Services;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.HttpOverrides;
using DotNetEnv;

// Load .env file for local development
if (File.Exists(".env"))
{
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register custom services
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IMcpService, McpService>();

// Configure CORS for Blazor WebAssembly
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWasm", policy =>
    {
        policy.WithOrigins("http://localhost:5195", "https://localhost:7112")
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
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();

// Configure forwarded headers for proxy scenarios (must be early in pipeline)
app.UseForwardedHeaders();

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

app.UseAuthorization();


// Configure static file serving for production
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Allow static files to be served regardless of Host header
        // This fixes subdomain issues like mcp.mkkai.dev
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
    }
});

// Map API controllers BEFORE fallback routing
app.MapControllers();

// SPA fallback routing - serve index.html for non-API routes (production only)
app.MapFallbackToFile("index.html");

app.Run();
