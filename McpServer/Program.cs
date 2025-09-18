using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer;

public class Program
{
    public static async Task Main(string[] args)
    {
        var server = new PortfolioMcpServer();
        await server.RunAsync();
    }
}

public class PortfolioMcpServer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly PortfolioData _data = new();

    public async Task RunAsync()
    {
        // Write server info to stderr for debugging
        await Console.Error.WriteLineAsync("Muhammad Portfolio MCP Server starting...");

        try
        {
            while (true)
            {
                var line = await Console.In.ReadLineAsync();
                if (line == null) break;

                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var request = JsonSerializer.Deserialize<McpRequest>(line, JsonOptions);
                    if (request != null)
                    {
                        var response = await HandleRequestAsync(request);
                        var responseJson = JsonSerializer.Serialize(response, JsonOptions);
                        await Console.Out.WriteLineAsync(responseJson);
                        await Console.Out.FlushAsync();
                    }
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"Error processing request: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Server error: {ex.Message}");
        }
    }

    private async Task<McpResponse> HandleRequestAsync(McpRequest request)
    {
        return request.Method switch
        {
            "initialize" => HandleInitialize(request),
            "notifications/initialized" => new McpResponse { Id = request.Id, Result = new { } },
            "resources/list" => HandleResourcesList(request),
            "resources/read" => HandleResourceRead(request),
            "tools/list" => HandleToolsList(request),
            "tools/call" => await HandleToolCallAsync(request),
            _ => CreateErrorResponse(request.Id, -32601, "Method not found")
        };
    }

    private McpResponse HandleInitialize(McpRequest request)
    {
        var serverInfo = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                resources = new { },
                tools = new { }
            },
            serverInfo = new
            {
                name = "muhammad-portfolio-mcp",
                version = "1.0.0"
            }
        };

        return new McpResponse { Id = request.Id, Result = serverInfo };
    }

    private McpResponse HandleResourcesList(McpRequest request)
    {
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
        foreach (var project in _data.Projects)
        {
            resources.Add(new
            {
                uri = $"portfolio://projects/{project.Id}",
                name = project.Title,
                description = project.Description.Length > 100
                    ? project.Description.Substring(0, 100) + "..."
                    : project.Description,
                mimeType = "application/json"
            });
        }

        return new McpResponse { Id = request.Id, Result = new { resources } };
    }

    private McpResponse HandleResourceRead(McpRequest request)
    {
        if (request.Params?.TryGetValue("uri", out var uriValue) != true)
        {
            return CreateErrorResponse(request.Id, -32602, "Missing uri parameter");
        }

        var uri = uriValue?.ToString();
        var content = GetResourceContent(uri);

        return new McpResponse
        {
            Id = request.Id,
            Result = new { contents = new[] { content } }
        };
    }

    private object GetResourceContent(string? uri)
    {
        return uri switch
        {
            "portfolio://profile" => new
            {
                uri,
                mimeType = "application/json",
                text = JsonSerializer.Serialize(_data.Profile, JsonOptions)
            },
            "portfolio://contact" => new
            {
                uri,
                mimeType = "application/json",
                text = JsonSerializer.Serialize(_data.Contact, JsonOptions)
            },
            "portfolio://projects/all" => new
            {
                uri,
                mimeType = "application/json",
                text = JsonSerializer.Serialize(_data.Projects, JsonOptions)
            },
            var projectUri when projectUri?.StartsWith("portfolio://projects/") == true =>
                GetProjectContent(projectUri),
            _ => new
            {
                uri,
                mimeType = "text/plain",
                text = "Resource not found"
            }
        };
    }

    private object GetProjectContent(string uri)
    {
        var projectId = uri.Replace("portfolio://projects/", "");
        var project = _data.Projects.FirstOrDefault(p => p.Id == projectId);

        if (project == null)
        {
            return new
            {
                uri,
                mimeType = "text/plain",
                text = "Project not found"
            };
        }

        return new
        {
            uri,
            mimeType = "application/json",
            text = JsonSerializer.Serialize(project, JsonOptions)
        };
    }

    private McpResponse HandleToolsList(McpRequest request)
    {
        var tools = new object[]
        {
            new
            {
                name = "submit_contact",
                description = "Submit a contact form message to Muhammad",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Your full name" },
                        email = new { type = "string", description = "Your email address" },
                        company = new { type = "string", description = "Your company/organization (optional)" },
                        role = new { type = "string", description = "Your role/position (optional)" },
                        subject = new { type = "string", description = "Subject of your message" },
                        message = new { type = "string", description = "Your message" }
                    },
                    required = new[] { "name", "email", "subject", "message" }
                }
            },
            new
            {
                name = "search_projects",
                description = "Search for projects by category, technology, or keyword",
                inputSchema = new
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

        return new McpResponse { Id = request.Id, Result = new { tools } };
    }

    private Task<McpResponse> HandleToolCallAsync(McpRequest request)
    {
        if (request.Params?.TryGetValue("name", out var nameValue) != true)
        {
            return Task.FromResult(CreateErrorResponse(request.Id, -32602, "Missing tool name"));
        }

        var toolName = nameValue?.ToString();
        var arguments = request.Params?.TryGetValue("arguments", out var argsValue) == true
            ? argsValue as JsonElement? : null;

        var result = toolName switch
        {
            "submit_contact" => HandleSubmitContact(arguments),
            "search_projects" => HandleSearchProjects(arguments),
            _ => new
            {
                content = new[]
                {
                    new { type = "text", text = "Unknown tool" }
                },
                isError = true
            }
        };

        return Task.FromResult(new McpResponse { Id = request.Id, Result = result });
    }

    private object HandleSubmitContact(JsonElement? arguments)
    {
        if (arguments?.ValueKind != JsonValueKind.Object)
        {
            return new
            {
                content = new[] { new { type = "text", text = "Invalid arguments" } },
                isError = true
            };
        }

        var args = arguments.Value;
        var name = GetStringProperty(args, "name");
        var email = GetStringProperty(args, "email");
        var company = GetStringProperty(args, "company");
        var role = GetStringProperty(args, "role");
        var subject = GetStringProperty(args, "subject");
        var message = GetStringProperty(args, "message");

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(message))
        {
            return new
            {
                content = new[] { new { type = "text", text = "Missing required fields: name, email, subject, and message are required" } },
                isError = true
            };
        }

        // Log the contact submission
        Console.Error.WriteLine($"Contact submission from {name} ({email}): {subject}");

        var responseText = $"Contact message received successfully! Here are the details:\n\n" +
                          $"Name: {name}\n" +
                          $"Email: {email}\n" +
                          (!string.IsNullOrEmpty(company) ? $"Company: {company}\n" : "") +
                          (!string.IsNullOrEmpty(role) ? $"Role: {role}\n" : "") +
                          $"Subject: {subject}\n" +
                          $"Message: {message}\n\n" +
                          "Thank you for reaching out! Muhammad will respond within 24-48 hours.";

        return new
        {
            content = new[] { new { type = "text", text = responseText } }
        };
    }

    private object HandleSearchProjects(JsonElement? arguments)
    {
        var projects = _data.Projects.AsEnumerable();

        if (arguments?.ValueKind == JsonValueKind.Object)
        {
            var args = arguments.Value;

            var category = GetStringProperty(args, "category");
            if (!string.IsNullOrEmpty(category))
            {
                projects = projects.Where(p =>
                    string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            var technology = GetStringProperty(args, "technology");
            if (!string.IsNullOrEmpty(technology))
            {
                projects = projects.Where(p =>
                    p.Technologies.Any(t => t.Contains(technology, StringComparison.OrdinalIgnoreCase)));
            }

            var keyword = GetStringProperty(args, "keyword");
            if (!string.IsNullOrEmpty(keyword))
            {
                projects = projects.Where(p =>
                    p.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }
        }

        var filteredProjects = projects.ToList();
        var resultText = filteredProjects.Any()
            ? JsonSerializer.Serialize(filteredProjects, JsonOptions)
            : "No projects found matching the search criteria.";

        return new
        {
            content = new[] { new { type = "text", text = resultText } }
        };
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private static McpResponse CreateErrorResponse(string? id, int code, string message)
    {
        return new McpResponse
        {
            Id = id,
            Error = new { code, message }
        };
    }
}

// Data models
public class PortfolioData
{
    public ProfileInfo Profile { get; } = new();
    public ContactInfo Contact { get; } = new();
    public List<ProjectInfo> Projects { get; } = new()
    {
        new ProjectInfo
        {
            Id = "health-data-analyzer",
            Title = "Health Data Analyzer",
            Description = "AI-powered healthcare data analysis tool using machine learning to identify patterns and trends in medical data. Built for healthcare professionals to analyze patient data efficiently.",
            Category = "Professional",
            Technologies = new() { "Python", "Machine Learning", "Healthcare AI", "Data Analysis", "Scikit-learn" },
            Status = "Completed"
        },
        new ProjectInfo
        {
            Id = "smart-portfolio-website",
            Title = "Smart Portfolio Website",
            Description = "Modern, responsive portfolio website built with Blazor WebAssembly featuring dynamic content, MCP integration, and real-time contact form functionality.",
            Category = "Personal",
            Technologies = new() { "Blazor", "C#", ".NET 8", "WebAssembly", "MCP", "Bootstrap" },
            Status = "In Progress"
        },
        new ProjectInfo
        {
            Id = "university-research-project",
            Title = "AI Neural Network Optimization Research",
            Description = "Advanced AI research project focusing on neural network optimization and performance enhancement using novel algorithms and architectures.",
            Category = "Academic",
            Technologies = new() { "Python", "TensorFlow", "PyTorch", "Research", "Neural Networks" },
            Status = "Ongoing"
        },
        new ProjectInfo
        {
            Id = "automated-testing-framework",
            Title = "Automated Testing Framework",
            Description = "Comprehensive testing framework for web applications with CI/CD integration, automated test generation, and detailed reporting capabilities.",
            Category = "Professional",
            Technologies = new() { "C#", "Selenium", "Azure DevOps", "Docker", "Test Automation" },
            Status = "Completed"
        },
        new ProjectInfo
        {
            Id = "data-visualization-dashboard",
            Title = "Interactive Data Visualization Dashboard",
            Description = "Real-time data visualization dashboard for business analytics with interactive charts, filters, and export functionality.",
            Category = "Personal",
            Technologies = new() { "Python", "Matplotlib", "Jupyter", "Data Visualization", "Pandas" },
            Status = "Completed"
        }
    };
}

public class ProfileInfo
{
    public string Name { get; set; } = "Muhammad Kashif-Khan";
    public string Title { get; set; } = "4th Year CS & AI Student | Aspiring AI/ML Engineer";
    public string University { get; set; } = "University of Leeds";
    public string Degree { get; set; } = "MEng/BSc Computer Science with Artificial Intelligence";
    public string GraduationYear { get; set; } = "2026";
    public string ExpectedGrade { get; set; } = "First-Class Honours";
    public List<string> Skills { get; set; } = new()
    {
        "Python", "C", "Java", "SQL", "Bash", "Git & GitHub", "Docker", "VS Code", "Gradle",
        "PyQt6", "Matplotlib", "Jupyter", "Anaconda", "Microsoft Azure", "AI Services", "ML on Cloud"
    };
    public List<string> Interests { get; set; } = new()
    {
        "Artificial Intelligence", "Machine Learning", "Healthcare Technology",
        "Data Analysis", "Software Architecture", "Algorithm Design"
    };
}

public class ContactInfo
{
    public string Email { get; set; } = "contact@mkkai.dev";
    public string LinkedIn { get; set; } = "https://linkedin.com/in/dynoabd786";
    public string GitHub { get; set; } = "https://github.com/DynoAbd786";
    public string Portfolio { get; set; } = "https://your-portfolio-url.com";
    public string ResponseTime { get; set; } = "24-48 hours";
}

public class ProjectInfo
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public List<string> Technologies { get; set; } = new();
    public string Status { get; set; } = "";
}

// MCP Protocol models
public class McpRequest
{
    public string? Id { get; set; }
    public string Method { get; set; } = "";
    public Dictionary<string, object>? Params { get; set; }
}

public class McpResponse
{
    public string? Id { get; set; }
    public object? Result { get; set; }
    public object? Error { get; set; }
}
