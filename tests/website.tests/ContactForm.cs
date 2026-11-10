using AngleSharp.Html.Dom;

namespace website.tests;

// Fills in and sends the homepage contact form the way a browser does: load the page, then post its
// hidden fields (antiforgery token, when the form was shown) with the visible ones filled in by id.
// A person takes a while to write a message, so the clock moves on between loading and sending.
public static class ContactForm
{
    public static Dictionary<string, string> Valid() => new()
    {
        ["name"] = "Ada Lovelace", ["email"] = "ada@example.com", ["subject"] = "Job Opportunity",
        ["message"] = "We'd like to talk about a graduate role.", ["agree"] = "true",
    };

    public static async Task<HttpResponseMessage> PostAsync(HttpClient client, Dictionary<string, string> fields, TestClock? clock = null, TimeSpan? writing = null)
    {
        var page = await Html.ParseAsync(await client.GetStringAsync("/"));
        var form = (IHtmlFormElement)page.QuerySelector("#contact form")!;
        var data = form.QuerySelectorAll("input[type=hidden]").Cast<IHtmlInputElement>().ToDictionary(i => i.Name!, i => i.Value);
        foreach (var (k, v) in fields) data[form.QuerySelector($"#{k}")!.GetAttribute("name")!] = v;
        clock?.Advance(writing ?? TimeSpan.FromSeconds(30));
        return await client.PostAsync(form.GetAttribute("action") is { Length: > 0 } a ? a : "/", new FormUrlEncodedContent(data));
    }
}

// The real time plus however far a test has moved it on
public sealed class TestClock : TimeProvider
{
    private TimeSpan _ahead;
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow + _ahead;
    public void Advance(TimeSpan by) => _ahead += by;
}
