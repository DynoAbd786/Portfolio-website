using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using website.Data;

namespace website.tests;

public class ProjectPageTemplateTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;
    private readonly HttpClient _client;

    public ProjectPageTemplateTests(SiteFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // Project pages already on the shared template
    public static TheoryData<string> MigratedRoutes => new()
    {
        "/projects/machine-learning", "/projects/medical-simulation",
        "/projects/3d-reconstruction", "/projects/pnanolocz", "/projects/distributed-systems",
        "/projects/home-lab", "/projects/workflow-graph-platform", "/projects/formula-student-ai",
    };

    [Theory]
    [MemberData(nameof(MigratedRoutes))]
    public async Task Uses_template(string route)
    {
        using var scope = _factory.Services.CreateScope();
        var project = await scope.ServiceProvider.GetRequiredService<ProjectService>().GetProjectByUrlAsync(route);
        var doc = await Html.GetAsync(_client, route);
        Assert.NotNull(doc.QuerySelector("main a.next-project"));
        Assert.Equal(project!.Title, Regex.Replace(doc.QuerySelector("main h1")!.TextContent, @"\s+", " ").Trim());
        Assert.Single(doc.QuerySelectorAll("main h1"));
    }

    [Fact]
    public async Task Synergy_page_leads_with_current_status()
    {
        var doc = await Html.GetAsync(_client, "/projects/workflow-graph-platform");
        var first = doc.QuerySelector("main section.project-section h2");
        Assert.Equal("Current status", first?.TextContent.Trim());
        var text = doc.QuerySelector("main")!.TextContent;
        Assert.Contains("How it started", text);
        Assert.DoesNotContain("pricing", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("C# to Python", text);
    }
}
