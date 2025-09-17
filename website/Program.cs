using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using website;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


// Configure HttpClient to use API base address in production, local API in development
var apiBaseAddress = builder.HostEnvironment.IsDevelopment()
    ? "http://localhost:7232/" // API dev server (HTTP to avoid certificate issues)
    : builder.HostEnvironment.BaseAddress; // Same domain in production (API serves both static files and API)

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseAddress) });
builder.Services.AddScoped<website.Data.ProjectService>();

await builder.Build().RunAsync();
