namespace website.tests;

public class LayoutTests : IClassFixture<SiteFactory>
{
    private readonly HttpClient _client;

    public LayoutTests(SiteFactory factory)
    {
        _client = factory.CreateClient();
    }

    public static TheoryData<string> Pages => new() { "/", "/projects/machine-learning" };

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Nav_has_work_about_contact_and_chat(string path)
    {
        var doc = await Html.GetAsync(_client, path);
        Assert.NotNull(doc.QuerySelector("header a[href=\"/projects\"]"));
        Assert.NotNull(doc.QuerySelector("header a[href=\"/about\"]"));
        Assert.NotNull(doc.QuerySelector("header a[href=\"/#contact\"]"));
        Assert.NotNull(doc.QuerySelector("header button[onclick=\"mkChat.toggle()\"]"));
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Footer_links_ai_integration_and_nothing_to_ethos(string path)
    {
        var doc = await Html.GetAsync(_client, path);
        Assert.Empty(doc.QuerySelectorAll("a[href=\"/ethos\"]"));
        Assert.NotNull(doc.QuerySelector("footer a[href=\"/ai-integration\"]"));
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Chat_root_is_permanent(string path)
    {
        var doc = await Html.GetAsync(_client, path);
        Assert.NotNull(doc.QuerySelector("#chat-root[data-permanent] #chat-panel"));
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Meta_description_no_longer_says_student(string path)
    {
        var doc = await Html.GetAsync(_client, path);
        var description = doc.QuerySelector("meta[name=description]")?.GetAttribute("content");
        Assert.NotNull(description);
        Assert.DoesNotContain("student", description, StringComparison.OrdinalIgnoreCase);
    }
}
