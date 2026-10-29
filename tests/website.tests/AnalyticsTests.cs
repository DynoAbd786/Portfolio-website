using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using website.api.Models;
using website.api.Services;

namespace website.tests;

// Umami (cookieless analytics): the script only on the live site, and the events only the server
// sees (contact messages, MCP use) sent to Umami's API. Umami is faked; nothing leaves the test.
public class AnalyticsTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public AnalyticsTests(SiteFactory factory) => _factory = factory;

    private sealed class FakeUmami : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, JsonElement Body)> Sent { get; } = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct)).RootElement.Clone();
            lock (Sent) Sent.Add((request, body));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
        }

        // events are sent in the background so a slow Umami never slows a request
        public async Task<JsonElement> WaitFor(string name)
        {
            for (var i = 0; i < 100; i++)
            {
                lock (Sent)
                {
                    var hit = Sent.Select(s => s.Body.GetProperty("payload")).FirstOrDefault(p => p.GetProperty("name").GetString() == name);
                    if (hit.ValueKind != JsonValueKind.Undefined) return hit;
                }
                await Task.Delay(50);
            }
            throw new Exception($"no '{name}' event was sent; sent: {string.Join(", ", Sent.Select(s => s.Body.GetProperty("payload").GetProperty("name").GetString()))}");
        }
    }

    private sealed class FakeEmail : IEmailService
    {
        public Task<bool> SendContactEmailAsync(ContactRequest request) => Task.FromResult(true);
    }

    private (HttpClient Http, FakeUmami Umami) Site()
    {
        var umami = new FakeUmami();
        var http = _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["Umami:Enabled"] = "true" }));
            b.ConfigureTestServices(s =>
            {
                s.AddHttpClient<Analytics>().ConfigurePrimaryHttpMessageHandler(() => umami);
                s.AddSingleton<IEmailService>(new FakeEmail());
            });
        }).CreateClient();
        return (http, umami);
    }

    [Fact]
    public async Task The_script_loads_only_on_the_live_site_and_never_in_the_chat_frame()
    {
        var live = _factory.WithWebHostBuilder(b => b.UseEnvironment("Production")).CreateClient();
        var home = await live.GetStringAsync("/");
        Assert.Contains("src=\"https://cloud.umami.is/script.js\"", home);
        Assert.Contains("data-website-id=\"6313f336-b4d7-4255-8e4a-032648a0d043\"", home);
        Assert.Contains("data-domains=\"mkkai.dev,www.mkkai.dev\"", home);
        Assert.Contains("data-exclude-hash=\"true\"", home);
        Assert.DoesNotContain("cloud.umami.is", await live.GetStringAsync("/chat-panel"));
        // local runs and the test suites never count as visits
        Assert.DoesNotContain("cloud.umami.is", await _factory.CreateClient().GetStringAsync("/"));
    }

    [Fact]
    public async Task The_footer_says_what_is_counted()
    {
        var doc = await Html.GetAsync(_factory.CreateClient(), "/projects");
        var note = doc.QuerySelector("footer .privacy-note")!.TextContent;
        Assert.Contains("no cookies", note);
        Assert.Contains("Ask AI", note);
    }

    [Fact]
    public async Task Mcp_tool_calls_and_resource_reads_are_counted_with_the_client()
    {
        var (http, umami) = Site();
        await using var mcp = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(http.BaseAddress!, "/api/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
        }, http, ownsHttpClient: false));
        await mcp.CallToolAsync("search_projects", new Dictionary<string, object?> { ["keyword"] = "HireForge" });
        var tool = await umami.WaitFor("mcp-tool");
        Assert.Equal("6313f336-b4d7-4255-8e4a-032648a0d043", tool.GetProperty("website").GetString());
        Assert.Equal("mkkai.dev", tool.GetProperty("hostname").GetString());
        Assert.Equal("search_projects", tool.GetProperty("data").GetProperty("tool").GetString());
        Assert.Equal("HireForge", tool.GetProperty("data").GetProperty("keyword").GetString());
        Assert.False(string.IsNullOrEmpty(tool.GetProperty("data").GetProperty("client").GetString()));

        await mcp.ReadResourceAsync("portfolio://profile");
        var read = await umami.WaitFor("mcp-resource");
        Assert.Equal("portfolio://profile", read.GetProperty("data").GetProperty("uri").GetString());
    }

    [Fact]
    public async Task Contact_messages_are_counted_with_how_they_came_and_what_about()
    {
        var (http, umami) = Site();
        await http.PostAsJsonAsync("/api/contact", new { name = "Ada", email = "ada@example.com", subject = "Job Opportunity", message = "Hi" });
        var contact = await umami.WaitFor("contact");
        Assert.Equal("api", contact.GetProperty("data").GetProperty("via").GetString());
        Assert.Equal("sent", contact.GetProperty("data").GetProperty("result").GetString());
        Assert.Equal("Job Opportunity", contact.GetProperty("data").GetProperty("subject").GetString());
    }

    [Fact]
    public async Task Server_events_send_a_browser_user_agent_so_umami_keeps_them()
    {
        var (http, umami) = Site();
        await http.PostAsJsonAsync("/api/contact", new { name = "Ada", email = "ada@example.com", subject = "Hello", message = "Hi" });
        await umami.WaitFor("contact");
        var (request, _) = umami.Sent[0];
        Assert.Equal("https://gateway.umami.is/api/send", request.RequestUri!.ToString());
        Assert.StartsWith("Mozilla/5.0", request.Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task Nothing_is_sent_unless_enabled()
    {
        var umami = new FakeUmami();
        var http = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.AddHttpClient<Analytics>().ConfigurePrimaryHttpMessageHandler(() => umami);
            s.AddSingleton<IEmailService>(new FakeEmail());
        })).CreateClient();
        await http.PostAsJsonAsync("/api/contact", new { name = "Ada", email = "ada@example.com", subject = "Hello", message = "Hi" });
        await Task.Delay(300);
        Assert.Empty(umami.Sent);
    }
}
