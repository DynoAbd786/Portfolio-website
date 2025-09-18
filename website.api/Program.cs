using website.api.Services;
using Microsoft.AspNetCore.StaticFiles;
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

// Static files are configured in the pipeline, not in services

var app = builder.Build();

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

// Configure static files with proper MIME types for Blazor WebAssembly
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".dat"] = "application/octet-stream";
provider.Mappings[".wasm"] = "application/wasm";
provider.Mappings[".blat"] = "application/octet-stream";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});
app.UseDefaultFiles();

app.UseAuthorization();

// Add request logging middleware for API paths
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("=== API REQUEST RECEIVED ===");
        logger.LogInformation("Method: {Method}", context.Request.Method);
        logger.LogInformation("Path: {Path}", context.Request.Path);
        logger.LogInformation("QueryString: {QueryString}", context.Request.QueryString);
        logger.LogInformation("ContentType: {ContentType}", context.Request.ContentType);
        logger.LogInformation("ContentLength: {ContentLength}", context.Request.ContentLength);
        logger.LogInformation("UserAgent: {UserAgent}", context.Request.Headers.UserAgent);
        logger.LogInformation("Host: {Host}", context.Request.Headers.Host);
        logger.LogInformation("=== FORWARDING TO CONTROLLER ===");
    }

    await next();
});

// Map API controllers BEFORE fallback routing
app.MapControllers();

// Fallback routing for SPA - only for non-API routes
app.MapWhen(context => !context.Request.Path.StartsWithSegments("/api"),
    appBuilder => appBuilder.Run(async context =>
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync("wwwroot/index.html");
    }));

app.Run();
