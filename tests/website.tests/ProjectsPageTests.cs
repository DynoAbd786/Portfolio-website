using Microsoft.Extensions.DependencyInjection;
using website.Data;

namespace website.tests;

public class ProjectsPageTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;
    private readonly HttpClient _client;

    public ProjectsPageTests(SiteFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<List<Project>> Projects()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ProjectService>().GetProjectsAsync();
    }

    private async Task<List<string>> CardLinks(string url)
    {
        var doc = await Html.GetAsync(_client, url);
        return doc.QuerySelectorAll("main .project-card a[href]").Select(a => a.GetAttribute("href")!).ToList();
    }

    [Fact]
    public async Task All_projects_listed_by_default()
    {
        Assert.Equal(24, (await CardLinks("/projects")).Count);
    }

    [Fact]
    public async Task Field_filter()
    {
        var expected = (await Projects()).Where(p => p.Fields.Contains(ProjectField.CloudDistributed)).Select(p => p.ProjectUrl);
        var cards = await CardLinks("/projects?field=cloud-distributed");
        Assert.NotEmpty(cards);
        Assert.Equal(expected.ToHashSet(), cards.ToHashSet());
        Assert.Equal(cards.Count, cards.Distinct().Count());
    }

    [Fact]
    public async Task Category_filter()
    {
        Assert.Equal(2, (await CardLinks("/projects?category=professional")).Count);
    }

    [Fact]
    public async Task Combined_filters()
    {
        var expected = (await Projects())
            .Where(p => p.Category == ProjectCategory.Academic && p.Fields.Contains(ProjectField.AiMl))
            .Select(p => p.ProjectUrl);
        var cards = await CardLinks("/projects?field=ai-ml&category=academic");
        Assert.NotEmpty(cards);
        Assert.Equal(expected.ToHashSet(), cards.ToHashSet());
    }

    [Fact]
    public async Task Unknown_filter_is_ignored()
    {
        Assert.Equal(24, (await CardLinks("/projects?field=zzz")).Count);
    }

    [Fact]
    public async Task Formula_student_is_in_the_grid()
    {
        Assert.Contains("/projects/formula-student-ai", await CardLinks("/projects"));
    }

    [Fact]
    public async Task Current_filter_is_marked_and_chips_are_links()
    {
        var doc = await Html.GetAsync(_client, "/projects?field=ai-ml");
        var current = doc.QuerySelectorAll("main a[aria-current=\"true\"]").Select(a => a.TextContent.Trim()).ToList();
        Assert.Contains("AI & ML", current);
        Assert.NotNull(doc.QuerySelector("main a[href=\"/projects?field=cloud-distributed\"]"));
        Assert.NotNull(doc.QuerySelector("main a[href=\"/projects?field=ai-ml&category=academic\"]"));
    }

    [Fact]
    public async Task No_match_shows_empty_state()
    {
        var projects = await Projects();
        var pair = Enum.GetValues<ProjectField>().SelectMany(f => Enum.GetValues<ProjectCategory>().Select(c => (f, c)))
            .Where(fc => !projects.Any(p => p.Category == fc.c && p.Fields.Contains(fc.f)))
            .Select(fc => ((ProjectField f, ProjectCategory c)?)fc).FirstOrDefault();
        Assert.True(pair.HasValue, "every field/category pair has a project; nothing to test");
        var (field, category) = pair.Value;
        var doc = await Html.GetAsync(_client, $"/projects?field={ProjectFields.Slug(field)}&category={category.ToString().ToLowerInvariant()}");
        Assert.Empty(doc.QuerySelectorAll("main .project-card"));
        Assert.NotNull(doc.QuerySelector("main .projects-empty a[href=\"/projects\"]"));
    }

    [Fact]
    public async Task Grid_leads_with_the_homepage_selected_work()
    {
        var cards = await CardLinks("/projects");
        Assert.Equal(HomeContent.SelectedWork.Select(w => w.ProjectUrl), cards.Take(HomeContent.SelectedWork.Count));
    }

    [Fact]
    public async Task Cards_show_the_filtered_field()
    {
        var doc = await Html.GetAsync(_client, "/projects?field=ai-ml");
        Assert.All(doc.QuerySelectorAll("main .project-card .mono-label"), label => Assert.Equal("// AI & ML", label.TextContent.Trim()));
    }
}
