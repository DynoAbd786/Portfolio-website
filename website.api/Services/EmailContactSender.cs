using website.api.Models;
using website.Data;

namespace website.api.Services;

// The homepage contact form is handled on the server: this checks it isn't a bot, that the address
// can get a reply (IMailDomains), keeps to the hourly limits it shares with MCP and the assistant
// (ContactLimiter), and emails it through Resend
public class EmailContactSender : IContactSender
{
    private readonly IEmailService _email;
    private readonly ContactLimiter _limiter;
    private readonly FormStamp _stamp;
    private readonly IMailDomains _domains;
    private readonly IHttpContextAccessor _http;
    private readonly Analytics _analytics;
    private readonly ILogger<EmailContactSender> _logger;

    public EmailContactSender(IEmailService email, ContactLimiter limiter, FormStamp stamp, IMailDomains domains, IHttpContextAccessor http, Analytics analytics, ILogger<EmailContactSender> logger)
    {
        _email = email;
        _limiter = limiter;
        _stamp = stamp;
        _domains = domains;
        _http = http;
        _analytics = analytics;
        _logger = logger;
    }

    public string StampForm() => _stamp.Issue();

    public async Task<ContactResult> SendAsync(ContactMessage m, FormCheck check)
    {
        var (result, reason) = await TrySendAsync(m, check, Visitor.Ip(_http.HttpContext));
        _analytics.Track("contact", new Dictionary<string, string?>
        {
            ["via"] = "form", ["result"] = result.ToString().ToLowerInvariant(), ["reason"] = reason,
            ["subject"] = m.Subject, ["company"] = m.Company, ["role"] = m.Role,
        }, "/#contact");
        return result;
    }

    private async Task<(ContactResult, string? Reason)> TrySendAsync(ContactMessage m, FormCheck check, string visitor)
    {
        var bot = !string.IsNullOrEmpty(check.Trap) ? "trap" : _stamp.Problem(check.Stamp);
        if (bot is not null)
        {
            _logger.LogWarning("Contact form from {Visitor} dropped as a bot ({Reason}): {Name} <{Email}>", visitor, bot, m.Name, m.Email);
            return (ContactResult.Dropped, bot);
        }

        // after the bot checks, so a bot is never told what was wrong; before the limit, so fixing a
        // typo doesn't use up the person's messages for the hour
        if (!await _domains.AcceptsMailAsync(Emails.Domain(m.Email)))
        {
            _logger.LogInformation("Contact form from {Visitor} turned back: {Email} has no mail server", visitor, m.Email);
            return (ContactResult.Undeliverable, null);
        }

        if (!_limiter.TryTake(visitor))
        {
            _logger.LogWarning("Contact limit reached for {Visitor}", visitor);
            return (ContactResult.Limited, null);
        }

        try
        {
            var sent = await _email.SendContactEmailAsync(new ContactRequest
            {
                Name = m.Name, Email = m.Email, Company = m.Company, Role = m.Role, Subject = m.Subject, Message = m.Message,
            });
            return (sent ? ContactResult.Sent : ContactResult.Failed, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Contact form email from {Email} failed", m.Email);
            return (ContactResult.Failed, null);
        }
    }
}
