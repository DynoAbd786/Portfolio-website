using Microsoft.AspNetCore.Mvc;
using website.api.Services;

namespace website.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly GeminiAgentService _geminiService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(GeminiAgentService geminiService, ILogger<ChatController> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
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

            // Limit history to last 20 messages to prevent token overflow
            var history = request.History ?? new List<MessageHistoryItem>();
            if (history.Count > 20)
            {
                history = history.TakeLast(20).ToList();
            }

            var response = await _geminiService.ChatAsync(request.Message, history);
            
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
}

public class ChatResponse
{
    public string Response { get; set; } = "";
}
