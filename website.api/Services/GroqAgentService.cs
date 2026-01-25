using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using website.api.Models;

namespace website.api.Services;

public class GroqAgentService
{
    private readonly HttpClient _httpClient;
    private readonly IMcpService _mcpService;
    private readonly IProjectService _projectService;
    private readonly string _apiKey;
    private readonly ILogger<GroqAgentService> _logger;
    
    private const string BaseUrl = "https://api.groq.com/openai/v1/chat/completions";

    public GroqAgentService(
        HttpClient httpClient, 
        IMcpService mcpService, 
        IProjectService projectService,
        IConfiguration config,
        ILogger<GroqAgentService> logger)
    {
        _httpClient = httpClient;
        _mcpService = mcpService;
        _projectService = projectService;
        _apiKey = config["Groq:ApiKey"] ?? ""; // Allow empty, check at runtime
        _logger = logger;
    }

    public async Task<string> ChatAsync(string userMessage, List<MessageHistoryItem> history, string model = "llama-3.3-70b-versatile")
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "⚠️ **Configuration Missing**: The Groq API Key is not set in the server configuration. Please ask Muhammad to add it to the `.env` file.";
        }

        _logger.LogInformation("Starting chat with Groq ({Model}). User message: {Message}", model, userMessage);

        // 1. Prepare Tools (OpenAI Format)
        var tools = new object[]
        {
            new {
                type = "function",
                function = new {
                    name = "search_projects",
                    description = "Search for projects by category, technology, or keyword. Use this when the user asks about specific projects or skills.",
                    parameters = new {
                        type = "object",
                        properties = new {
                            keyword = new { type = "string", description = "Keyword to search in project titles and descriptions" },
                            category = new { type = "string", description = "Project category: Personal, Professional, or Academic" },
                            technology = new { type = "string", description = "Technology or skill to filter by (e.g., Python, C#)" }
                        }
                    }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "submit_contact",
                    description = "Submit a contact form message to Muhammad. Use this when the user explicitly wants to send a message or get in touch.",
                    parameters = new {
                        type = "object",
                        properties = new {
                            name = new { type = "string", description = "User's full name" },
                            email = new { type = "string", description = "User's email address" },
                            subject = new { type = "string", description = "Message subject" },
                            message = new { type = "string", description = "The message content" },
                            company = new { type = "string", description = "Company name (optional)" },
                            role = new { type = "string", description = "User's role (optional)" }
                        },
                        required = new[] { "name", "email", "subject", "message" }
                    }
                }
            }
        };

        // 2. Prepare Context
        var allProjects = await _projectService.GetProjectsAsync();
        var systemInstruction = 
            "You are the interactive portfolio assistant for Muhammad Kashif-Khan (MkKai). " +
            "Your goal is to help recruiters and visitors navigate his portfolio, understand his skills, and contact him.\n\n" +
            "CORE BEHAVIORS:\n" +
            "- Be professional, enthusiastic, and concise.\n" +
            "- You have direct access to tool capabilities. USE THEM. If a user asks about Python, search for Python projects.\n" +
            "- If a user wants to contact Muhammad, guide them to provide the necessary info and call the submit_contact tool.\n" +
            "- When showing projects, summarize them effectively and provide the ProjectUrl.\n" +
            "- DO NOT hallucinate projects. Use the search_projects tool to find facts.\n\n" +
            "PORTFOLIO DATA CONTEXT:\n" +
            JsonSerializer.Serialize(allProjects);

        // 3. Build Messages
        var messages = new List<object>
        {
            new { role = "system", content = systemInstruction }
        };

        foreach (var item in history)
        {
            // Map 'model' role to 'assistant' for OpenAI API compatibility
            var role = item.Role == "model" ? "assistant" : item.Role;
            messages.Add(new { role = role, content = item.Content });
        }
        
        messages.Add(new { role = "user", content = userMessage });

        // 4. Main Interaction Loop
        int maxTurns = 5;
        int currentTurn = 0;

        while (currentTurn < maxTurns)
        {
            var requestBody = new
            {
                model = model,
                messages = messages,
                tools = tools,
                tool_choice = "auto"
            };

            // Create request
            var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = JsonContent.Create(requestBody);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Groq API Error: {StatusCode} - {Error}", response.StatusCode, error);
                
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    return "🚫 **Groq Rate Limit Reached**: Please switch back to Gemini or try another model!";
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                     return $"⚠️ **Model Not Found**: The model `{model}` might not be available or the API key is invalid. Please check your settings.";
                }

                return $"I encountered an error with the Groq brain ({response.StatusCode}). Check logs for details.";
            }

            var resultModel = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>();
            var choice = resultModel?.Choices?.FirstOrDefault();
            var message = choice?.Message;

            // Case A: Tool Calls
            if (message?.ToolCalls != null && message.ToolCalls.Count > 0)
            {
                // Add assistant's tool call message
                messages.Add(new { 
                    role = "assistant", 
                    content = message.Content,
                    tool_calls = message.ToolCalls
                });

                foreach (var toolCall in message.ToolCalls)
                {
                    _logger.LogInformation("Groq requested tool: {ToolName}", toolCall.Function.Name);

                    // Execute via MCP Service
                    var mcpRequest = new McpRequest
                    {
                        Method = "tools/call",
                        Id = Guid.NewGuid().ToString(),
                        Params = new Dictionary<string, object>
                        {
                            ["name"] = toolCall.Function.Name,
                            ["arguments"] = JsonSerializer.Deserialize<Dictionary<string, object>>(toolCall.Function.Arguments) 
                                            ?? new Dictionary<string, object>()
                        }
                    };

                    var mcpResponse = await _mcpService.HandleRequestAsync(mcpRequest);
                    var toolResult = mcpResponse.Result;

                    // Add tool response
                    messages.Add(new {
                        role = "tool",
                        tool_call_id = toolCall.Id,
                        content = JsonSerializer.Serialize(toolResult)
                    });
                }

                currentTurn++;
                continue; // Loop back
            }

            // Case B: Text Response
            if (!string.IsNullOrEmpty(message?.Content))
            {
                return message.Content;
            }

            break;
        }

        return "I'm not sure how to respond to that.";
    }
}

// Helper Models for OpenAI/Groq API
public class OpenAiChatResponse
{
    [JsonPropertyName("choices")]
    public List<OpenAiChoice>? Choices { get; set; }
}

public class OpenAiChoice
{
    [JsonPropertyName("message")]
    public OpenAiMessage? Message { get; set; }
}

public class OpenAiMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "assistant";

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<OpenAiToolCall>? ToolCalls { get; set; }
}

public class OpenAiToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OpenAiFunction Function { get; set; } = new();
}

public class OpenAiFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "{}";
}
