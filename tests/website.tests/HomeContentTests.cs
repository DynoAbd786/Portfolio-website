using System.Net;
using Microsoft.Extensions.DependencyInjection;
using website.Data;

namespace website.tests;

public class HomeContentTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public HomeContentTests(SiteFactory factory)
    {
        _factory = factory;
    }

    // Every string the homepage shows outside the project data
    private static IEnumerable<string> AllCopy() =>
        new[] { HomeContent.WorkshopJson() }
            .Concat(HomeContent.Proof.SelectMany(p => new[] { p.Value, p.Label }))
            .Concat(HomeContent.SelectedWork.Select(w => w.FieldLabel))
            .Concat(HomeContent.Products.SelectMany(p => p.Tags.Append(p.Name).Append(p.Summary)))
            .Concat(HomeContent.Experience.SelectMany(e => new[] { e.When, e.Title, e.Org, e.Detail }));

    [Fact]
    public async Task Selected_work_points_at_real_projects()
    {
        using var scope = _factory.Services.CreateScope();
        var projects = await scope.ServiceProvider.GetRequiredService<ProjectService>().GetProjectsAsync();
        Assert.Equal(5, HomeContent.SelectedWork.Count);
        Assert.All(HomeContent.SelectedWork, w => Assert.Contains(projects, p => p.ProjectUrl == w.ProjectUrl));
    }

    [Fact]
    public void Workshop_has_the_eleven_hotspots_in_order()
    {
        Assert.Equal(
            new[] { "synergy", "hireforge", "thesis", "stent", "afm", "homelab", "azure", "whiteboard", "certificate", "bookcase", "robot" },
            HomeContent.Workshop.Select(h => h.Key));
        Assert.Equal("assistant", HomeContent.Workshop.Single(h => h.Key == "robot").Action);
        Assert.All(HomeContent.Workshop.Where(h => h.Key != "robot"), h => Assert.Equal("link", h.Action));
    }

    [Fact]
    public async Task Workshop_links_resolve()
    {
        var client = _factory.CreateClient();
        foreach (var hotspot in HomeContent.Workshop.Where(h => h.Href.StartsWith('/')))
        {
            var response = await client.GetAsync(hotspot.Href);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{hotspot.Key} → {hotspot.Href} returned {(int)response.StatusCode}");
        }
    }

    [Fact]
    public void Nothing_mentions_formula_student()
    {
        Assert.All(AllCopy(), s =>
        {
            Assert.DoesNotContain("formula", s, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("fs-ai", s, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void No_sales_language()
    {
        Assert.All(AllCopy(), s =>
        {
            foreach (var word in new[] { "pricing", "sign up", "waitlist", "founder" })
                Assert.DoesNotContain(word, s, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Workshop_json_is_camel_case()
    {
        var json = HomeContent.WorkshopJson();
        Assert.Contains("\"key\":\"synergy\"", json);
        Assert.Contains("\"pinX\":", json);
    }

    [Fact]
    public void Proof_counts_each_piece_of_work_once()
    {
        // pNanoLocz has its own fact, so no fact also counts it among "internships"
        Assert.DoesNotContain(HomeContent.Proof, p => (p.Value + " " + p.Label).Contains("internships"));
        Assert.Contains(HomeContent.Proof, p => p.Value == "Stent simulation");
    }

    [Fact]
    public async Task Homepage_and_project_copy_has_no_em_dashes()
    {
        Assert.All(AllCopy(), text => Assert.DoesNotContain("\u2014", text));
        using var scope = _factory.Services.CreateScope();
        var projects = await scope.ServiceProvider.GetRequiredService<ProjectService>().GetProjectsAsync();
        Assert.All(projects, p => Assert.DoesNotContain("\u2014", p.Title + p.Description + p.Result));
        var html = await _factory.CreateClient().GetStringAsync("/");
        var doc = await Html.ParseAsync(html);
        Assert.DoesNotContain("\u2014", doc.QuerySelector("main")!.TextContent);
    }
}
