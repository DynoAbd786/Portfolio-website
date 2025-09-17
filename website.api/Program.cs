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

app.UseHttpsRedirection();

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
app.MapControllers();

// Fallback routing for SPA (serve index.html for client-side routing)
app.MapFallbackToFile("index.html");

app.Run();
