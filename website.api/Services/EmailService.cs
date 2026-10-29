using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using website.api.Models;

namespace website.api.Services;

public interface IEmailService
{
    Task<bool> SendContactEmailAsync(ContactRequest contactRequest);
}

// Emails contact messages to Muhammad through Resend (https://resend.com/docs/api-reference/emails/send-email)
public class EmailService : IEmailService
{
    private const string SendUrl = "https://api.resend.com/emails";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(HttpClient httpClient, IConfiguration configuration, ILogger<EmailService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendContactEmailAsync(ContactRequest contactRequest)
    {
        var apiKey = _configuration["Resend:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("Contact email from {Email} not sent: Resend__ApiKey is not set", contactRequest.Email);
            return false;
        }

        try
        {
            _logger.LogInformation("Contact form submission from {Name} <{Email}>: {Subject}",
                contactRequest.Name, contactRequest.Email, contactRequest.Subject);

            var request = new HttpRequestMessage(HttpMethod.Post, SendUrl)
            {
                Content = JsonContent.Create(new Dictionary<string, object>
                {
                    ["from"] = $"Portfolio contact form <{_configuration["Resend:FromEmail"]}>",
                    ["to"] = new[] { _configuration["ContactEmail"] },
                    ["reply_to"] = new[] { contactRequest.Email },
                    ["subject"] = $"[Portfolio Contact] {contactRequest.Subject}",
                    ["html"] = HtmlBody(contactRequest),
                    ["text"] = TextBody(contactRequest),
                }),
            };
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            using var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            // Resend answers a sent email with its id; anything else means it wasn't sent
            string? id = null;
            if (response.IsSuccessStatusCode)
            {
                try { id = JsonDocument.Parse(body).RootElement.TryGetProperty("id", out var v) ? v.GetString() : null; }
                catch (JsonException) { }
            }

            if (!string.IsNullOrEmpty(id))
            {
                _logger.LogInformation("Contact email from {Email} sent via Resend, id {Id}", contactRequest.Email, id);
                return true;
            }

            _logger.LogError("Resend did not send the contact email from {Email}: {Status} {Body}",
                contactRequest.Email, (int)response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact email from {Email}", contactRequest.Email);
            return false;
        }
    }

    private static string HtmlBody(ContactRequest c)
    {
        static string E(string? s) => WebUtility.HtmlEncode(s ?? "Not specified");
        return $@"
            <h2>New contact form submission from your portfolio website:</h2>
            <p><strong>Name:</strong> {E(c.Name)}</p>
            <p><strong>Email:</strong> <a href=""mailto:{E(c.Email)}"">{E(c.Email)}</a></p>
            <p><strong>Company:</strong> {E(c.Company)}</p>
            <p><strong>Role:</strong> {E(c.Role)}</p>
            <p><strong>Subject:</strong> {E(c.Subject)}</p>
            <p><strong>Message:</strong></p>
            <p>{E(c.Message).Replace("\n", "<br>")}</p>
            <hr>
            <p><em>This email was sent from your portfolio contact form.</em></p>
        ";
    }

    private static string TextBody(ContactRequest c) => $@"
New contact form submission from your portfolio website:

Name: {c.Name}
Email: {c.Email}
Company: {c.Company ?? "Not specified"}
Role: {c.Role ?? "Not specified"}
Subject: {c.Subject}

Message:
{c.Message}

---
This email was sent from your portfolio contact form.
";
}
