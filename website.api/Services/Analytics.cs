using System.Net.Http.Json;

namespace website.api.Services;

// Umami, the site's cookieless analytics. The page script (App.razor) counts visits and what visitors
// do in the browser; this sends the events only the server sees (contact messages, MCP and assistant
// tool use) to Umami's API. Only on the live site (Production) or with Umami:Enabled; sending runs in
// the background, so a slow or failing Umami never holds up a request.
public class Analytics
{
    private const string Endpoint = "https://gateway.umami.is/api/send";
    // Umami ignores requests whose User-Agent looks like a bot, so server events present as a browser
    private const string UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36";
    private const int MaxText = 500; // Umami's limit for a string value

    private readonly HttpClient _http;
    private readonly ILogger<Analytics> _logger;
    private readonly string? _websiteId;
    private readonly bool _enabled;

    public Analytics(HttpClient http, IConfiguration config, IWebHostEnvironment env, ILogger<Analytics> logger)
    {
        _http = http;
        _logger = logger;
        _websiteId = config["Umami:WebsiteId"];
        _enabled = !string.IsNullOrEmpty(_websiteId) && (env.IsProduction() || config.GetValue<bool>("Umami:Enabled"));
    }

    public void Track(string name, IReadOnlyDictionary<string, string?> data, string url = "/")
    {
        if (!_enabled) return;
        var values = data.Where(kv => !string.IsNullOrEmpty(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value!.Length > MaxText ? kv.Value[..MaxText] : kv.Value);
        _ = SendAsync(new { type = "event", payload = new { website = _websiteId, hostname = "mkkai.dev", url, name, data = values } });
    }

    private async Task SendAsync(object body)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = JsonContent.Create(body) };
            request.Headers.UserAgent.ParseAdd(UserAgent);
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Umami refused an event: {Status}", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Couldn't send an event to Umami");
        }
    }
}
