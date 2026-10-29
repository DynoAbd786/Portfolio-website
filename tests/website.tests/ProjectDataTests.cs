using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using website.Data;

namespace website.tests;

public class ProjectDataTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public ProjectDataTests(SiteFactory factory)
    {
        _factory = factory;
    }

    private async Task<List<Project>> ProjectsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ProjectService>().GetProjectsAsync();
    }

    [Fact]
    public async Task Every_project_has_a_field_and_a_result()
    {
        var projects = await ProjectsAsync();
        Assert.Equal(31, projects.Count);
        Assert.All(projects, p =>
        {
            Assert.True(p.Fields.Length >= 1, $"{p.Title} has no field");
            Assert.InRange(p.Result.Length, 1, 140);
        });
    }

    [Fact]
    public async Task Synergy_is_renamed_and_lives_at_its_own_url()
    {
        using var scope = _factory.Services.CreateScope();
        var project = await scope.ServiceProvider.GetRequiredService<ProjectService>()
            .GetProjectByUrlAsync("/projects/synergy");
        Assert.NotNull(project);
        Assert.StartsWith("Synergy", project.Title);
    }

    [Fact]
    public async Task Formula_student_is_not_featured()
    {
        var projects = await ProjectsAsync();
        Assert.False(projects.Single(p => p.ProjectUrl == "/projects/formula-student-ai").IsFeatured);
    }

    [Fact]
    public async Task Next_project_wraps()
    {
        var projects = await ProjectsAsync();
        Assert.Same(projects[1], ProjectService.GetNextProject(projects, projects[0].ProjectUrl));
        Assert.Same(projects[0], ProjectService.GetNextProject(projects, projects[^1].ProjectUrl));
    }

    [Fact]
    public void Field_slugs_round_trip()
    {
        foreach (var field in Enum.GetValues<ProjectField>())
        {
            Assert.Equal(field, ProjectFields.FromSlug(ProjectFields.Slug(field)));
            Assert.False(string.IsNullOrWhiteSpace(ProjectFields.Label(field)));
        }
        Assert.Null(ProjectFields.FromSlug("x"));
        Assert.Null(ProjectFields.FromSlug(null));
    }

    [Theory]
    [InlineData("portfolio://profile")]
    [InlineData("portfolio://about")]
    public async Task Mcp_profile_is_not_a_student(string uri)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/mcp")
        {
            Content = JsonContent.Create(new { jsonrpc = "2.0", id = 1, method = "resources/read", @params = new { uri } }),
        };
        request.Headers.Add("Accept", "application/json, text/event-stream"); // MCP clients must accept both
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        Assert.Contains("\"contents\"", body);
        Assert.DoesNotContain("\"error\":{", body);
        Assert.DoesNotContain("4th Year", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("student", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Api_projects_exposes_fields()
    {
        var client = _factory.CreateClient();
        using var json = JsonDocument.Parse(await client.GetStringAsync("/api/projects"));
        Assert.All(json.RootElement.EnumerateArray(), p =>
            Assert.Equal(JsonValueKind.Array, p.GetProperty("fields").ValueKind));
    }
}
