using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using website;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Enable detailed logging for debugging
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


// Configure HttpClient to use API base address in production, local API in development
var apiBaseAddress = builder.HostEnvironment.IsDevelopment()
    ? "http://localhost:5154/" // API dev server (HTTP to avoid certificate issues)
    : builder.HostEnvironment.BaseAddress; // Same domain in production (API serves both static files and API)

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseAddress) });
builder.Services.AddScoped<website.Data.ProjectService>();

await builder.Build().RunAsync();
