using System.Net;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using website.Data;

namespace website.tests;

// The .NET WebAssembly runtime (about 3 MB) only loads where a page needs it: the chat loads it
// when opened, and the contact form works as a plain server-side form.
public class LazyRuntimeTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public LazyRuntimeTests(SiteFactory factory) => _factory = factory;

    // Pages that need it for their own interactive parts (image gallery, live bridge status)
    private static readonly string[] NeedsRuntime = { "/projects/medical-simulation", "/projects/machine-learning", "/projects/home-lab" };

    [Theory]
    [MemberData(nameof(RouteTests.AllPageRoutes), MemberType = typeof(RouteTests))]
    public async Task Pages_do_not_start_the_webassembly_runtime(string path)
    {
        if (NeedsRuntime.Contains(path)) return;
        var html = await _factory.CreateClient().GetStringAsync(path);
        Assert.DoesNotContain("\"type\":\"webassembly\"", html);
    }

    [Fact]
    public async Task Chat_panel_page_hosts_the_assistant_and_is_not_indexed()
    {
        var html = await _factory.CreateClient().GetStringAsync("/chat-panel");
        Assert.Contains("\"type\":\"webassembly\"", html);
        Assert.Contains("noindex", html);
    }

    private sealed class FakeSender : IContactSender
    {
        public List<ContactMessage> Sent { get; } = new();
        public Task<bool> SendAsync(ContactMessage message) { Sent.Add(message); return Task.FromResult(true); }
    }

    private async Task<(HttpResponseMessage, FakeSender)> PostContact(Dictionary<string, string> fields)
    {
        var sender = new FakeSender();
        var client = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<IContactSender>(sender))).CreateClient();
        var page = await Html.ParseAsync(await client.GetStringAsync("/"));
        var form = (IHtmlFormElement)page.QuerySelector("#contact form")!;
        var data = form.QuerySelectorAll("input[type=hidden]").Cast<IHtmlInputElement>().ToDictionary(i => i.Name!, i => i.Value);
        foreach (var (k, v) in fields) data[form.QuerySelector($"#{k}")!.GetAttribute("name")!] = v;
        var response = await client.PostAsync(form.GetAttribute("action") is { Length: > 0 } a ? a : "/", new FormUrlEncodedContent(data));
        return (response, sender);
    }

    [Fact]
    public async Task Contact_form_sends_without_javascript()
    {
        var (response, sender) = await PostContact(new()
        {
            ["name"] = "Ada Lovelace", ["email"] = "ada@example.com", ["subject"] = "Job Opportunity",
            ["message"] = "We'd like to talk about a graduate role.", ["agree"] = "true",
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sent = Assert.Single(sender.Sent);
        Assert.Equal("ada@example.com", sent.Email);
        Assert.Contains("Thank you", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Contact_form_rejects_a_bad_message()
    {
        var (response, sender) = await PostContact(new() { ["name"] = "", ["email"] = "not-an-email", ["message"] = "short" });
        Assert.Empty(sender.Sent);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Name is required", body);
        Assert.Contains("valid email", body);
    }
}

public class ContactDeliveryTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public ContactDeliveryTests(SiteFactory factory) => _factory = factory;

    // contact@mkkai.dev is not a mailbox Muhammad reads; form messages go to his own inbox
    [Fact]
    public void Contact_form_messages_go_to_muhammads_inbox()
    {
        var config = _factory.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        Assert.Equal("Abdua786@outlook.com", config["ContactEmail"]);
    }
}
