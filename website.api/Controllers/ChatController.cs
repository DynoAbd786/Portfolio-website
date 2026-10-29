using Microsoft.AspNetCore.Mvc;
using website.api.Models;
using website.api.Services;

namespace website.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly DeepSeekAgentService _assistant;

    public ChatController(ILogger<ChatController> logger, DeepSeekAgentService assistant)
    {
        _logger = logger;
        _assistant = assistant;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Message is required" });
            }

            // One model for everyone (DeepSeek Flash); any provider or model a client sends is ignored
            var history = request.History ?? new List<MessageHistoryItem>();
            if (history.Count > 20)
            {
                history = history.TakeLast(20).ToList();
            }

            var response = await _assistant.ChatAsync(request.Message, history);
            return Ok(new ChatResponse { Response = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return StatusCode(500, new { message = "An internal error occurred." });
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = "";
    public List<MessageHistoryItem>? History { get; set; }
    // Older clients still send these; the assistant always uses DeepSeek Flash
    public string? Provider { get; set; }
    public string? Model { get; set; }
}

public class ChatResponse
{
    public string Response { get; set; } = "";
}
