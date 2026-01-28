using System.Text.Json;
using System.Text.Json.Serialization;
using website.api.Models;
using website.api.Services;

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

    public McpService(IServiceScopeFactory scopeFactory, ILogger<McpService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<McpResponse> HandleRequestAsync(McpRequest request)
    {
        _logger.LogInformation("MCP Request: {Method} (ID: {Id})", request.Method, request.Id);

        try
        {
            var response = request.Method switch
            {
                "initialize" => HandleInitialize(request),
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
                uri = "portfolio://ethos",
                name = "Professional Ethos",
                description = "Philosophy on technology, responsibility, and human-AI collaboration",
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
            var projectId = project.Title.ToLower().Replace(" ", "-").Replace("&", "and");
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
            "portfolio://profile" => new McpResourceContent
            {
                Uri = uri,
                MimeType = "application/json",
                Text = JsonSerializer.Serialize(new
                {
                    name = "Muhammad Kashif-Khan",
                    title = "4th Year CS & AI Student | Aspiring AI/ML Engineer",
                    university = "University of Leeds",
                    degree = "MEng/BSc Computer Science with Artificial Intelligence",
                    graduationYear = "2026",
                    expectedGrade = "First-Class Honours",
                    skills = new[]
                    {
                        "Python", "C", "Java", "SQL", "Bash",
                        "Git & GitHub", "Docker", "VS Code", "Gradle",
                        "PyQt6", "Matplotlib", "Jupyter", "Anaconda",
                        "Microsoft Azure", "AI Services", "ML on Cloud"
                    },
                    interests = new[]
                    {
                        "Artificial Intelligence", "Machine Learning", "Healthcare Technology",
                        "Data Analysis", "Software Architecture", "Algorithm Design"
                    }
                }, new JsonSerializerOptions { WriteIndented = true })
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
                    portfolio = "https://your-portfolio-url.com",
                    responseTime = "24-48 hours"
                }, new JsonSerializerOptions { WriteIndented = true })
            },
            "portfolio://about" => new McpResourceContent
            {
                Uri = uri,
                MimeType = "application/json",
                Text = JsonSerializer.Serialize(new
                {
                    background = "Computer Science student at University of Leeds with strong focus on AI, medical technology, and multidisciplinary problem-solving",
                    education = "BSc Computer Science, University of Leeds (2022-2025)",
                    specializations = new[] { "AI & Machine Learning", "Medical Technology", "Scientific Computing", "Distributed Systems", "Graphics Programming" },
                    technicalStrengths = new[] { "Python", "C/C++", "C#", "CUDA", "OpenMP", "MPI", "OpenCL", "GPT-4 API", "ROS2", "OpenGL" },
                    domains = new[] { "Healthcare AI", "Medical Imaging", "Robotics", "CFD Simulation", "Computer Vision", "High Performance Computing" },
                    philosophy = "Passionate about leveraging AI responsibly to solve real-world problems, particularly in healthcare and scientific domains",
                    academicFocus = "Strong academic performance with emphasis on practical applications and ethical technology development",
                    note = "Portfolio developed during a period of long-term illness, demonstrating resilience and commitment to learning"
                }, new JsonSerializerOptions { WriteIndented = true })
            },
            "portfolio://ethos" => new McpResourceContent
            {
                Uri = uri,
                MimeType = "application/json",
                Text = JsonSerializer.Serialize(new
                {
                    title = "Professional Ethos & Philosophy",
                    coreValues = new[] { "Responsible AI Development", "Human-Centered Technology", "Ethical Innovation", "Transparency", "Continuous Learning" },
                    aiPhilosophy = "AI should augment human capabilities rather than replace human judgment, with transparency and ethical considerations at the forefront",
                    responsibilityPrinciples = new[] {
                        "Privacy-first design with GDPR compliance",
                        "Transparent AI systems with explainable outputs",
                        "Inclusive technology that serves diverse populations",
                        "Environmental consciousness in computing practices"
                    },
                    collaborationApproach = "Believe in multidisciplinary teams where AI expertise combines with domain knowledge for meaningful impact",
                    innovationMindset = "Focus on solving real problems rather than pursuing technology for its own sake",
                    learningCommitment = "Committed to lifelong learning and staying current with emerging technologies while maintaining ethical standards"
                }, new JsonSerializerOptions { WriteIndented = true })
            },
            "portfolio://ai-integration" => new McpResourceContent
            {
                Uri = uri,
                MimeType = "application/json",
                Text = JsonSerializer.Serialize(new
                {
                    title = "AI Integration Methodology",
                    approach = "Strategic integration of AI tools to enhance development productivity while maintaining code quality and understanding",
                    experience = new[] {
                        "Extensive use of Claude Code Pro for complex architectural decisions",
                        "GPT-4 API integration for healthcare applications",
                        "AI-assisted development for rapid prototyping and learning",
                        "Comparative analysis of different AI coding assistants"
                    },
                    principles = new[] {
                        "AI as a collaborative partner, not a replacement for understanding",
                        "Always verify and understand AI-generated code",
                        "Use AI to accelerate learning of new technologies and frameworks",
                        "Maintain human oversight for critical design decisions"
                    },
                    practicalApplications = new[] {
                        "Blazor WebAssembly portfolio development with Claude Code assistance",
                        "FastAPI backend development with AI-guided architecture",
                        "Complex distributed systems design with AI consultation",
                        "Medical AI system development with ethical AI integration"
                    },
                    futureVision = "See AI as transformative for development efficiency while emphasizing the irreplaceable value of human creativity, ethics, and domain expertise"
                }, new JsonSerializerOptions { WriteIndented = true })
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
            Text = JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = true })
        };
    }

    private async Task<McpResourceContent> GetProjectContent(string uri)
    {
        var projectId = uri.Replace("portfolio://projects/", "");
        
        using var scope = _scopeFactory.CreateScope();
        var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
        
        var projects = await projectService.GetProjectsAsync();

        var project = projects.FirstOrDefault(p =>
            p.Title.ToLower().Replace(" ", "-").Replace("&", "and") == projectId);

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
            Text = JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true })
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
                            ? "Contact message sent successfully! Muhammad will respond within 24-48 hours."
                            : "Failed to send contact message. Please try again or email directly at contact@mkkai.dev"
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
            ? JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = true })
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