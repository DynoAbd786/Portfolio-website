using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using website.api.Services;

namespace website.tests;

// Contact messages are emailed through Resend. The Resend API is faked, so no real emails are sent.
public class ContactTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public ContactTests(SiteFactory factory) => _factory = factory;

    private sealed class FakeResend : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _reply;
        public FakeResend(HttpStatusCode status, string reply) => (_status, _reply) = (status, reply);

        public List<(HttpRequestMessage Request, string Body)> Calls { get; } = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls.Add((request, request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct)));
            return new HttpResponseMessage(_status) { Content = new StringContent(_reply, Encoding.UTF8, "application/json") };
        }
    }

    private (HttpClient, FakeResend) Client(string apiKey, HttpStatusCode status = HttpStatusCode.OK, string reply = """{"id":"4ef9a417-02e9-4d39-ad75-9611e0fcc33c"}""")
    {
        var api = new FakeResend(status, reply);
        var client = _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["Resend:ApiKey"] = apiKey }));
            b.ConfigureTestServices(s => s.AddHttpClient<IEmailService, EmailService>().ConfigurePrimaryHttpMessageHandler(() => api));
        }).CreateClient();
        return (client, api);
    }

    private static object Message(string message = "Hi, are you open to a graduate role?") => new
    {
        name = "Ada Recruiter", email = "ada@example.com", company = "Acme", subject = "Graduate role", message,
    };

    [Fact]
    public async Task Contact_message_is_emailed_to_muhammad_through_resend()
    {
        var (client, api) = Client("test-key");
        var response = await client.PostAsJsonAsync("/api/contact", Message());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (request, body) = Assert.Single(api.Calls);
        Assert.Equal("https://api.resend.com/emails", request.RequestUri!.ToString());
        Assert.Equal("Bearer test-key", request.Headers.Authorization!.ToString());
        var email = JsonDocument.Parse(body).RootElement;
        Assert.Equal("Abdua786@outlook.com", email.GetProperty("to")[0].GetString());
        Assert.Equal("ada@example.com", email.GetProperty("reply_to")[0].GetString());
        Assert.Contains("noreply@mkkai.dev", email.GetProperty("from").GetString());
        Assert.Contains("Graduate role", email.GetProperty("subject").GetString());
        Assert.Contains("open to a graduate role", email.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Contact_reports_failure_when_resend_rejects_the_email()
    {
        var (client, _) = Client("test-key", HttpStatusCode.Forbidden, """{"statusCode":403,"name":"validation_error","message":"The mkkai.dev domain is not verified."}""");
        var response = await client.PostAsJsonAsync("/api/contact", Message());
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Contact_reports_failure_when_resend_returns_no_email_id()
    {
        var (client, _) = Client("test-key", HttpStatusCode.OK, "{}");
        var response = await client.PostAsJsonAsync("/api/contact", Message());
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Contact_without_a_key_fails_without_calling_resend()
    {
        var (client, api) = Client("");
        var response = await client.PostAsJsonAsync("/api/contact", Message());
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Empty(api.Calls);
    }

    [Fact]
    public async Task Visitor_text_is_escaped_in_the_html_email()
    {
        var (client, api) = Client("test-key");
        await client.PostAsJsonAsync("/api/contact", Message("<img src=x onerror=alert(1)> hello"));
        var html = JsonDocument.Parse(Assert.Single(api.Calls).Body).RootElement.GetProperty("html").GetString()!;
        Assert.Contains("&lt;img src=x onerror=alert(1)&gt; hello", html);
        Assert.DoesNotContain("<img", html);
    }
}
