using MailKit.Net.Smtp;
using MimeKit;
using website.api.Models;

namespace website.api.Services;

public interface IEmailService
{
    Task<bool> SendContactEmailAsync(ContactRequest contactRequest);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendContactEmailAsync(ContactRequest contactRequest)
    {
        try
        {
            var message = new MimeMessage();

            // From address - using the sender's email but setting reply-to
            message.From.Add(new MailboxAddress($"{contactRequest.Name} (Portfolio Contact)",
                _configuration["SmtpSettings:FromEmail"]));

            // To address
            message.To.Add(new MailboxAddress("Muhammad Kashif-Khan",
                _configuration["ContactEmail"]));

            // Reply-To address (so replies go to the actual sender)
            message.ReplyTo.Add(new MailboxAddress(contactRequest.Name, contactRequest.Email));

            // Subject
            message.Subject = $"[Portfolio Contact] {contactRequest.Subject}";

            // Body
            var bodyBuilder = new BodyBuilder();
            bodyBuilder.TextBody = $@"
New contact form submission from your portfolio website:

Name: {contactRequest.Name}
Email: {contactRequest.Email}
Company: {contactRequest.Company ?? "Not specified"}
Role: {contactRequest.Role ?? "Not specified"}
Subject: {contactRequest.Subject}

Message:
{contactRequest.Message}

---
This email was sent from your portfolio contact form.
Reply directly to this email to respond to {contactRequest.Name}.
";

            message.Body = bodyBuilder.ToMessageBody();

            // Send the email
            using var client = new SmtpClient();

            await client.ConnectAsync(
                _configuration["SmtpSettings:Host"],
                int.Parse(_configuration["SmtpSettings:Port"]),
                bool.Parse(_configuration["SmtpSettings:UseSsl"])
            );

            await client.AuthenticateAsync(
                _configuration["SmtpSettings:Username"],
                _configuration["SmtpSettings:Password"]
            );

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Contact email sent successfully from {Email}", contactRequest.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact email from {Email}", contactRequest.Email);
            return false;
        }
    }
}