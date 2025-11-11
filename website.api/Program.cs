using website.api.Services;
using website.api.Middleware;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
builder.Services.AddScoped<IOAuthService, OAuthService>();

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
