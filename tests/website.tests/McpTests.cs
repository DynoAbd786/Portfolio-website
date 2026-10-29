using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using website.api.Models;
using website.api.Services;
using website.Data;

namespace website.tests;

// The MCP server at /api/mcp, as the official MCP client sees it (Streamable HTTP, the SDK's
// stateless mode). Email is faked.
public class McpTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public McpTests(SiteFactory factory) => _factory = factory;

    private sealed class FakeEmail : IEmailService
    {
        public List<ContactRequest> Sent { get; } = new();
        public Task<bool> SendContactEmailAsync(ContactRequest request)
        {
            Sent.Add(request);
            return Task.FromResult(true);
        }
    }

    private (HttpClient Http, FakeEmail Email) Site()
    {
        var email = new FakeEmail();
        var http = _factory.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddSingleton<IEmailService>(email))).CreateClient();
        return (http, email);
    }

    private static Task<McpClient> Connect(HttpClient http) => McpClient.CreateAsync(
        new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(http.BaseAddress!, "/api/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
        }, http, ownsHttpClient: false));

    private static Dictionary<string, object?> Contact(string email = "ada@example.com") =>
        new() { ["name"] = "Ada", ["email"] = email, ["subject"] = "Job Opportunity", ["message"] = "We'd like to talk." };

    private static string Text(CallToolResult r) => string.Join("\n", r.Content.OfType<TextContentBlock>().Select(c => c.Text));

    private static async Task<string> Read(McpClient mcp, string uri) =>
        ((TextResourceContents)(await mcp.ReadResourceAsync(uri)).Contents[0]).Text;

    [Fact]
    public async Task Tools_say_which_only_read_and_which_send_a_message()
    {
        var (http, _) = Site();
        await using var mcp = await Connect(http);
        var tools = (await mcp.ListToolsAsync()).ToDictionary(t => t.Name, t => t.ProtocolTool);
        Assert.Equal(new[] { "search_projects", "submit_contact" }, tools.Keys.Order());
        Assert.True(tools["search_projects"].Annotations!.ReadOnlyHint);
        Assert.False(tools["search_projects"].Annotations!.OpenWorldHint);
        Assert.NotNull(tools["search_projects"].OutputSchema);
        Assert.False(tools["submit_contact"].Annotations!.ReadOnlyHint);
        Assert.False(tools["submit_contact"].Annotations!.DestructiveHint);
        Assert.True(tools["submit_contact"].Annotations!.OpenWorldHint);
        var required = tools["submit_contact"].InputSchema.GetProperty("required").EnumerateArray().Select(r => r.GetString()).Order();
        Assert.Equal(new[] { "email", "message", "name", "subject" }, required);
    }

    [Fact]
    public async Task Search_returns_structured_projects_with_their_pages()
    {
        var (http, _) = Site();
        await using var mcp = await Connect(http);
        var result = await mcp.CallToolAsync("search_projects", new Dictionary<string, object?> { ["keyword"] = "HireForge" });
        Assert.NotEqual(true, result.IsError);
        var projects = JsonSerializer.SerializeToElement(result.StructuredContent).GetProperty("projects");
        var hireforge = projects.EnumerateArray().Single(p => p.GetProperty("title").GetString()!.StartsWith("HireForge"));
        Assert.Equal("https://mkkai.dev/projects/hireforge", hireforge.GetProperty("page").GetString());
    }

    [Fact]
    public async Task Submit_contact_rejects_missing_fields_and_sends_nothing()
    {
        var (http, email) = Site();
        await using var mcp = await Connect(http);
        // fields sent but empty get a message that says which; fields left out fail the schema
        var empty = await mcp.CallToolAsync("submit_contact", new Dictionary<string, object?> { ["name"] = "x", ["email"] = "", ["subject"] = " ", ["message"] = "" });
        Assert.True(empty.IsError);
        Assert.Contains("Missing: email, subject, message", Text(empty));
        var omitted = await mcp.CallToolAsync("submit_contact", new Dictionary<string, object?> { ["name"] = "x" });
        Assert.True(omitted.IsError);
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task Submit_contact_rejects_a_bad_email()
    {
        var (http, email) = Site();
        await using var mcp = await Connect(http);
        Assert.True((await mcp.CallToolAsync("submit_contact", Contact("not-an-email"))).IsError);
        Assert.True((await mcp.CallToolAsync("submit_contact", Contact("ada@nope"))).IsError); // no real domain
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task Submit_contact_sends_a_complete_message()
    {
        var (http, email) = Site();
        await using var mcp = await Connect(http);
        Assert.NotEqual(true, (await mcp.CallToolAsync("submit_contact", Contact())).IsError);
        Assert.Equal("Ada", Assert.Single(email.Sent).Name);
    }

    [Fact]
    public async Task Submit_contact_limits_how_often_one_visitor_can_send()
    {
        var (http, email) = Site();
        await using var mcp = await Connect(http);
        for (var i = 0; i < ContactLimiter.PerVisitorPerHour; i++)
            Assert.NotEqual(true, (await mcp.CallToolAsync("submit_contact", Contact())).IsError);
        var over = await mcp.CallToolAsync("submit_contact", Contact());
        Assert.True(over.IsError);
        Assert.Contains("later", Text(over));
        Assert.Equal(ContactLimiter.PerVisitorPerHour, email.Sent.Count);
    }

    [Fact]
    public async Task Each_project_is_a_resource_named_after_its_page()
    {
        var (http, _) = Site();
        await using var mcp = await Connect(http);
        var templates = (await mcp.ListResourceTemplatesAsync()).Select(t => t.UriTemplate).ToList();
        Assert.Contains("portfolio://projects/{id}", templates);
        Assert.Contains("HireForge", await Read(mcp, "portfolio://projects/hireforge"));
        Assert.Contains("pNanoLocz", await Read(mcp, "portfolio://projects/pnanolocz"));
        await Assert.ThrowsAsync<McpProtocolException>(async () => await mcp.ReadResourceAsync("portfolio://projects/nope"));
    }

    [Fact]
    public async Task Resources_cover_the_profile_contact_about_and_this_page()
    {
        var (http, _) = Site();
        await using var mcp = await Connect(http);
        var uris = (await mcp.ListResourcesAsync()).Select(r => r.Uri).ToHashSet();
        foreach (var uri in new[] { "portfolio://profile", "portfolio://contact", "portfolio://about", "portfolio://ai-integration", "portfolio://projects/all" })
            Assert.Contains(uri, uris);
        Assert.DoesNotContain("portfolio://ethos", uris);
    }

    [Fact]
    public async Task Profile_matches_what_the_homepage_shows()
    {
        var (http, _) = Site();
        await using var mcp = await Connect(http);
        var profile = await Read(mcp, "portfolio://profile");
        foreach (var item in HomeContent.Stack.SelectMany(g => g.Items)) Assert.Contains(item, profile);
        foreach (var e in HomeContent.Experience) Assert.Contains(e.Title, profile);
        foreach (var p in HomeContent.Products) Assert.Contains(p.Name, profile);
        Assert.DoesNotContain("Gradle", profile);
        var ai = await Read(mcp, "portfolio://ai-integration");
        Assert.DoesNotContain("GPT-4", ai);
        Assert.Contains("DeepSeek", ai);
        Assert.DoesNotContain("/sse", ai);
    }

    [Fact]
    public async Task Plain_curl_from_the_ai_page_works()
    {
        // the page's examples: one POST each, no session, the Accept header MCP requires
        var (http, _) = Site();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/mcp")
        {
            Content = JsonContent.Create(new { jsonrpc = "2.0", id = 1, method = "tools/list" }),
        };
        request.Headers.Add("Accept", "application/json, text/event-stream");
        var response = await http.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("search_projects", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("DELETE")]
    public async Task No_stream_or_session_to_open_or_close_is_a_405(string method)
    {
        // the spec: a server without the standalone SSE stream answers GET with 405 (clients like the
        // MCP Inspector disconnect on anything else), and a stateless one has no session to DELETE
        var (http, _) = Site();
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/mcp");
        request.Headers.Add("Accept", "text/event-stream");
        var response = await http.SendAsync(request);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Contains("POST", response.Content.Headers.Allow);
    }

    [Theory]
    [InlineData("/api/mcp/sse")]
    [InlineData("/api/mcp/status/bridge")]
    [InlineData("/api/oauth/.well-known/oauth-authorization-server")]
    [InlineData("/api/chat/models")]
    public async Task The_old_bridge_sse_and_oauth_endpoints_are_gone(string path)
    {
        var (http, _) = Site();
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync(path)).StatusCode);
    }
}
