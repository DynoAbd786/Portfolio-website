using System.Net;
using System.Text.Json;

namespace website.tests;

public class RouteTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;
    private readonly HttpClient _client;

    public RouteTests(SiteFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public static TheoryData<string> AllPageRoutes => new()
    {
        "/", "/about", "/ai-integration", "/ethos", "/not-found", "/projects",
        "/projects/3d-reconstruction", "/projects/ai-decision-trees", "/projects/ai-search-algorithms",
        "/projects/autonomous-robotics", "/projects/business-plan", "/projects/c-programming",
        "/projects/data-mining", "/projects/distributed-systems", "/projects/formula-student-ai",
        "/projects/graphics-fundamentals", "/projects/home-lab", "/projects/machine-learning",
        "/projects/medical-simulation", "/projects/mental-health-app", "/projects/numerical-computation",
        "/projects/opengl-renderer", "/projects/parallel-computing", "/projects/pnanolocz",
        "/projects/portfolio", "/projects/quantum-dots", "/projects/terratracker",
        "/projects/synergy", "/projects/xv6-operating-system",
        "/projects/advanced-software-engineering", "/projects/deep-learning", "/projects/coevolutionary-boids",
        "/projects/evolutionary-agents", "/projects/kubernetes-cloud-edge", "/projects/climate-data-science",
        "/vascular-blood-flow",
    };

    [Theory]
    [MemberData(nameof(AllPageRoutes))]
    public async Task Page_returns_200(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AllPageRoutes))]
    public async Task Page_has_no_em_dashes(string path)
    {
        var doc = await Html.GetAsync(_client, path);
        Assert.DoesNotContain("\u2014", doc.Body!.TextContent);
    }

    // The 3D reconstruction work was the master's group project, and it reconstructs static scenes
    [Theory]
    [MemberData(nameof(AllPageRoutes))]
    public async Task Page_does_not_call_the_group_project_a_thesis(string path)
    {
        var doc = await Html.GetAsync(_client, path);
        foreach (var script in doc.QuerySelectorAll("script").ToList()) script.Remove();
        var text = doc.Body!.TextContent;
        Assert.DoesNotMatch(@"(?i)\bthesis\b", text);
        Assert.DoesNotMatch(@"\b4D\b", text);
    }

    [Fact]
    public async Task Removed_taskflow_page_redirects_to_all_projects()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/projects/taskflow");
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/projects", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Old_synergy_url_redirects_permanently()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/projects/workflow-graph-platform");
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/projects/synergy", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Unknown_page_returns_404_with_not_found_page()
    {
        var response = await _client.GetAsync("/nope");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("nothing at this address", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Api_and_health_still_respond()
    {
        var projects = await _client.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.OK, projects.StatusCode);
        using var json = JsonDocument.Parse(await projects.Content.ReadAsStringAsync());
        Assert.Equal(31, json.RootElement.GetArrayLength());

        var health = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal("Healthy", await health.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cv_pdf_is_served()
    {
        var response = await _client.GetAsync("/docs/Muhammad-Kashif-Khan-CV.pdf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    }
}
