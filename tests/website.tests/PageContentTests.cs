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
        Assert.NotNull(main.QuerySelector("a[href=\"/ethos\"]"));
        Assert.NotNull(main.QuerySelector("a[href=\"/ai-integration\"]"));
        Assert.DoesNotContain("4th year", main.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("student", main.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Predicted", main.TextContent);
    }
}
