using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using website.api.Models;

namespace website.api.Services;

// The portfolio assistant: one model, DeepSeek Flash, through DeepSeek's OpenAI-compatible API
public class DeepSeekAgentService
{
    private readonly HttpClient _httpClient;
    private readonly PortfolioTools _tools;
    private readonly IProjectService _projectService;
    private readonly string _apiKey;
    private readonly ILogger<DeepSeekAgentService> _logger;
    
    private const string BaseUrl = "https://api.deepseek.com/chat/completions";
    public const string Model = "deepseek-flash";

    public DeepSeekAgentService(
        HttpClient httpClient, 
        PortfolioTools tools,
        IProjectService projectService,
        IConfiguration config,
        ILogger<DeepSeekAgentService> logger)
    {
        _httpClient = httpClient;
        _tools = tools;
        _projectService = projectService;
        _apiKey = config["DeepSeek:ApiKey"] ?? ""; // checked per request, so the site runs without it
        _logger = logger;
    }

    private const string Unavailable = "The assistant isn't available right now. You can email Muhammad at Abdua786@outlook.com and he'll get back to you.";

    public async Task<string> ChatAsync(string userMessage, List<MessageHistoryItem> history)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return Unavailable;
        }

        _logger.LogInformation("Starting chat with {Model}", Model);

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
        var systemInstruction = AssistantContext.SystemPrompt(allProjects);

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
                model = Model,
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
                _logger.LogError("DeepSeek API error: {StatusCode} - {Error}", response.StatusCode, error);
                return response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                    ? "The assistant is busy right now. Please try again in a minute."
                    : Unavailable;
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
                    _logger.LogInformation("DeepSeek requested tool: {ToolName}", toolCall.Function.Name);

                    messages.Add(new {
                        role = "tool",
                        tool_call_id = toolCall.Id,
                        content = await RunToolAsync(toolCall.Function.Name, toolCall.Function.Arguments)
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

    // The same tools as the MCP server (PortfolioTools), so the chat and outside agents agree
    private async Task<string> RunToolAsync(string name, string? argumentsJson)
    {
        Dictionary<string, JsonElement> args;
        try { args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson ?? "{}") ?? new(); }
        catch (JsonException) { return "The tool arguments weren't valid JSON."; }
        string? Arg(string key) => args.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        switch (name)
        {
            case "search_projects":
                var found = await _tools.SearchAsync(Arg("keyword"), Arg("category"), Arg("technology"));
                return found.Projects.Count == 0 ? "No projects match that." : JsonSerializer.Serialize(found);
            case "submit_contact":
                return (await _tools.SubmitContactAsync(Arg("name"), Arg("email"), Arg("subject"), Arg("message"), Arg("company"), Arg("role"))).Message;
            default:
                return $"There's no tool called {name}.";
        }
    }
}

// OpenAI-format response models (DeepSeek's API uses the same shape)
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
