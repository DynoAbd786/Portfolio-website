using DnsClient;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using website.api.Models;
using website.api.Services;

namespace website.tests;

// A reply to a mistyped address bounces, so the form checks the address before sending: it must
// look like name@domain.tld, and the domain must have a mail server. In memory only .invalid
// domains have none (SiteFactory); MailDomainsTests below asks real DNS. Email is faked.
public class ContactEmailTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public ContactEmailTests(SiteFactory factory) => _factory = factory;

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

    private static Dictionary<string, string> WithEmail(string email, string? trap = null)
    {
        var fields = ContactForm.Valid();
        fields["email"] = email;
        if (trap is not null) fields["website"] = trap;
        return fields;
    }

    [Theory]
    [InlineData("ada@gmail")] // no .com
    [InlineData("ada@gmail.")]
    [InlineData("ada@.com")]
    [InlineData("ada@gmail..com")]
    [InlineData("ada@gmail.c")]
    [InlineData("ada@gmail,co.uk")]
    [InlineData("ada lovelace@gmail.com")]
    [InlineData("ada@@gmail.com")]
    public async Task An_address_that_is_not_name_at_domain_dot_tld_is_turned_back(string address)
    {
        var (http, email, clock) = Site();
        var body = await (await ContactForm.PostAsync(http, WithEmail(address), clock)).Content.ReadAsStringAsync();
        Assert.Contains("Please enter a valid email address", body);
        Assert.DoesNotContain("Thank you", body);
        Assert.Empty(email.Sent);
    }

    [Theory]
    [InlineData("ada.lovelace+jobs@example.co.uk")]
    [InlineData(" ada@example.com ")]
    [InlineData("ada@bücher.de")]
    [InlineData("ADA@EXAMPLE.ORG")]
    public async Task Ordinary_addresses_go_through(string address)
    {
        var (http, email, clock) = Site();
        var body = await (await ContactForm.PostAsync(http, WithEmail(address), clock)).Content.ReadAsStringAsync();
        Assert.Contains("Thank you", body);
        Assert.Equal(address.Trim(), Assert.Single(email.Sent).Email);
    }

    [Fact]
    public async Task An_address_whose_domain_has_no_mail_server_is_turned_back_with_the_domain_named()
    {
        var (http, email, clock) = Site();
        var page = await Html.ParseAsync(await (await ContactForm.PostAsync(http, WithEmail("ada@gmial.invalid"), clock)).Content.ReadAsStringAsync());
        // shown under the email box, like the other field errors, with the box marked as wrong
        var box = page.QuerySelector("#email")!;
        Assert.Equal("true", box.GetAttribute("aria-invalid"));
        var error = box.NextElementSibling!;
        Assert.Contains("text-red-400", error.ClassName);
        Assert.Contains("Emails to gmial.invalid can't be delivered", error.TextContent);
        Assert.Contains("check your address", error.TextContent);
        Assert.DoesNotContain("Thank you", page.QuerySelector("#contact")!.TextContent);
        Assert.Empty(email.Sent);
    }

    // the bot checks come first, so a bot is never told what was wrong
    [Fact]
    public async Task A_bot_with_a_bad_domain_is_still_just_dropped()
    {
        var (http, email, clock) = Site();
        var body = await (await ContactForm.PostAsync(http, WithEmail("bot@spam.invalid", trap: "https://spam.example"), clock)).Content.ReadAsStringAsync();
        Assert.Contains("Thank you", body);
        Assert.DoesNotContain("spam.invalid", body);
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task Fixing_a_typo_does_not_use_up_the_hourly_limit()
    {
        var (http, email, clock) = Site();
        http.DefaultRequestHeaders.Add("CF-Connecting-IP", "203.0.113.9");
        for (var i = 0; i < ContactLimiter.PerVisitorPerHour; i++)
            await ContactForm.PostAsync(http, WithEmail("ada@gmial.invalid"), clock);
        Assert.Contains("Thank you", await (await ContactForm.PostAsync(http, ContactForm.Valid(), clock)).Content.ReadAsStringAsync());
        Assert.Single(email.Sent);
    }
}

// The real check, against live DNS (needs the network)
public class MailDomainsTests
{
    private static MailDomains Check(LookupClient? dns = null) => new(dns ?? new LookupClient(), NullLogger<MailDomains>.Instance);

    [Theory]
    [InlineData("gmail.com")]
    [InlineData("outlook.com")]
    [InlineData("GMAIL.COM")]
    public async Task A_domain_with_a_mail_server_takes_mail(string domain) =>
        Assert.True(await Check().AcceptsMailAsync(domain));

    [Theory]
    [InlineData("example.com")] // says outright it takes no mail (null MX, RFC 7505)
    [InlineData("www.google.com")] // a website, but no mail server: how most typo domains look
    [InlineData("nothing-here.invalid")] // doesn't exist
    public async Task A_domain_without_one_does_not(string domain) =>
        Assert.False(await Check().AcceptsMailAsync(domain));

    // a DNS hiccup must never turn a real person away
    [Fact]
    public async Task When_dns_itself_fails_the_address_is_let_through()
    {
        var unreachable = new LookupClient(new LookupClientOptions(new NameServer(System.Net.IPAddress.Loopback, 9))
        {
            Timeout = TimeSpan.FromMilliseconds(500), Retries = 0, UseTcpFallback = false,
        });
        Assert.True(await Check(unreachable).AcceptsMailAsync("gmail.com"));
    }

    // the site in memory swaps it for SiteFactory's stand-in, so check the real one is what runs
    [Fact]
    public void The_site_uses_the_real_check()
    {
        using var site = new WebApplicationFactory<Program>();
        Assert.IsType<MailDomains>(site.Services.GetRequiredService<IMailDomains>());
    }
}
