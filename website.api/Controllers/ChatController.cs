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
    private readonly IDiscordService _discordService;
    private readonly IAccessStateService _accessState;
    private readonly OllamaService _ollamaService;
    private readonly IConfiguration _configuration;

    public ChatController(
        ILogger<ChatController> logger, 
        DeepSeekAgentService assistant,
        IDiscordService discordService,
        IAccessStateService accessState,
        OllamaService ollamaService,
        IConfiguration configuration)
    {
        _logger = logger;
        _assistant = assistant;
        _discordService = discordService;
        _accessState = accessState;
        _ollamaService = ollamaService;
        _configuration = configuration;
    }

    [HttpPost("request-access")]
    public async Task<IActionResult> RequestAccess([FromBody] AccessRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || 
            string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "All fields are required" });
        }

        var requestId = await _discordService.SendAccessRequestAsync(request);
        
        if (requestId != null)
        {
            return Ok(new { message = "Request sent successfully", requestId });
        }

        return StatusCode(500, new { message = "Failed to send request" });
    }

    [HttpGet("status/{id}")]
    public IActionResult GetStatus(string id)
    {
        var status = _accessState.GetStatus(id);
        return Ok(new { status = status.ToString() });
    }

    [HttpPost("callback/{id}")]
    public IActionResult SetReady(string id)
    {
        // SECURITY CHECK
        var apiKey = _configuration["BRIDGE_API_KEY"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            if (!Request.Headers.TryGetValue("X-Bridge-Key", out var providedKey) || providedKey != apiKey)
            {
                return Unauthorized();
            }
        }

        _accessState.SetStatus(id, AccessStatus.Ready);
        return Ok(new { message = "Status updated to Ready" });
    }

    [HttpGet("models")]
    public async Task<IActionResult> GetModels([FromQuery] string provider = "gemini")
    {
        if (provider.ToLower() == "ollama")
        {
            var models = await _ollamaService.GetModelsAsync();
            return Ok(new { models });
        }
        
        // Return default models for other providers if needed, or just standard list logic
        // For now, only dynamic list is for Ollama
        return Ok(new { models = new List<string>() });
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
