using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using website.api.Models;

namespace website.api.Services;

public class GeminiAgentService
{
    private readonly HttpClient _httpClient;
    private readonly IMcpService _mcpService;
    private readonly IProjectService _projectService;
    private readonly string _apiKey;
    private readonly ILogger<GeminiAgentService> _logger;
    
    // Using Gemini 3 Flash Preview (Confirmed availability)
    private const string ModelName = "gemini-3-flash-preview";
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public GeminiAgentService(
        HttpClient httpClient, 
        IMcpService mcpService, 
        IProjectService projectService,
        IConfiguration config,
        ILogger<GeminiAgentService> logger)
    {
        _httpClient = httpClient;
        _mcpService = mcpService;
        _projectService = projectService;
        _apiKey = config["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey configuration is missing");
        _logger = logger;
    }

    public async Task<string> ChatAsync(string userMessage, List<MessageHistoryItem> history)
    {
        _logger.LogInformation("Starting chat with Gemini 3 Flash. User message: {Message}", userMessage);

        // 1. Prepare Tools (mapped from McpService concepts)
        var tools = new
        {
            function_declarations = new object[]
            {
                new {
                    name = "search_projects",
                    description = "Search for projects by category, technology, or keyword. Use this when the user asks about specific projects or skills.",
                    parameters = new {
                        type = "OBJECT",
                        properties = new {
                            keyword = new { type = "STRING", description = "Keyword to search in project titles and descriptions" },
                            category = new { type = "STRING", description = "Project category: Personal, Professional, or Academic" },
                            technology = new { type = "STRING", description = "Technology or skill to filter by (e.g., Python, C#)" }
                        }
                    }
                },
                new {
                    name = "submit_contact",
                    description = "Submit a contact form message to Muhammad. Use this when the user explicitly wants to send a message or get in touch.",
                    parameters = new {
                        type = "OBJECT",
                        properties = new {
                            name = new { type = "STRING", description = "User's full name" },
                            email = new { type = "STRING", description = "User's email address" },
                            subject = new { type = "STRING", description = "Message subject" },
                            message = new { type = "STRING", description = "The message content" },
                            company = new { type = "STRING", description = "Company name (optional)" },
                            role = new { type = "STRING", description = "User's role (optional)" }
                        },
                        required = new[] { "name", "email", "subject", "message" }
                    }
                }
            }
        };

        // 2. Prepare Context (Long Context RAG)
        var allProjects = await _projectService.GetProjectsAsync();
        var systemInstruction = 
            "You are the interactive portfolio assistant for Muhammad Kashif-Khan (MkKai). " +
            "Your goal is to help recruiters and visitors navigate his portfolio, understand his skills, and contact him.\n\n" +
            "CORE BEHAVIORS:\n" +
            "- Be professional, enthusiastic, and concise.\n" +
            "- You have direct access to tool capabilities. USE THEM. If a user asks about Python, search for Python projects.\n" +
            "- If a user wants to contact Muhammad, guide them to provide the necessary info and call the submit_contact tool.\n" +
            "- When showing projects, summarize them effectively and provide the ProjectUrl.\n\n" +
            "PORTFOLIO DATA CONTEXT:\n" +
            JsonSerializer.Serialize(allProjects);

        // 3. Build Request with History
        var contents = new List<object>();
        
        // Add history (simplified for now)
        foreach (var item in history)
        {
            contents.Add(new { role = item.Role, parts = new[] { new { text = item.Content } } });
        }
        
        // Add current user message
        contents.Add(new { role = "user", parts = new[] { new { text = userMessage } } });

        // 4. Main Interaction Loop (Handle Function Calls)
        int maxTurns = 5;
        int currentTurn = 0;

        while (currentTurn < maxTurns)
        {
            var requestBody = new
            {
                system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                contents = contents,
                tools = new[] { tools }
            };

            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/{ModelName}:generateContent?key={_apiKey}", requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API Error: {Error}", error);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    return "🚫 **Daily Limit Reached**: My AI brain has exhausted its free resources for today. Please switch to the **Groq** provider in the settings above to continue chatting! ⚡";
                }

                return "I apologize, but I'm having trouble connecting to my brain right now. Please try again later.";
            }

            var resultModel = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            var candidate = resultModel?.Candidates?.FirstOrDefault();
            var contentPart = candidate?.Content?.Parts?.FirstOrDefault();

            // Case A: Function Call
            if (contentPart?.FunctionCall != null)
            {
                var fc = contentPart.FunctionCall;
                _logger.LogInformation("Gemini requested tool execution: {ToolName}", fc.Name);
                if (fc.ExtensionData != null)
                {
                   _logger.LogInformation("ExtensionData keys: {Keys}", string.Join(", ", fc.ExtensionData.Keys));
                }
                else 
                {
                   _logger.LogInformation("ExtensionData is NULL");
                }

                // Add model's tool call to history
                contents.Add(new { 
                    role = "model", 
                    parts = new[] { contentPart } 
                });

                // Execute via MCP Service
                var mcpRequest = new McpRequest
                {
                    Method = "tools/call",
                    Id = Guid.NewGuid().ToString(),
                    Params = new Dictionary<string, object>
                    {
                        ["name"] = fc.Name,
                        ["arguments"] = fc.Args
                    }
                };

                var mcpResponse = await _mcpService.HandleRequestAsync(mcpRequest);
                var toolResult = mcpResponse.Result; // This is the McpToolResult

                // Add tool response to history
                contents.Add(new {
                    role = "function",
                    parts = new[] { 
                        new { 
                            functionResponse = new {
                                name = fc.Name,
                                response = new { content = toolResult }
                            } 
                        } 
                    }
                });

                currentTurn++;
                continue; // Loop back to send tool output to Gemini
            }

            // Case B: Text Response
            if (contentPart?.Text != null)
            {
                return contentPart.Text;
            }

            break;
        }

        return "I'm not sure how to respond to that.";
    }
}

// Helper Models for Gemini API
public class MessageHistoryItem
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = "";
}

public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<Candidate>? Candidates { get; set; }
}

public class Candidate
{
    [JsonPropertyName("content")]
    public Content? Content { get; set; }
}

public class Content
{
    [JsonPropertyName("parts")]
    public List<Part>? Parts { get; set; }
}

public class Part
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("functionCall")]
    public FunctionCall? FunctionCall { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

public class FunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("args")]
    public Dictionary<string, object>? Args { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
