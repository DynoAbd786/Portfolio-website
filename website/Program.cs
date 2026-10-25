using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// Runs in the browser for the interactive components only (chat, contact form,
// HomeLab status, image gallery). Pages themselves are rendered by website.api.
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The API and the site share an origin, in development and production
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
