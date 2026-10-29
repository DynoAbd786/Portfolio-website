using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using website.api.Services;
using website.Data;

namespace website.tests;

// The assistant runs on one model, DeepSeek Flash. The DeepSeek API is faked, so no real calls are made.
public class ChatTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public ChatTests(SiteFactory factory) => _factory = factory;

    private sealed class FakeDeepSeek : HttpMessageHandler
    {
        public List<(Uri Url, string Body)> Calls { get; } = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls.Add((request.RequestUri!, request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct)));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"choices":[{"message":{"role":"assistant","content":"hello from deepseek"}}]}""", Encoding.UTF8, "application/json"),
            };
        }
    }

    private (HttpClient, FakeDeepSeek) Client(string apiKey)
    {
        var api = new FakeDeepSeek();
        var client = _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["DeepSeek:ApiKey"] = apiKey }));
            b.ConfigureTestServices(s => s.AddHttpClient<DeepSeekAgentService>().ConfigurePrimaryHttpMessageHandler(() => api));
        }).CreateClient();
        return (client, api);
    }

    private static async Task<string> Ask(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/chat", body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Dictionary<string, string>>())!["response"];
    }

    [Fact]
    public async Task Chat_answers_with_deepseek_flash()
    {
        var (client, api) = Client("test-key");
        Assert.Equal("hello from deepseek", await Ask(client, new { message = "What did you build as an intern?", history = Array.Empty<object>() }));
        var call = Assert.Single(api.Calls);
        Assert.Equal("api.deepseek.com", call.Url.Host);
        Assert.Contains("\"model\":\"deepseek-flash\"", call.Body);
    }

    [Fact]
    public async Task Chat_ignores_any_other_provider_or_model_a_request_names()
    {
        var (client, api) = Client("test-key");
        await Ask(client, new { message = "hi", history = Array.Empty<object>(), provider = "groq", model = "llama-3.3-70b-versatile" });
        Assert.Contains("\"model\":\"deepseek-flash\"", Assert.Single(api.Calls).Body);
    }

    [Fact]
    public async Task Chat_without_a_key_says_so_and_points_to_email()
    {
        var (client, api) = Client("");
        var reply = await Ask(client, new { message = "hi", history = Array.Empty<object>() });
        Assert.Contains("Abdua786@outlook.com", reply);
        Assert.DoesNotContain(".env", reply);
        Assert.Empty(api.Calls);
    }

    private static string SystemPrompt(string body) =>
        JsonDocument.Parse(body).RootElement.GetProperty("messages")[0].GetProperty("content").GetString()!;

    [Fact]
    public async Task Assistant_knows_the_profile_the_homepage_shows()
    {
        var (client, api) = Client("test-key");
        await Ask(client, new { message = "Tell me about him", history = Array.Empty<object>() });
        var prompt = SystemPrompt(Assert.Single(api.Calls).Body);
        Assert.Contains("open to work", prompt);
        foreach (var e in HomeContent.Experience) Assert.Contains(e.Title, prompt);
        foreach (var p in HomeContent.Products) Assert.Contains(p.Name, prompt);
        foreach (var item in HomeContent.Stack.SelectMany(g => g.Items)) Assert.Contains(item, prompt);
        Assert.Contains("/projects/hireforge", prompt);
        Assert.Contains("/#contact", prompt);
        Assert.Contains("never a bare address", prompt);
        Assert.Contains("the link is the words that name it", prompt);
    }
}
