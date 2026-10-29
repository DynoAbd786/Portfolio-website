namespace website.tests;

public class PageContentTests : IClassFixture<SiteFactory>
{
    private readonly HttpClient _client;

    public PageContentTests(SiteFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task About_contains_the_about_me_text()
    {
        var doc = await Html.GetAsync(_client, "/about");
        var main = doc.QuerySelector("main")!;
        var headings = main.QuerySelectorAll("h1, h2, h3").Select(h => h.TextContent.Trim()).ToList();
        Assert.Contains("Driving Innovation Through Code", headings);
        Assert.Contains("Looking Forward", headings);
        Assert.Empty(main.QuerySelectorAll("a[href=\"/ethos\"]"));
        var how = main.QuerySelector("#how-i-work")!;
        Assert.Contains("pull request", how.TextContent);
        Assert.DoesNotContain("Muslim", main.TextContent);
        Assert.NotNull(main.QuerySelector("a[href=\"/ai-integration\"]"));
        Assert.DoesNotContain("4th year", main.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("student", main.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Predicted", main.TextContent);
    }

    [Fact]
    public async Task Not_found_links_home_and_work()
    {
        var response = await _client.GetAsync("/no-such-page");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        var doc = await Html.ParseAsync(await response.Content.ReadAsStringAsync());
        var main = doc.QuerySelector("main")!;
        Assert.NotNull(main.QuerySelector("h1"));
        Assert.NotNull(main.QuerySelector("a[href=\"/\"]"));
        Assert.NotNull(main.QuerySelector("a[href=\"/projects\"]"));
    }

    [Theory]
    [InlineData("/ai-integration", null)]
    public async Task Ai_integration_renders_in_new_layout(string route, string? heading)
    {
        var doc = await Html.GetAsync(_client, route);
        var main = doc.QuerySelector("main")!;
        Assert.Single(main.QuerySelectorAll("h1"));
        Assert.NotEmpty(main.QuerySelectorAll(".surface"));
        Assert.Empty(main.QuerySelectorAll("[class*=\"bg-gradient-to\"]"));
        if (heading is not null)
            Assert.Contains(heading, main.QuerySelectorAll("h2").Select(h => h.TextContent.Trim()));
    }

    [Fact]
    public async Task Home_lab_status_shows_no_made_up_uptime()
    {
        var doc = await Html.GetAsync(_client, "/projects/home-lab");
        var status = doc.QuerySelector("main .bridge-status")!.TextContent;
        Assert.DoesNotContain("Uptime", status);
        Assert.DoesNotContain("%", status);
    }
}
