using System.Text.RegularExpressions;

namespace website.tests;

// Pages use the site's tokens (ink, line, cy, lamp, slate text), not the old rainbow colours and gradients
public class StyleTests : IClassFixture<SiteFactory>
{
    private readonly HttpClient _client;

    public StyleTests(SiteFactory factory) => _client = factory.CreateClient();

    private static readonly Regex OldColour = new(
        @"\b(?:[a-z]+:)*(?:text|bg|border|from|via|to|ring)-(?:gray|blue|purple|green|yellow|pink|red|orange|indigo|teal|cyan|emerald|violet|amber|rose|sky|lime|fuchsia)-\d{2,3}(?:/\d+)?(?![\w/-])",
        RegexOptions.Compiled);

    // colours that mean something: form errors and success, "open to work", and a caption naming the
    // blue line in a picture
    private static readonly HashSet<string> Meaningful = new()
    {
        "text-red-400", "bg-red-500/20", "border-red-500", "bg-green-500/20", "border-green-500", "text-green-400",
        "bg-emerald-400", "text-blue-400",
    };

    public static TheoryData<string> Pages()
    {
        var data = new TheoryData<string> { "/", "/about", "/ai-integration", "/projects" };
        foreach (var route in ProjectPageTemplateTests.AllRoutes) data.Add(route);
        return data;
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Page_uses_the_site_colours(string path)
    {
        var doc = await Html.GetAsync(_client, path);
        var old = doc.QuerySelectorAll("main [class]")
            .SelectMany(e => OldColour.Matches(e.GetAttribute("class")!).Select(m => m.Value))
            .Where(c => !Meaningful.Contains(c))
            .Distinct().ToList();
        Assert.True(old.Count == 0, $"{path} uses {string.Join(", ", old)}");
    }
}
