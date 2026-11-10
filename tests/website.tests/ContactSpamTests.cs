using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using website.api.Models;
using website.api.Services;

namespace website.tests;

// The homepage form keeps bots out without bothering people: a field only bots fill in, a check that
// the form wasn't sent seconds after the page loaded, a few messages per visitor an hour, and fields
// of a sensible size. A dropped message still looks sent, so a bot learns nothing. Email is faked.
public class ContactSpamTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public ContactSpamTests(SiteFactory factory) => _factory = factory;

    private sealed class FakeEmail : IEmailService
    {
        public List<ContactRequest> Sent { get; } = new();
        public Task<bool> SendContactEmailAsync(ContactRequest request)
        {
            lock (Sent) Sent.Add(request);
            return Task.FromResult(true);
        }
    }

    private (HttpClient Http, FakeEmail Email, TestClock Clock) Site()
    {
        var email = new FakeEmail();
        var clock = new TestClock();
        var http = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.AddSingleton<IEmailService>(email);
            s.AddSingleton<TimeProvider>(clock);
        })).CreateClient();
        return (http, email, clock);
    }

    private static Dictionary<string, string> With(string field, string value)
    {
        var fields = ContactForm.Valid();
        fields[field] = value;
        return fields;
    }

    [Fact]
    public async Task A_person_who_takes_their_time_gets_their_message_sent()
    {
        var (http, email, clock) = Site();
        var response = await ContactForm.PostAsync(http, ContactForm.Valid(), clock);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Thank you", await response.Content.ReadAsStringAsync());
        Assert.Equal("ada@example.com", Assert.Single(email.Sent).Email);
    }

    [Fact]
    public async Task A_filled_in_trap_field_is_dropped_but_looks_sent()
    {
        var (http, email, clock) = Site();
        var response = await ContactForm.PostAsync(http, With("website", "https://example.com"), clock);
        Assert.Contains("Thank you", await response.Content.ReadAsStringAsync());
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task A_form_sent_seconds_after_loading_is_dropped_but_looks_sent()
    {
        var (http, email, clock) = Site();
        var response = await ContactForm.PostAsync(http, ContactForm.Valid(), clock, writing: TimeSpan.FromSeconds(1));
        Assert.Contains("Thank you", await response.Content.ReadAsStringAsync());
        Assert.Empty(email.Sent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1731000000000")]
    [InlineData("CfDJ8made-up")]
    public async Task A_missing_or_made_up_form_stamp_is_dropped(string stamp)
    {
        var (http, email, clock) = Site();
        var response = await ContactForm.PostAsync(http, With("stamp", stamp), clock);
        Assert.Contains("Thank you", await response.Content.ReadAsStringAsync());
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task One_visitor_can_send_a_few_messages_an_hour()
    {
        var (http, email, clock) = Site();
        http.DefaultRequestHeaders.Add("CF-Connecting-IP", "203.0.113.7");
        for (var i = 0; i < ContactLimiter.PerVisitorPerHour; i++)
            Assert.Contains("Thank you", await (await ContactForm.PostAsync(http, ContactForm.Valid(), clock)).Content.ReadAsStringAsync());

        var over = await (await ContactForm.PostAsync(http, ContactForm.Valid(), clock)).Content.ReadAsStringAsync();
        Assert.DoesNotContain("Thank you", over);
        Assert.Contains("try again later", over);
        Assert.Equal(ContactLimiter.PerVisitorPerHour, email.Sent.Count);

        // someone else (Cloudflare tells them apart on the live site) can still write
        http.DefaultRequestHeaders.Remove("CF-Connecting-IP");
        http.DefaultRequestHeaders.Add("CF-Connecting-IP", "198.51.100.4");
        Assert.Contains("Thank you", await (await ContactForm.PostAsync(http, ContactForm.Valid(), clock)).Content.ReadAsStringAsync());
        Assert.Equal(ContactLimiter.PerVisitorPerHour + 1, email.Sent.Count);
    }

    [Theory]
    [InlineData("name", 101, "Name is too long")]
    [InlineData("email", 255, "Email is too long")]
    [InlineData("company", 101, "Company is too long")]
    [InlineData("role", 101, "Role is too long")]
    [InlineData("message", 5001, "Message is too long")]
    public async Task Fields_have_a_sensible_size(string field, int length, string error)
    {
        var (http, email, clock) = Site();
        var value = field == "email" ? new string('a', length - "@example.com".Length) + "@example.com" : new string('a', length);
        var response = await ContactForm.PostAsync(http, With(field, value), clock);
        Assert.Contains(error, await response.Content.ReadAsStringAsync());
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task Fields_say_how_long_they_can_be()
    {
        var form = (await Html.GetAsync(_factory.CreateClient(), "/")).QuerySelector("#contact form")!;
        Assert.Equal("100", form.QuerySelector("#name")!.GetAttribute("maxlength"));
        Assert.Equal("254", form.QuerySelector("#email")!.GetAttribute("maxlength"));
        Assert.Equal("5000", form.QuerySelector("#message")!.GetAttribute("maxlength"));
    }

    [Fact]
    public async Task The_subject_must_be_one_from_the_list()
    {
        var (http, email, clock) = Site();
        var response = await ContactForm.PostAsync(http, With("subject", "Cheap followers"), clock);
        Assert.Contains("Please select a subject", await response.Content.ReadAsStringAsync());
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task The_trap_field_is_hidden_from_people_but_not_from_bots()
    {
        var form = (await Html.GetAsync(_factory.CreateClient(), "/")).QuerySelector("#contact form")!;
        var trap = form.QuerySelector("#website")!;
        // a plain text box (bots skip type=hidden), out of sight, out of the tab order and away from screen readers and autofill
        Assert.Equal("text", trap.GetAttribute("type") ?? "text");
        Assert.Equal("-1", trap.GetAttribute("tabindex"));
        Assert.Equal("off", trap.GetAttribute("autocomplete"));
        var wrapper = trap.Closest(".form-trap")!;
        Assert.Equal("true", wrapper.GetAttribute("aria-hidden"));
    }

    // It had no limits and nothing used it; the form and MCP are the ways in
    [Fact]
    public async Task The_open_contact_api_is_gone()
    {
        var (http, email, _) = Site();
        Task<HttpResponseMessage> Post(string path) => http.PostAsync(path, new StringContent(
            """{"name":"x","email":"x@example.com","subject":"x","message":"Hello there, a message"}""", System.Text.Encoding.UTF8, "application/json"));
        var response = await Post("/api/contact");
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal((await Post("/api/no-such-thing")).StatusCode, response.StatusCode);
        Assert.Empty(email.Sent);
    }
}
