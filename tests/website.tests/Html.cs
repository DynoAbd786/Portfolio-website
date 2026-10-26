using AngleSharp;
using AngleSharp.Dom;

namespace website.tests;

public static class Html
{
    // Fetches a page, asserts it returned 200 and parses it
    public static async Task<IDocument> GetAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        Assert.True(response.IsSuccessStatusCode, $"{path} returned {(int)response.StatusCode}");
        return await ParseAsync(await response.Content.ReadAsStringAsync());
    }

    public static Task<IDocument> ParseAsync(string html) =>
        BrowsingContext.New(Configuration.Default).OpenAsync(req => req.Content(html));
}
