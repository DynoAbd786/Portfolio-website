using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using website.api.Models;
using website.api.Services;
using website.Data;

namespace website.tests;

// The MCP server at /api/mcp (JSON-RPC 2.0), which AI agents and the site's assistant use. Email is faked.
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

    private (HttpClient, FakeEmail) Client()
    {
        var email = new FakeEmail();
        var client = _factory.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddSingleton<IEmailService>(email))).CreateClient();
        return (client, email);
    }

    private static async Task<HttpResponseMessage> Post(HttpClient client, string method, object? @params = null, int? id = 1)
    {
        var body = new Dictionary<string, object?> { ["jsonrpc"] = "2.0", ["method"] = method };
        if (id is not null) body["id"] = id;
        if (@params is not null) body["params"] = @params;
        return await client.PostAsJsonAsync("/api/mcp", body);
    }

    private static async Task<JsonElement> Rpc(HttpClient client, string method, object? @params = null)
    {
        var response = await Post(client, method, @params);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    private static async Task<(bool IsError, string Text)> Call(HttpClient client, string tool, object arguments)
    {
        var result = (await Rpc(client, "tools/call", new { name = tool, arguments })).GetProperty("result");
        return (result.GetProperty("isError").GetBoolean(), result.GetProperty("content")[0].GetProperty("text").GetString()!);
    }

    private static async Task<string> Read(HttpClient client, string uri) =>
        (await Rpc(client, "resources/read", new { uri })).GetProperty("result").GetProperty("contents")[0].GetProperty("text").GetString()!;

    private static object Contact(string email = "ada@example.com") =>
        new { name = "Ada", email, subject = "Job Opportunity", message = "We'd like to talk." };

    [Fact]
    public async Task Submit_contact_rejects_missing_fields_and_sends_nothing()
    {
        var (client, email) = Client();
        var (isError, text) = await Call(client, "submit_contact", new { name = "x" });
        Assert.True(isError);
        Assert.Contains("email", text);
        Assert.Contains("message", text);
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task Submit_contact_rejects_a_bad_email()
    {
        var (client, email) = Client();
        var (isError, _) = await Call(client, "submit_contact", Contact("not-an-email"));
        Assert.True(isError);
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task Submit_contact_sends_a_complete_message()
    {
        var (client, email) = Client();
        var (isError, _) = await Call(client, "submit_contact", Contact());
        Assert.False(isError);
        Assert.Equal("Ada", Assert.Single(email.Sent).Name);
    }

    [Fact]
    public async Task Submit_contact_limits_how_often_one_visitor_can_send()
    {
        var (client, email) = Client();
        for (var i = 0; i < ContactLimiter.PerVisitorPerHour; i++)
            Assert.False((await Call(client, "submit_contact", Contact())).IsError);
        var (isError, text) = await Call(client, "submit_contact", Contact());
        Assert.True(isError);
        Assert.Contains("later", text);
        Assert.Equal(ContactLimiter.PerVisitorPerHour, email.Sent.Count);
    }

    [Fact]
    public async Task Ping_answers_with_an_empty_result()
    {
        var (client, _) = Client();
        var reply = await Rpc(client, "ping");
        Assert.Equal(JsonValueKind.Object, reply.GetProperty("result").ValueKind);
        Assert.False(reply.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null);
    }

    [Fact]
    public async Task Notifications_get_no_reply()
    {
        var (client, _) = Client();
        var response = await Post(client, "notifications/initialized", id: null);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Each_project_is_a_resource_named_after_its_page()
    {
        var (client, _) = Client();
        var uris = (await Rpc(client, "resources/list")).GetProperty("result").GetProperty("resources")
            .EnumerateArray().Select(r => r.GetProperty("uri").GetString()).ToList();
        Assert.Contains("portfolio://projects/hireforge", uris);
        Assert.Contains("portfolio://projects/pnanolocz", uris);
        Assert.Contains("HireForge", await Read(client, "portfolio://projects/hireforge"));
    }

    [Fact]
    public async Task Profile_matches_what_the_homepage_shows()
    {
        var (client, _) = Client();
        var profile = await Read(client, "portfolio://profile");
        foreach (var item in HomeContent.Stack.SelectMany(g => g.Items)) Assert.Contains(item, profile);
        foreach (var e in HomeContent.Experience) Assert.Contains(e.Title, profile);
        foreach (var p in HomeContent.Products) Assert.Contains(p.Name, profile);
        Assert.DoesNotContain("Gradle", profile);
    }
}
