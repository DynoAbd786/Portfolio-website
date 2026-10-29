using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using website.api.Models;
using website.api.Services;
using HomeContent = website.Data.HomeContent;

namespace website.api.Services;

public interface IMcpService
{
    Task<McpResponse> HandleRequestAsync(McpRequest request);
    
    // SSE Bridge Methods
    void RegisterConnection(string sessionId, HttpResponse response, bool isBridge);
    void RemoveConnection(string sessionId);
    bool IsBridgeConnected();
    Task<OllamaBridgeResponse> SendChatRequestAsync(string model, List<Models.MessageHistoryItem> history, string message, List<McpTool>? tools = null, object? options = null);
    void HandleChatResponse(string callbackId, string response, string model, List<OllamaToolCall>? toolCalls = null, bool toolsDisabled = false);
    void HandleChatError(string callbackId, string error);
    Task<List<string>> GetModelsAsync();
    void HandleModelsResponse(string callbackId, List<string> models);
    List<McpTool> GetTools();
}

public class OllamaBridgeResponse
{
    public string Response { get; set; } = "";
    public List<OllamaToolCall>? ToolCalls { get; set; }
    public bool ToolsDisabled { get; set; }
}

public class OllamaToolCall
{
    [JsonPropertyName("function")]
    public OllamaFunction Function { get; set; } = new();
}

public class OllamaFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("arguments")]
    public Dictionary<string, object>? Arguments { get; set; }
}

public class McpService : IMcpService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<McpService> _logger;

    // SSE State
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, SseClient> _connectedClients = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, TaskCompletionSource<OllamaBridgeResponse>> _pendingChatRequests = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, TaskCompletionSource<List<string>>> _pendingModelsRequests = new();

    public McpService(IServiceScopeFactory scopeFactory, ILogger<McpService> logger, ContactLimiter contactLimiter, IHttpContextAccessor http)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _contactLimiter = contactLimiter;
        _http = http;
    }

    // resources are read by agents, not put in a page: keep "C/C++" and "&" as they are
    private static readonly JsonSerializerOptions Readable = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly ContactLimiter _contactLimiter;
    private readonly IHttpContextAccessor _http;

    // A project's resource id is its page's name: /projects/hireforge is portfolio://projects/hireforge
    private static string ResourceId(Project project) => project.ProjectUrl.TrimEnd('/').Split('/').Last();

    public async Task<McpResponse> HandleRequestAsync(McpRequest request)
    {
        _logger.LogInformation("MCP Request: {Method} (ID: {Id})", request.Method, request.Id);

        try
        {
            var response = request.Method switch
            {
                "initialize" => HandleInitialize(request),
                "ping" => new McpResponse { Id = request.Id, Result = new { } },
                "notifications/initialized" => await HandleInitializedNotification(request),
                "notifications/cancelled" => HandleNotification(request),
                "resources/list" => await HandleResourcesList(request),
                "resources/read" => await HandleResourceRead(request),
                "tools/list" => HandleToolsList(request),
                "tools/call" => await HandleToolCall(request),
                _ when request.Method.StartsWith("notifications/") => HandleNotification(request),
                _ => CreateErrorResponse(request.Id, -32601, "Method not found")
            };

            _logger.LogInformation("MCP Service returning response for method {Method} with ID {ResponseId}",
                request.Method, response.Id);

            if (response.Error != null)
            {
                _logger.LogWarning("Response contains error: Code={ErrorCode}, Message={ErrorMessage}",
                    response.Error.Code, response.Error.Message);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request: {Method}", request.Method);
            return CreateErrorResponse(request.Id, -32603, "Internal error");
        }
    }

    private McpResponse HandleInitialize(McpRequest request)
    {
        _logger.LogInformation("=== INITIALIZE REQUEST START ===");
        _logger.LogInformation("Client protocol version: {ClientProtocol}",
            request.Params?.ToString()?.Contains("2025-06-18") == true ? "2025-06-18" : "unknown");

        var serverInfo = new
        {
            protocolVersion = "2025-06-18",
            capabilities = new
            {
                logging = new { },
                resources = new
                {
                    subscribe = false,
                    listChanged = false
                },
                tools = new
                {
                    listChanged = true
                },
                completion = new { },
                experimental = new { }
            },
            serverInfo = new
            {
                name = "muhammad-portfolio-mcp",
                version = "1.0.0"
            },
            instructions = "MCP server for Muhammad Kashif-Khan's portfolio. Use submit_contact to send messages and search_projects to find project information."
        };

        var response = new McpResponse
        {
            Id = request.Id,
            Result = serverInfo
        };

        _logger.LogInformation("Initialized with protocol {Protocol}", "2025-06-18");

        return response;
    }

    private async Task<McpResponse> HandleResourcesList(McpRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
        
        var projects = await projectService.GetProjectsAsync();
        var resources = new List<object>
        {
            new {
                uri = "portfolio://profile",
                name = "Professional Profile",
                description = "Muhammad Kashif-Khan's professional information and bio",
                mimeType = "application/json"
            },
            new {
                uri = "portfolio://contact",
                name = "Contact Information",
                description = "Contact details and professional links",
                mimeType = "application/json"
            },
            new {
                uri = "portfolio://about",
                name = "About Page",
                description = "Detailed background, education, and personal journey",
                mimeType = "application/json"
            },
            new {
                uri = "portfolio://ai-integration",
                name = "AI Integration Approach",
                description = "Methodology and philosophy for AI integration in development",
                mimeType = "application/json"
            },
            new {
                uri = "portfolio://projects/all",
                name = "All Projects",
                description = "Complete portfolio of projects across all categories",
                mimeType = "application/json"
            }
        };

        // Add individual project resources
        foreach (var project in projects)
        {
            var projectId = ResourceId(project);
            resources.Add(new {
                uri = $"portfolio://projects/{projectId}",
                name = project.Title,
                description = project.Description.Length > 100 ?
                    project.Description.Substring(0, 100) + "..." : project.Description,
                mimeType = "application/json"
            });
        }

        return new McpResponse
        {
            Id = request.Id,
            Result = new { resources }
        };
    }

    private async Task<McpResponse> HandleResourceRead(McpRequest request)
    {
        if (request.Params == null)
        {
            return CreateErrorResponse(request.Id, -32602, "Missing parameters");
        }

        var paramsJson = JsonSerializer.Serialize(request.Params);
        var readParams = JsonSerializer.Deserialize<Dictionary<string, object>>(paramsJson);

        if (readParams?.TryGetValue("uri", out var uriObj) != true || uriObj == null)
        {
            return CreateErrorResponse(request.Id, -32602, "Missing uri parameter");
        }

        var uri = uriObj.ToString();
        var content = await GetResourceContent(uri!);

        return new McpResponse
        {
            Id = request.Id,
            Result = new { contents = new[] { content } }
        };
    }

    private async Task<McpResourceContent> GetResourceContent(string uri)
    {
        return uri switch
        {
            // the same facts as the homepage (HomeContent), so the two never disagree
            "portfolio://profile" => new McpResourceContent
            {
                Uri = uri,
                MimeType = "application/json",
                Text = JsonSerializer.Serialize(new
                {
                    name = "Muhammad Kashif-Khan",
                    title = "Software Engineer | MEng CS & AI (First-Class), University of Leeds",
                    university = "University of Leeds",
                    degree = "MEng/BSc Computer Science with Artificial Intelligence",
                    graduationYear = "2026",
                    grade = "First-Class Honours",
                    openToWork = HomeContent.OpenToWork,
                    experience = HomeContent.Experience.Select(e => new { when = e.When, title = e.Title, organisation = e.Org, detail = e.Detail }),
                    building = HomeContent.Products.Select(p => new { name = p.Name, summary = p.Summary, tags = p.Tags, page = p.Href is null ? null : "https://mkkai.dev" + p.Href }),
                    stack = HomeContent.Stack.ToDictionary(g => g.Label, g => g.Items)
                }, Readable)
            },
            "portfolio://contact" => new McpResourceContent
            {
                Uri = uri,
                MimeType = "application/json",
                Text = JsonSerializer.Serialize(new
                {
                    email = "contact@mkkai.dev",
                    linkedin = "https://linkedin.com/in/dynoabd786",
                    github = "https://github.com/DynoAbd786",
                    portfolio = "https://mkkai.dev",
                    contactForm = "https://mkkai.dev/#contact"
                }, Readable)
            },
            "portfolio://about" => new McpResourceContent
            {
                Uri = uri,
                MimeType = "application/json",
                Text = JsonSerializer.Serialize(new
                {
                    background = "First-Class MEng Computer Science with AI graduate (University of Leeds, 2026) with a focus on AI, medical technology and multidisciplinary problem-solving",
                    education = "MEng Computer Science with Artificial Intelligence, University of Leeds (2022-2026), First-Class Honours",
                    specializations = new[] { "AI & Machine Learning", "Medical Technology", "Scientific Computing", "Distributed Systems", "Graphics Programming" },
                    technicalStrengths = HomeContent.Stack.SelectMany(g => g.Items),
                    domains = new[] { "Healthcare AI", "Medical Imaging", "Robotics", "CFD Simulation", "Computer Vision", "High Performance Computing" },
                    note = "Portfolio developed during a period of long-term illness, demonstrating resilience and commitment to learning"
                }, Readable)
            },
            // the same facts as the /ai-integration page
            "portfolio://ai-integration" => new McpResourceContent
            {
                Uri = uri,
                MimeType = "application/json",
                Text = JsonSerializer.Serialize(new
                {
                    assistant = "The site's Ask AI assistant runs on DeepSeek Flash, knows the site's profile and projects, looks projects up through this server's tools, and can pass a message on (name, email, subject, message).",
                    mcpServer = new
                    {
                        endpoint = "https://mkkai.dev/api/mcp",
                        sseEndpoint = "https://mkkai.dev/api/mcp/sse",
                        protocol = "JSON-RPC 2.0 over HTTP, no sign-in",
                        tools = new[] { "search_projects (keyword, category, technology)", "submit_contact (name, email, subject, message; company and role optional; a few an hour)" },
                        page = "https://mkkai.dev/ai-integration"
                    },
                    howIWork = "Writing code has got fast; deciding what to build hasn't. I put most of my time into understanding the problem and the design. I use AI agents every day, and I treat what they write like a colleague's pull request: I read it, test it, and don't ship what I can't explain."
                }, Readable)
            },
            "portfolio://projects/all" => await GetAllProjectsContent(uri),
            _ when uri.StartsWith("portfolio://projects/") => await GetProjectContent(uri),
            _ => new McpResourceContent
            {
                Uri = uri,
                MimeType = "text/plain",
                Text = "Resource not found"
            }
        };
    }

    private McpResponse HandleNotification(McpRequest request)
    {
        _logger.LogInformation("Recieved notification: {Method}", request.Method);
        // Notifications MUST NOT return a response to the client.
        // We return a response object with IsNotification = true so the controller knows not to send it.
        return new McpResponse { Id = null, Result = null };
    }

    private Task<McpResponse> HandleInitializedNotification(McpRequest request)
    {
        _logger.LogInformation("=== INITIALIZED NOTIFICATION ===");
        _logger.LogInformation("Client has completed initialization handshake");
        _logger.LogInformation("Tools available: submit_contact, search_projects");
        _logger.LogInformation("Client should now call tools/list to discover available tools");

        // Return empty response for notification (no result expected)
        return Task.FromResult(new McpResponse { Id = null, Result = null });
    }

    private async Task<McpResourceContent> GetAllProjectsContent(string uri)
    {
        using var scope = _scopeFactory.CreateScope();
        var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();

        var projects = await projectService.GetProjectsAsync();
        return new McpResourceContent
        {
            Uri = uri,
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(projects, Readable)
        };
    }

    private async Task<McpResourceContent> GetProjectContent(string uri)
    {
        var projectId = uri.Replace("portfolio://projects/", "");
        
        using var scope = _scopeFactory.CreateScope();
        var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
        
        var projects = await projectService.GetProjectsAsync();

        var project = projects.FirstOrDefault(p => ResourceId(p) == projectId);

        if (project == null)
        {
            return new McpResourceContent
            {
                Uri = uri,
                MimeType = "text/plain",
                Text = "Project not found"
            };
        }

        return new McpResourceContent
        {
            Uri = uri,
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(project, Readable)
        };
    }

    public List<McpTool> GetTools()
    {
        var tools = new List<McpTool>
        {
            new McpTool
            {
                Name = "submit_contact",
                Description = "Submit a contact form message to Muhammad",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Your full name" },
                        email = new { type = "string", description = "Your email address" },
                        company = new { type = "string", description = "Your company/organization (optional)" },
                        role = new { type = "string", description = "Your role/position (optional)" },
                        subject = new { type = "string", description = "Subject: Job Opportunity, Freelance Project, Collaboration, Consulting, General Inquiry, or Other" },
                        message = new { type = "string", description = "Your message" }
                    },
                    required = new[] { "name", "email", "subject", "message" }
                }
            },
            new McpTool
            {
                Name = "search_projects",
                Description = "Search for projects by category, technology, or keyword",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        category = new { type = "string", description = "Project category: Personal, Professional, or Academic" },
                        technology = new { type = "string", description = "Technology or skill to filter by" },
                        keyword = new { type = "string", description = "Keyword to search in project titles and descriptions" }
                    }
                }
            }
        };

        _logger.LogInformation("Returning {ToolCount} tools", tools.Count);
        return tools;
    }

    private McpResponse HandleToolsList(McpRequest request)
    {
        _logger.LogInformation("Listing tools for Request ID: {RequestId}", request.Id);
        var tools = GetTools();

        return new McpResponse
        {
            Id = request.Id,
            Result = new { tools }
        };
    }

    private async Task<McpResponse> HandleToolCall(McpRequest request)
    {
        _logger.LogInformation("Tool Call Request ID: {RequestId}", request.Id);

        var paramsJson = JsonSerializer.Serialize(request.Params);
        var toolCall = JsonSerializer.Deserialize<McpToolCall>(paramsJson);

        _logger.LogInformation("Executing tool: {ToolName}", toolCall?.Name);

        if (toolCall?.Name == null)
        {
            _logger.LogWarning("Tool call missing name - returning error");
            return CreateErrorResponse(request.Id, -32602, "Missing tool name");
        }

        _logger.LogInformation("Executing tool: {ToolName}", toolCall.Name);

        var result = toolCall.Name switch
        {
            "submit_contact" => await HandleSubmitContact(toolCall.Arguments),
            "search_projects" => await HandleSearchProjects(toolCall.Arguments),
            _ => new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "Unknown tool" }
                },
                IsError = true
            }
        };

        _logger.LogInformation("Tool execution result - IsError: {IsError}, ContentCount: {ContentCount}",
            result.IsError, result.Content?.Count ?? 0);

        var response = new McpResponse
        {
            Id = request.Id,
            Result = result
        };

        _logger.LogInformation("Tools call response created with ID: {ResponseId}", response.Id);
        _logger.LogInformation("=== TOOLS/CALL REQUEST COMPLETE ===");

        return response;
    }

    private async Task<McpToolResult> HandleSubmitContact(Dictionary<string, object>? arguments)
    {
        if (arguments == null)
        {
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "Missing contact arguments" }
                },
                IsError = true
            };
        }

        try
        {
            var contactRequest = new ContactRequest
            {
                Name = arguments.GetValueOrDefault("name")?.ToString() ?? "",
                Email = arguments.GetValueOrDefault("email")?.ToString() ?? "",
                Company = arguments.GetValueOrDefault("company")?.ToString(),
                Role = arguments.GetValueOrDefault("role")?.ToString(),
                Subject = arguments.GetValueOrDefault("subject")?.ToString() ?? "",
                Message = arguments.GetValueOrDefault("message")?.ToString() ?? ""
            };

            var missing = new[] { ("name", contactRequest.Name), ("email", contactRequest.Email), ("subject", contactRequest.Subject), ("message", contactRequest.Message) }
                .Where(f => string.IsNullOrWhiteSpace(f.Item2)).Select(f => f.Item1).ToList();
            if (missing.Count > 0)
                return ContactError($"Missing: {string.Join(", ", missing)}. Ask for these, then call submit_contact again.");
            if (!new EmailAddressAttribute().IsValid(contactRequest.Email))
                return ContactError("That email address doesn't look right. Ask for it again.");

            var visitor = _http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!_contactLimiter.TryTake(visitor))
            {
                _logger.LogWarning("MCP contact limit reached for {Visitor}", visitor);
                return ContactError("Too many messages in the last hour. Please try again later, or use the contact form at https://mkkai.dev/#contact.");
            }

            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var success = await emailService.SendContactEmailAsync(contactRequest);

            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent
                    {
                        Type = "text",
                        Text = success
                            ? "Message sent. Muhammad will reply by email."
                            : "The message couldn't be sent just now. Please try the contact form at https://mkkai.dev/#contact or email contact@mkkai.dev."
                    }
                },
                IsError = !success
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting contact via MCP");
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "Error processing contact submission" }
                },
                IsError = true
            };
        }
    }

    private static McpToolResult ContactError(string text) => new()
    {
        Content = new List<McpContent> { new McpContent { Type = "text", Text = text } },
        IsError = true
    };

    private async Task<McpToolResult> HandleSearchProjects(Dictionary<string, object>? arguments)
    {
        using var scope = _scopeFactory.CreateScope();
        var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
        var projects = await projectService.GetProjectsAsync();

        if (arguments != null)
        {
            var category = arguments.GetValueOrDefault("category")?.ToString();
            var technology = arguments.GetValueOrDefault("technology")?.ToString();
            var keyword = arguments.GetValueOrDefault("keyword")?.ToString();

            if (!string.IsNullOrEmpty(category))
            {
                if (Enum.TryParse<ProjectCategory>(category, true, out var cat))
                {
                    projects = projects.Where(p => p.Category == cat).ToList();
                }
            }

            if (!string.IsNullOrEmpty(technology))
            {
                projects = projects.Where(p =>
                    p.Tags.Any(tag => tag.Contains(technology, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                projects = projects.Where(p =>
                    p.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }

        var resultText = projects.Any()
            ? JsonSerializer.Serialize(projects, Readable)
            : "No projects found matching the search criteria.";

        return new McpToolResult
        {
            Content = new List<McpContent>
            {
                new McpContent { Type = "text", Text = resultText }
            }
        };
    }

    private static McpResponse CreateErrorResponse(object? id, int code, string message)
    {
        return new McpResponse
        {
            Id = id,
            Error = new McpError
            {
                Code = code,
                Message = message
            }
        };
    }

    // --- SSE BRIDGE IMPLEMENTATION ---

    public void RegisterConnection(string sessionId, HttpResponse response, bool isBridge)
    {
        var client = new SseClient(response, isBridge);
        _connectedClients.TryAdd(sessionId, client);
        _logger.LogInformation("Client registered in McpService. SessionId: {SessionId}, IsBridge: {IsBridge}", sessionId, isBridge);
    }

    public void RemoveConnection(string sessionId)
    {
        _connectedClients.TryRemove(sessionId, out _);
        _logger.LogInformation("Client removed from McpService. SessionId: {SessionId}", sessionId);
    }

    public bool IsBridgeConnected()
    {
        return _connectedClients.Values.Any(c => c.IsBridge);
    }

    public async Task<OllamaBridgeResponse> SendChatRequestAsync(string model, List<Models.MessageHistoryItem> history, string message, List<McpTool>? tools = null, object? options = null)
    {
        if (_connectedClients.IsEmpty)
        {
            throw new InvalidOperationException("No local bridge connected via SSE.");
        }

        // Use the first available BRIDGE client (User-to-HomePC 1:1 assumption for now)
        var client = _connectedClients.Values.FirstOrDefault(c => c.IsBridge);
        if (client == null) throw new InvalidOperationException("Bridge client unavailable.");

        var callbackId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<OllamaBridgeResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        // Timeout after 60 seconds
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        cts.Token.Register(() => 
        {
            _pendingChatRequests.TryRemove(callbackId, out _);
            tcs.TrySetException(new TimeoutException("Local LLM did not respond in time."));
        });

        _pendingChatRequests.TryAdd(callbackId, tcs);

        var payload = new
        {
            model = model,
            callback_id = callbackId,
            messages = history.Select(h => new { role = h.Role, content = h.Content }).Concat(new[] { new { role = "user", content = message } }),
            tools = tools,
            options = options
        };

        try 
        {
            await client.SendEventAsync("chat_request", JsonSerializer.Serialize(payload));
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            _pendingChatRequests.TryRemove(callbackId, out _);
            throw new Exception($"Failed to send request to bridge: {ex.Message}");
        }
    }

    public void HandleChatResponse(string callbackId, string response, string model, List<OllamaToolCall>? toolCalls = null, bool toolsDisabled = false)
    {
        if (_pendingChatRequests.TryRemove(callbackId, out var tcs))
        {
            tcs.TrySetResult(new OllamaBridgeResponse 
            { 
                Response = response,
                ToolCalls = toolCalls,
                ToolsDisabled = toolsDisabled
            });
        }
        else
        {
            _logger.LogWarning("Received chat response for unknown or expired callback ID: {CallbackId}", callbackId);
        }
    }

    public void HandleChatError(string callbackId, string error)
    {
        if (_pendingChatRequests.TryRemove(callbackId, out var tcs))
        {
            tcs.TrySetException(new Exception(error));
        }
    }

    public async Task<List<string>> GetModelsAsync()
    {
        if (_connectedClients.IsEmpty)
        {
            return new List<string>();
        }

        var client = _connectedClients.Values.FirstOrDefault(c => c.IsBridge);
        if (client == null) return new List<string>();

        var callbackId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<List<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        // Timeout after 15 seconds for model listing
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        cts.Token.Register(() => 
        {
            _pendingModelsRequests.TryRemove(callbackId, out _);
            tcs.TrySetResult(new List<string>()); // Empty list on timeout
        });

        _pendingModelsRequests.TryAdd(callbackId, tcs);

        try 
        {
            await client.SendEventAsync("models_request", JsonSerializer.Serialize(new { callback_id = callbackId }));
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            _pendingModelsRequests.TryRemove(callbackId, out _);
            _logger.LogError(ex, "Failed to send models request to bridge");
            return new List<string>();
        }
    }

    public void HandleModelsResponse(string callbackId, List<string> models)
    {
        if (_pendingModelsRequests.TryRemove(callbackId, out var tcs))
        {
            tcs.TrySetResult(models);
        }
        else
        {
            _logger.LogWarning("Received models response for unknown or expired callback ID: {CallbackId}", callbackId);
        }
    }

    private class SseClient
    {
        private readonly HttpResponse _response;
        public bool IsBridge { get; }

        public SseClient(HttpResponse response, bool isBridge)
        {
            _response = response;
            IsBridge = isBridge;
        }

        public async Task SendEventAsync(string eventType, string data)
        {
            try 
            {
                await _response.WriteAsync($"event: {eventType}\n");
                await _response.WriteAsync($"data: {data}\n\n");
                await _response.Body.FlushAsync();
            }
            catch
            {
                // Likely disconnected
            }
        }
    }
}