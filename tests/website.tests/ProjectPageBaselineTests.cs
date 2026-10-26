using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Microsoft.Extensions.DependencyInjection;
using website.Data;

namespace website.tests;

// Every project page keeps its content when it moves onto the shared template: the baseline is
// what each page had before (section headings, PDF links, images), recorded once.
public class ProjectPageBaselineTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;
    private readonly HttpClient _client;

    public ProjectPageBaselineTests(SiteFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public record PageContent(List<string> Headings, List<string> Pdfs, List<string> Images);

    private static string BaselinePath([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here)!, "ProjectPageBaseline.json");

    private static string Norm(string s) => Regex.Replace(s, @"\s+", " ").Trim();

    // Headings outside the page's own header block (the title and subtitle the template replaces)
    private static PageContent Read(IDocument doc)
    {
        var main = doc.QuerySelector("main")!;
        var header = main.QuerySelector("h1")?.ParentElement;
        var headings = main.QuerySelectorAll("h2, h3")
            .Where(h => header is null || !header.Contains(h))
            .Select(h => Norm(h.TextContent)).Where(t => t.Length > 0).ToList();
        var pdfs = main.QuerySelectorAll("a[href$='.pdf']").Select(a => a.GetAttribute("href")!).ToList();
        var images = main.QuerySelectorAll("img").Select(i => i.GetAttribute("src")!).ToList();
        return new PageContent(headings, pdfs, images);
    }

    public static IEnumerable<object[]> ProjectRoutes() =>
        JsonSerializer.Deserialize<Dictionary<string, PageContent>>(File.ReadAllText(BaselinePath()))!
            .Keys.Select(k => new object[] { k });

    [Fact(Skip = "generator: un-skip once to record the baseline from the untouched pages")]
    public async Task Generate_baseline()
    {
        using var scope = _factory.Services.CreateScope();
        var projects = await scope.ServiceProvider.GetRequiredService<ProjectService>().GetProjectsAsync();
        var baseline = new SortedDictionary<string, PageContent>();
        foreach (var url in projects.Select(p => p.ProjectUrl))
            baseline[url] = Read(await Html.GetAsync(_client, url));
        File.WriteAllText(BaselinePath(), JsonSerializer.Serialize(baseline, new JsonSerializerOptions { WriteIndented = true }));
    }

    [Theory]
    [MemberData(nameof(ProjectRoutes))]
    public async Task Content_is_preserved(string route)
    {
        var expected = JsonSerializer.Deserialize<Dictionary<string, PageContent>>(File.ReadAllText(BaselinePath()))![route];
        var doc = await Html.GetAsync(_client, route);
        var main = doc.QuerySelector("main")!;
        var headings = main.QuerySelectorAll("h1, h2, h3, h4").Select(h => Norm(h.TextContent)).ToHashSet();
        var actual = Read(doc);
        Assert.All(expected.Headings, h => Assert.Contains(h, headings));
        Assert.All(expected.Pdfs, p => Assert.Contains(p, actual.Pdfs));
        Assert.All(expected.Images, i => Assert.Contains(i, actual.Images));
    }
}
