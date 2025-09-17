using PostmarkDotNet;
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
            // Log the contact form submission
            _logger.LogInformation("=== NEW CONTACT FORM SUBMISSION ===");
            _logger.LogInformation("Name: {Name}", contactRequest.Name);
            _logger.LogInformation("Email: {Email}", contactRequest.Email);
            _logger.LogInformation("Company: {Company}", contactRequest.Company ?? "Not specified");
            _logger.LogInformation("Role: {Role}", contactRequest.Role ?? "Not specified");
            _logger.LogInformation("Subject: {Subject}", contactRequest.Subject);
            _logger.LogInformation("Message: {Message}", contactRequest.Message);
            _logger.LogInformation("======================================");

            // Create Postmark client
            var client = new PostmarkClient(_configuration["POSTMARK_API_TOKEN"]);

            // Create email message
            var message = new PostmarkMessage()
            {
                To = _configuration["ContactEmail"],
                From = _configuration["Postmark:FromEmail"],
                TrackOpens = true,
                Subject = $"[Portfolio Contact] {contactRequest.Subject}",
                HtmlBody = $@"
                    <h2>New contact form submission from your portfolio website:</h2>
                    <p><strong>Name:</strong> {contactRequest.Name}</p>
                    <p><strong>Email:</strong> <a href=""mailto:{contactRequest.Email}"">{contactRequest.Email}</a></p>
                    <p><strong>Company:</strong> {contactRequest.Company ?? "Not specified"}</p>
                    <p><strong>Role:</strong> {contactRequest.Role ?? "Not specified"}</p>
                    <p><strong>Subject:</strong> {contactRequest.Subject}</p>
                    <p><strong>Message:</strong></p>
                    <p>{contactRequest.Message.Replace("\n", "<br>")}</p>
                    <hr>
                    <p><em>This email was sent from your portfolio contact form.</em></p>
                ",
                TextBody = $@"
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
",
                ReplyTo = contactRequest.Email,
                Tag = "contact-form"
            };

            // Send the email
            var response = await client.SendMessageAsync(message);

            if (response.Status == PostmarkStatus.Success)
            {
                _logger.LogInformation("Contact email sent successfully via Postmark from {Email}. Message ID: {MessageId}",
                    contactRequest.Email, response.Message);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send email via Postmark. Status: {Status}, Message: {Message}",
                    response.Status, response.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact email from {Email}", contactRequest.Email);
            return false;
        }
    }
}