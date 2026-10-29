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

    // Every project page, all on the shared template
    private static readonly string[] Routes =
    {
        "/projects/machine-learning", "/projects/medical-simulation",
        "/projects/3d-reconstruction", "/projects/pnanolocz", "/projects/distributed-systems",
        "/projects/home-lab", "/projects/synergy", "/projects/formula-student-ai",
        "/projects/data-mining", "/projects/numerical-computation", "/projects/mental-health-app",
        "/projects/terratracker", "/projects/business-plan", "/vascular-blood-flow",
        "/projects/ai-decision-trees", "/projects/ai-search-algorithms",
        "/projects/parallel-computing", "/projects/autonomous-robotics", "/projects/opengl-renderer",
        "/projects/xv6-operating-system", "/projects/quantum-dots", "/projects/c-programming",
        "/projects/graphics-fundamentals", "/projects/portfolio",
    };

    public static TheoryData<string> MigratedRoutes => new(Routes);

    [Fact]
    public async Task All_project_routes_use_template()
    {
        using var scope = _factory.Services.CreateScope();
        var projects = await scope.ServiceProvider.GetRequiredService<ProjectService>().GetProjectsAsync();
        Assert.Equal(projects.Select(p => p.ProjectUrl).ToHashSet(), Routes.ToHashSet());
    }

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
    public async Task Synergy_page_describes_it_as_it_is_today()
    {
        var doc = await Html.GetAsync(_client, "/projects/synergy");
        var sections = doc.QuerySelectorAll("main section.project-section > div h2").Select(h => h.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "What it does", "Using it with AI coding agents", "How it works", "Where it's at" }, sections.Take(4));
        // the original design is kept, folded away at the end
        var history = doc.QuerySelector("main details.synergy-history");
        Assert.NotNull(history);
        Assert.Contains("How it started", history!.QuerySelector("summary")!.TextContent);
        Assert.Null(history.GetAttribute("open"));
        var text = doc.QuerySelector("main")!.TextContent;
        Assert.Contains("21 tools", text);
        Assert.DoesNotContain("pricing", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("C# to Python", text);
        Assert.DoesNotContain("\u2014", doc.QuerySelector("main header")!.TextContent + string.Join("", sections));
    }
}
