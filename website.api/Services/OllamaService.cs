using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace website.api.Services;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;
    private readonly IMcpService _mcpService;

    public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger, IConfiguration configuration, IMcpService mcpService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _mcpService = mcpService;
        
        var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<List<string>> GetModelsAsync()
    {
        // 1. Check for Bridge Connection (Scenario A: Remote Site -> Local PC)
        if (_mcpService.IsBridgeConnected())
        {
            _logger.LogInformation("Routing models request via SSE Bridge to registered client.");
            try 
            {
                return await _mcpService.GetModelsAsync();
            }
            catch (Exception bridgeEx)
            {
                 _logger.LogWarning("Bridge model discovery failed, falling back to local: {Message}", bridgeEx.Message);
            }
        }

        // 2. Fallback to Local/Same-Network Ollama (Scenario B: Local Dev / Same Server)
        try
        {
            var response = await _httpClient.GetAsync("/api/tags");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>();
            return content?.Models.Select(m => m.Name).ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Ollama models from local API");
            return new List<string>();
        }
    }

    public async Task<string> ChatAsync(string message, List<Models.MessageHistoryItem> history, string model)
    {
        // 1. Check for Bridge Connection (Scenario A: Remote Site -> Local PC)
        if (_mcpService.IsBridgeConnected())
        {
            _logger.LogInformation("Routing chat request via SSE Bridge to registered client.");
            try 
            {
                return await _mcpService.SendChatRequestAsync(model, history, message);
            }
            catch (Exception bridgeEx)
            {
                 _logger.LogWarning("Bridge execution failed, falling back to local: {Message}", bridgeEx.Message);
            }
        }

        // 2. Fallback to Local/Same-Network Ollama (Scenario B: Local Dev / Same Server)
        try
        {
            var messages = new List<object>();
            
            // Convert history to Ollama format
            foreach (var item in history)
            {
                messages.Add(new { role = item.Role, content = item.Content });
            }

            // Add current user message
            messages.Add(new { role = "user", content = message });

            var request = new
            {
                model = model,
                messages = messages,
                stream = false // For simplicity, we disable streaming for now
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/chat", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>();
            return result?.Message?.Content ?? "No response from Ollama.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ollama chat API");
            return $"Error connecting to Ollama: {ex.Message}";
        }
    }

    // Models for Ollama API responses
    private class OllamaTagsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModelInfo> Models { get; set; } = new();
    }

    private class OllamaModelInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    private class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }
    }

    private class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";
        
        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }
}
