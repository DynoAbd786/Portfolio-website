using Microsoft.AspNetCore.Mvc;
using website.api.Models;
using website.api.Services;

namespace website.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly GroqAgentService _groqService;
    private readonly GeminiAgentService _geminiService;
    private readonly IDiscordService _discordService;
    private readonly IAccessStateService _accessState;
    private readonly OllamaService _ollamaService;
    private readonly IConfiguration _configuration;

    public ChatController(
        ILogger<ChatController> logger, 
        GroqAgentService groqService, 
        GeminiAgentService geminiService,
        IDiscordService discordService,
        IAccessStateService accessState,
        OllamaService ollamaService,
        IConfiguration configuration)
    {
        _logger = logger;
        _groqService = groqService;
        _geminiService = geminiService;
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

            // Route based on provider
            var provider = request.Provider?.ToLower() ?? "gemini";

            // If local model, check access request ID
            if (provider == "ollama")
            {
                Request.Headers.TryGetValue("X-Access-RequestId", out var requestId);
                if (string.IsNullOrEmpty(requestId) || _accessState.GetStatus(requestId!) != AccessStatus.Ready)
                {
                    return Unauthorized(new { message = "Ollama access required." });
                }
                
                // Refresh session on every message
                _accessState.RefreshAccess(requestId!);
            }

            // Limit history to last 20 messages
            var history = request.History ?? new List<MessageHistoryItem>();
            if (history.Count > 20)
            {
                history = history.TakeLast(20).ToList();
            }

            string response;
            if (provider == "groq")
            {
                var model = !string.IsNullOrEmpty(request.Model) ? request.Model : "llama-3.3-70b-versatile";
                response = await _groqService.ChatAsync(request.Message, history, model);
            }
            else if (provider == "ollama")
            {
                var model = !string.IsNullOrEmpty(request.Model) ? request.Model : "llama3.2";
                response = await _ollamaService.ChatAsync(request.Message, history, model);
            }
            else
            {
                response = await _geminiService.ChatAsync(request.Message, history);
            }
            
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
    public string? Provider { get; set; } = "gemini"; // "gemini" or "groq"
    public string? Model { get; set; } = "gemini-3-flash";
}

public class ChatResponse
{
    public string Response { get; set; } = "";
}
