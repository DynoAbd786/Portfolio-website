using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using website.Data;

namespace website.tests;

// The sitemap search engines read lists exactly the pages that exist
public class SitemapTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public SitemapTests(SiteFactory factory) => _factory = factory;

    [Fact]
    public async Task Sitemap_lists_every_page_and_nothing_else()
    {
        using var scope = _factory.Services.CreateScope();
        var projects = await scope.ServiceProvider.GetRequiredService<ProjectService>().GetProjectsAsync();
        var expected = new[] { "/", "/about", "/projects", "/ethos", "/ai-integration" }
            .Concat(projects.Select(p => p.ProjectUrl)).ToHashSet();

        var xml = XDocument.Parse(await _factory.CreateClient().GetStringAsync("/sitemap.xml"));
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var listed = xml.Descendants(ns + "loc").Select(l => new Uri(l.Value).AbsolutePath).ToList();

        Assert.Equal(listed.Count, listed.Distinct().Count());
        Assert.Equal(expected.OrderBy(u => u), listed.OrderBy(u => u));
    }
}
