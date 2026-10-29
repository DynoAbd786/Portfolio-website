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
        Assert.Contains("What I work with", headings);
        Assert.Contains("Education", headings);
        foreach (var stale in new[] { "Driving Innovation", "Arsenal", "leverag", "passion", "cutting-edge", "GPT-4", "Ollama" })
            Assert.DoesNotContain(stale, main.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(main.QuerySelector("a[href=\"/projects/3d-reconstruction\"]"));
        Assert.NotNull(main.QuerySelector("a[href=\"/projects/medical-simulation\"]"));
        Assert.NotNull(main.QuerySelector("a[href=\"/projects/pnanolocz\"]"));
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

    [Fact]
    public async Task Ai_integration_says_only_what_is_true_now()
    {
        var doc = await Html.GetAsync(_client, "/ai-integration");
        var text = doc.QuerySelector("main")!.TextContent;
        foreach (var stale in new[] { "Local LLM", "LM Studio", "Fully Operational", "100%", "Claude Desktop: Testing", "GPT-4" })
            Assert.DoesNotContain(stale, text);
        Assert.DoesNotMatch(@"[\u2705\u23F3]|\uD83D[\uDE80\uDEE1\uDCCB\uDCC1\uDD27\uDDEA\uDD16]|\uD83E\uDDEA", text); // no emoji ticks
        foreach (var fact in new[] { "https://mkkai.dev/api/mcp", "search_projects", "submit_contact", "portfolio://projects/{id}", "DeepSeek", "official MCP C# SDK", "read-only" })
            Assert.Contains(fact, text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/api/mcp/sse", text); // the bridge and the old SSE transport are gone
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
    public async Task Home_lab_has_no_live_status_panel()
    {
        var doc = await Html.GetAsync(_client, "/projects/home-lab");
        Assert.Null(doc.QuerySelector("main .bridge-status"));
        Assert.DoesNotContain("System Status", doc.QuerySelector("main")!.TextContent);
    }
}
