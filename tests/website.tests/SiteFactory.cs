using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using website.api.Services;

namespace website.tests;

// Runs the real host (pages, API, static assets) in memory
public class SiteFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(s => s.AddSingleton<IMailDomains, MailDomainsStandIn>());
    }
}

// Real DNS is slow, needs the network, and says example.com takes no mail, which every test address
// uses. In memory, only .invalid domains (which never exist) have no mail server.
public sealed class MailDomainsStandIn : IMailDomains
{
    public Task<bool> AcceptsMailAsync(string domain) =>
        Task.FromResult(!domain.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase));
}
