using website.api.Models;
using website.Data;

namespace website.api.Services;

// The contact form is handled on the server, so it emails through the same service as /api/contact
public class EmailContactSender : IContactSender
{
    private readonly IEmailService _email;
    private readonly ILogger<EmailContactSender> _logger;

    public EmailContactSender(IEmailService email, ILogger<EmailContactSender> logger)
    {
        _email = email;
        _logger = logger;
    }

    public async Task<bool> SendAsync(ContactMessage m)
    {
        try
        {
            return await _email.SendContactEmailAsync(new ContactRequest
            {
                Name = m.Name, Email = m.Email, Company = m.Company, Role = m.Role, Subject = m.Subject, Message = m.Message,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Contact form email from {Email} failed", m.Email);
            return false;
        }
    }
}
