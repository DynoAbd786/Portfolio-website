using System.Text.Json;
using website.api.Models;
using website.api.Services;

namespace website.api.Services;

public interface IMcpService
{
    Task<McpResponse> HandleRequestAsync(McpRequest request);
}

public class McpService : IMcpService
{
    private readonly IProjectService _projectService;
    private readonly IEmailService _emailService;
    private readonly ILogger<McpService> _logger;

    public McpService(IProjectService projectService, IEmailService emailService, ILogger<McpService> logger)
    {
        _projectService = projectService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<McpResponse> HandleRequestAsync(McpRequest request)
    {
        _logger.LogInformation("=== MCP SERVICE REQUEST ===");
        _logger.LogInformation("Method: {Method} | ID: {Id} | JsonRpc: {JsonRpc}",
            request.Method, request.Id, request.JsonRpc);
        _logger.LogInformation("Params: {Params}",
            request.Params != null ? JsonSerializer.Serialize(request.Params) : "null");

        try
        {
            var response = request.Method switch
            {
                "initialize" => HandleInitialize(request),
                "notifications/initialized" => new McpResponse { Id = request.Id, Result = new { } },
                "resources/list" => await HandleResourcesList(request),
                "resources/read" => await HandleResourceRead(request),
                "tools/list" => HandleToolsList(request),
                "tools/call" => await HandleToolCall(request),
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
                resources = new
                {
                    subscribe = false,
                    listChanged = false
                },
                tools = new
                {
                    listChanged = false
                }
            },
            serverInfo = new
            {
                name = "muhammad-portfolio-mcp",
                version = "1.0.0"
            }
        };

        var response = new McpResponse
        {
            Id = request.Id,
            Result = serverInfo
        };

        // Log the exact initialize response being sent
        var jsonResponse = JsonSerializer.Serialize(response,
            new JsonSerializerOptions { WriteIndented = true });

        _logger.LogInformation("--- INITIALIZE RESPONSE JSON ---");
        _logger.LogInformation(jsonResponse);
        _logger.LogInformation("--------------------------------");

        return response;
    }

    private async Task<McpResponse> HandleResourcesList(McpRequest request)
    {
        var projects = await _projectService.GetProjectsAsync();
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

    private async Task<McpResourceContent> GetAllProjectsContent(string uri)
    {
        var projects = await _projectService.GetProjectsAsync();
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
        var projects = await _projectService.GetProjectsAsync();

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

    private McpResponse HandleToolsList(McpRequest request)
    {
        _logger.LogInformation("=== TOOLS/LIST REQUEST START ===");
        _logger.LogInformation("Request ID: {RequestId}", request.Id);
        _logger.LogInformation("Request Method: {Method}", request.Method);

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

        _logger.LogInformation("Created {ToolCount} tools:", tools.Count);
        foreach (var tool in tools)
        {
            _logger.LogInformation("- Tool: {ToolName} | Description: {Description}", tool.Name, tool.Description);
        }

        var response = new McpResponse
        {
            Id = request.Id,
            Result = new { tools }
        };

        // Add this logging to see the exact output
        var jsonResponse = JsonSerializer.Serialize(response,
            new JsonSerializerOptions { WriteIndented = true });

        _logger.LogInformation("--- FINAL 'tools/list' JSON RESPONSE ---");
        _logger.LogInformation(jsonResponse);
        _logger.LogInformation("------------------------------------");

        _logger.LogInformation("Tools list response created with ID: {ResponseId}", response.Id);
        _logger.LogInformation("Response Result Type: {ResultType}", response.Result?.GetType().Name);
        _logger.LogInformation("=== TOOLS/LIST REQUEST COMPLETE ===");

        return response;
    }

    private async Task<McpResponse> HandleToolCall(McpRequest request)
    {
        _logger.LogInformation("=== TOOLS/CALL REQUEST START ===");
        _logger.LogInformation("Request ID: {RequestId}", request.Id);
        _logger.LogInformation("Request Method: {Method}", request.Method);
        _logger.LogInformation("Request Params: {Params}", JsonSerializer.Serialize(request.Params));

        var paramsJson = JsonSerializer.Serialize(request.Params);
        var toolCall = JsonSerializer.Deserialize<McpToolCall>(paramsJson);

        _logger.LogInformation("Parsed tool call - Name: {ToolName}", toolCall?.Name);
        _logger.LogInformation("Tool arguments: {Arguments}",
            toolCall?.Arguments != null ? JsonSerializer.Serialize(toolCall.Arguments) : "null");

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

            var success = await _emailService.SendContactEmailAsync(contactRequest);

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
        var projects = await _projectService.GetProjectsAsync();

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
}