using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using website.api.Models;

namespace website.api.Services;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;
    private readonly IMcpService _mcpService;
    private readonly IProjectService _projectService;

    public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger, IConfiguration configuration, IMcpService mcpService, IProjectService projectService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _mcpService = mcpService;
        _projectService = projectService;
        
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
        // Construct System Prompt with Condensed Context
        var allProjects = await _projectService.GetProjectsAsync();
        var condensedProjects = allProjects.Select(p => new {
            p.Title,
            p.Description,
            p.Tags,
            p.Category
        });

        var systemInstruction = 
            "You are the interactive portfolio assistant for Muhammad Kashif-Khan (MKKAI). " +
            "Your goal is to help recruiters and visitors navigate his portfolio, understand his skills, and explore his projects.\n\n" +
            "CORE BEHAVIORS:\n" +
            "- Be professional, enthusiastic, and concise.\n" +
            "- Use the project data below to answer questions accurately.\n" +
            "- If asked about skills or projects you don't see in the context, refer to his main portfolio at mkkai.dev.\n\n" +
            "PORTFOLIO DATA CONTEXT:\n" +
            JsonSerializer.Serialize(condensedProjects);

        // Prepend system instruction to history for Ollama
        var fullHistory = new List<Models.MessageHistoryItem> 
        { 
            new Models.MessageHistoryItem { Role = "system", Content = systemInstruction }
        };
        fullHistory.AddRange(history);
        
        // Add current user message to working list
        var workingHistory = new List<Models.MessageHistoryItem>(fullHistory);
        var currentInput = message;

        // Ollama Options to increase context window
        var ollamaOptions = new { num_ctx = 8192, temperature = 0.7 };

        // 1. Check for Bridge Connection (Scenario A: Remote Site -> Local PC)
        if (_mcpService.IsBridgeConnected())
        {
            _logger.LogInformation("Routing chat request via SSE Bridge with TOOL support and expanded context.");
            
            int maxTurns = 5;
            int currentTurn = 0;

            try 
            {
                var availableTools = _mcpService.GetTools();

                while (currentTurn < maxTurns)
                {
                    _logger.LogInformation("Bridge Turn {Turn} for model {Model}", currentTurn + 1, model);
                    
                    // We'll pass options in the payload if needed, but the bridge needs to be updated to use them
                    var response = await _mcpService.SendChatRequestAsync(model, workingHistory, currentInput, availableTools, ollamaOptions);
                    
                    // Case A: Tool Calls
                    if (response.ToolCalls != null && response.ToolCalls.Count > 0)
                    {
                        _logger.LogInformation("Bridge model requested {Count} tool calls", response.ToolCalls.Count);
                        
                        // Add model's tool call response to history
                        workingHistory.Add(new Models.MessageHistoryItem { Role = "user", Content = currentInput });
                        workingHistory.Add(new Models.MessageHistoryItem 
                        { 
                            Role = "assistant", 
                            Content = response.Response 
                        });

                        foreach (var toolCall in response.ToolCalls)
                        {
                            var mcpRequest = new McpRequest
                            {
                                Method = "tools/call",
                                Id = Guid.NewGuid().ToString(),
                                Params = new Dictionary<string, object>
                                {
                                    ["name"] = toolCall.Function.Name,
                                    ["arguments"] = toolCall.Function.Arguments ?? new Dictionary<string, object>()
                                }
                            };

                            var mcpResponse = await _mcpService.HandleRequestAsync(mcpRequest);
                            workingHistory.Add(new Models.MessageHistoryItem 
                            { 
                                Role = "tool", 
                                Content = JsonSerializer.Serialize(mcpResponse.Result)
                            });
                        }

                        currentTurn++;
                        currentInput = ""; 
                        continue;
                    }

                    // Case B: Final Text Response
                    return response.Response;
                }
            }
            catch (Exception bridgeEx)
            {
                 _logger.LogWarning("Bridge tool execution failed, falling back to simple local: {Message}", bridgeEx.Message);
            }
        }

        // 2. Fallback to Local/Same-Network Ollama (Scenario B: Local Dev / Same Server)
        try
        {
            var messages = new List<object>();
            foreach (var item in fullHistory) { messages.Add(new { role = item.Role, content = item.Content }); }
            messages.Add(new { role = "user", content = message });

            var request = new { 
                model = model, 
                messages = messages, 
                stream = false,
                options = ollamaOptions
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
            _logger.LogError(ex, "Error calling local Ollama fallback");
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
