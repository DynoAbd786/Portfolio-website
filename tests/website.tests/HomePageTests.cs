using System.Text.Json;
using AngleSharp.Dom;

namespace website.tests;

public class HomePageTests : IClassFixture<SiteFactory>
{
    private readonly HttpClient _client;

    public HomePageTests(SiteFactory factory)
    {
        _client = factory.CreateClient();
    }

    private Task<IDocument> HomeAsync() => Html.GetAsync(_client, "/");

    [Fact]
    public async Task Hero_has_name_ctas_and_status()
    {
        var doc = await HomeAsync();
        Assert.Equal("Muhammad Kashif-Khan", System.Text.RegularExpressions.Regex.Replace(doc.QuerySelector("h1")!.TextContent, @"\s+", " ").Trim());
        Assert.NotNull(doc.QuerySelector("main a[href=\"#work\"]"));
        Assert.NotNull(doc.QuerySelector("main a[href^=\"/docs/Muhammad-Kashif-Khan-CV.pdf\"]"));
        Assert.Contains("Open to graduate software & AI roles", doc.QuerySelector("main")!.TextContent);
    }

    [Fact]
    public async Task Hero_buttons_are_see_my_work_cv_then_get_in_touch()
    {
        var doc = await HomeAsync();
        var buttons = doc.QuerySelectorAll(".hero-buttons a").ToList();
        Assert.Equal(new[] { "See my work", "Download CV", "Get in touch" }, buttons.Select(b => b.TextContent.Trim()));
        Assert.Equal("#work", buttons[0].GetAttribute("href"));
        Assert.StartsWith("/docs/Muhammad-Kashif-Khan-CV.pdf", buttons[1].GetAttribute("href"));
        Assert.Equal("#contact", buttons[2].GetAttribute("href"));
        Assert.Contains("btn-primary", buttons[2].ClassList);
        Assert.Contains("btn-shine", buttons[2].ClassList);
        Assert.All(buttons.Take(2), b => Assert.Contains("btn-ghost", b.ClassList));
        Assert.NotNull(doc.QuerySelector("#hero .hero-head h1"));
        Assert.NotNull(doc.QuerySelector("#hero canvas.hero-namefx"));
        Assert.NotNull(doc.QuerySelector(".hero-cover #work"));
    }

    [Fact]
    public async Task Stack_strip_shows_four_groups()
    {
        var doc = await HomeAsync();
        var groups = doc.QuerySelectorAll("#stack dl > div");
        Assert.Equal(new[] { "// languages", "// ai & vision", "// web & cloud", "// systems & hpc" },
            groups.Select(g => g.QuerySelector("dt")!.TextContent.Trim()));
        Assert.Contains("Python", groups[0].QuerySelectorAll("li").Select(li => li.TextContent));
    }

    [Fact]
    public async Task Sections_in_order()
    {
        var doc = await HomeAsync();
        var ids = doc.QuerySelectorAll("main section[id]").Select(s => s.Id).ToList();
        Assert.Equal(new[] { "stack", "work", "building", "experience", "contact" }, ids);
    }

    [Fact]
    public async Task Selected_work_has_five_cards_linking_to_projects()
    {
        var doc = await HomeAsync();
        var links = doc.QuerySelectorAll("#work article a[href^=\"/projects/\"]").Select(a => a.GetAttribute("href")).ToList();
        Assert.Equal(new[]
        {
            "/projects/medical-simulation", "/projects/pnanolocz", "/projects/3d-reconstruction",
            "/projects/distributed-systems", "/projects/home-lab",
        }, links);
        Assert.NotNull(doc.QuerySelector("#work a[href=\"/projects\"]"));
    }

    [Fact]
    public async Task Workshop_data_is_valid_json_with_eleven_hotspots()
    {
        var doc = await HomeAsync();
        var script = doc.QuerySelector("#workshop script#workshop-data[type=\"application/json\"]");
        Assert.NotNull(script);
        using var json = JsonDocument.Parse(script.TextContent);
        Assert.Equal(11, json.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Every_hotspot_has_a_pin()
    {
        var doc = await HomeAsync();
        Assert.Equal(11, doc.QuerySelectorAll("#workshop .workshop-pin").Length);
        Assert.NotNull(doc.QuerySelector("#workshop button.workshop-pin[onclick=\"mkChat.toggle()\"]"));
    }

    [Fact]
    public async Task Homepage_never_mentions_formula_student()
    {
        var doc = await HomeAsync();
        Assert.DoesNotContain("Formula", doc.QuerySelector("main")!.TextContent);
    }

    [Fact]
    public async Task Building_cards_link_to_their_pages()
    {
        var doc = await HomeAsync();
        var cards = doc.QuerySelectorAll("#building article");
        Assert.Equal(2, cards.Length);
        var hireforge = cards.Single(c => c.TextContent.Contains("HireForge"));
        Assert.NotNull(hireforge.QuerySelector("a[href=\"/projects/hireforge\"]"));
        var synergy = cards.Single(c => c.TextContent.Contains("Synergy"));
        Assert.NotNull(synergy.QuerySelector("a[href=\"/projects/synergy\"]"));
    }
}
