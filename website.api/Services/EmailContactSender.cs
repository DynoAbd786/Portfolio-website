using website.api.Models;
using website.Data;

namespace website.api.Services;

// The contact form is handled on the server, so it emails through the same service as /api/contact
public class EmailContactSender : IContactSender
{
    private readonly IEmailService _email;
    private readonly ILogger<EmailContactSender> _logger;
    private readonly Analytics _analytics;

    public EmailContactSender(IEmailService email, Analytics analytics, ILogger<EmailContactSender> logger)
    {
        _email = email;
        _analytics = analytics;
        _logger = logger;
    }

    public async Task<bool> SendAsync(ContactMessage m)
    {
        bool sent;
        try
        {
            sent = await _email.SendContactEmailAsync(new ContactRequest
            {
                Name = m.Name, Email = m.Email, Company = m.Company, Role = m.Role, Subject = m.Subject, Message = m.Message,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Contact form email from {Email} failed", m.Email);
            sent = false;
        }
        _analytics.Track("contact", new Dictionary<string, string?>
        {
            ["via"] = "form", ["result"] = sent ? "sent" : "failed", ["subject"] = m.Subject, ["company"] = m.Company, ["role"] = m.Role,
        }, "/#contact");
        return sent;
    }
}
