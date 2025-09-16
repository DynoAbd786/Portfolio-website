using Microsoft.AspNetCore.Mvc;
using website.api.Models;
using website.api.Services;

namespace website.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactController> _logger;

    public ContactController(IEmailService emailService, ILogger<ContactController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SendContactEmail([FromBody] ContactRequest contactRequest)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _emailService.SendContactEmailAsync(contactRequest);

            if (success)
            {
                _logger.LogInformation("Contact form submitted successfully by {Email}", contactRequest.Email);
                return Ok(new { message = "Email sent successfully!" });
            }
            else
            {
                _logger.LogWarning("Failed to send contact email for {Email}", contactRequest.Email);
                return StatusCode(500, new { message = "Failed to send email. Please try again later." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing contact form submission from {Email}", contactRequest?.Email);
            return StatusCode(500, new { message = "An error occurred while processing your request." });
        }
    }
}