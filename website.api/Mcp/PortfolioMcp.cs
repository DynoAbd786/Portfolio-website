using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using website.api.Services;
using HomeContent = website.Data.HomeContent;

namespace website.api.Mcp;

// The MCP server's tools, on the official SDK (mapped at /api/mcp in Program.cs). The work is done by
// PortfolioTools, which the site's assistant shares; the annotations tell clients what each one does.
// Who's calling, for the analytics: the client's own name if it sent one (clientInfo), otherwise
// the first part of its User-Agent
internal static class McpCaller
{
    public static string Name(McpServer server, IHttpContextAccessor http) =>
        server.ClientInfo?.Name is { Length: > 0 } name ? name
        : http.HttpContext?.Request.Headers.UserAgent.ToString().Split(' ', 2)[0] is { Length: > 0 } agent ? agent
        : "unknown";
}

[McpServerToolType]
public sealed class PortfolioMcpTools
{
    private readonly PortfolioTools _tools;
    private readonly Analytics _analytics;
    private readonly IHttpContextAccessor _http;

    public PortfolioMcpTools(PortfolioTools tools, Analytics analytics, IHttpContextAccessor http)
    {
        _tools = tools;
        _analytics = analytics;
        _http = http;
    }

    [McpServerTool(Name = "search_projects", Title = "Search Muhammad's projects",
        ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Search Muhammad Kashif-Khan's projects. Every filter is optional; with none, all projects are returned. Each result has its page on mkkai.dev.")]
    public Task<ProjectSearchResult> SearchProjects(
        McpServer server,
        [Description("Words to look for in a project's title, description or result")] string? keyword = null,
        [Description("Personal, Professional or Academic")] string? category = null,
        [Description("A technology the project used, such as Python or C#")] string? technology = null)
    {
        _analytics.Track("mcp-tool", new Dictionary<string, string?>
        {
            ["tool"] = "search_projects", ["client"] = McpCaller.Name(server, _http), ["keyword"] = keyword, ["category"] = category, ["technology"] = technology,
        }, "/api/mcp");
        return _tools.SearchAsync(keyword, category, technology);
    }

    [McpServerTool(Name = "submit_contact", Title = "Send Muhammad a message",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Send Muhammad Kashif-Khan an email from someone who wants to get in touch. Ask the person for their name, email address, a subject and the message first. A few messages an hour at most.")]
    public async Task<CallToolResult> SubmitContact(
        McpServer server,
        [Description("The sender's full name")] string name,
        [Description("The sender's email address, for the reply")] string email,
        [Description("What it's about, such as Job Opportunity")] string subject,
        [Description("The message itself")] string message,
        [Description("The sender's company (optional)")] string? company = null,
        [Description("The sender's role (optional)")] string? role = null)
    {
        _analytics.Track("mcp-tool", new Dictionary<string, string?> { ["tool"] = "submit_contact", ["client"] = McpCaller.Name(server, _http) }, "/api/mcp");
        var outcome = await _tools.SubmitContactAsync("mcp", name, email, subject, message, company, role);
        return new CallToolResult { IsError = !outcome.Sent, Content = [new TextContentBlock { Text = outcome.Message }] };
    }
}

// The MCP server's resources: the profile is built from the same content as the homepage, so the two
// never disagree; projects are a template named after each page (portfolio://projects/hireforge).
[McpServerResourceType]
public sealed class PortfolioMcpResources
{
    private static readonly JsonSerializerOptions Readable = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static string Json(object value) => JsonSerializer.Serialize(value, Readable);
    private string Tracked(McpServer server, string uri, object value)
    {
        Read(server, uri);
        return Json(value);
    }

    private readonly IProjectService _projects;
    private readonly Analytics _analytics;
    private readonly IHttpContextAccessor _http;

    public PortfolioMcpResources(IProjectService projects, Analytics analytics, IHttpContextAccessor http)
    {
        _projects = projects;
        _analytics = analytics;
        _http = http;
    }

    private void Read(McpServer server, string uri) =>
        _analytics.Track("mcp-resource", new Dictionary<string, string?> { ["uri"] = uri, ["client"] = McpCaller.Name(server, _http) }, "/api/mcp");

    [McpServerResource(UriTemplate = "portfolio://profile", Name = "profile", Title = "Profile", MimeType = "application/json")]
    [Description("Who Muhammad is: education, experience, what he's building, the stack he works with, and whether he's open to work.")]
    public string Profile(McpServer server) => Tracked(server, "portfolio://profile", new
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
        stack = HomeContent.Stack.ToDictionary(g => g.Label, g => g.Items),
    });

    [McpServerResource(UriTemplate = "portfolio://contact", Name = "contact", Title = "Contact", MimeType = "application/json")]
    [Description("How to reach Muhammad.")]
    public string Contact(McpServer server) => Tracked(server, "portfolio://contact", new
    {
        email = "contact@mkkai.dev",
        linkedin = "https://linkedin.com/in/dynoabd786",
        github = "https://github.com/DynoAbd786",
        portfolio = "https://mkkai.dev",
        contactForm = "https://mkkai.dev/#contact",
    });

    [McpServerResource(UriTemplate = "portfolio://about", Name = "about", Title = "About", MimeType = "application/json")]
    [Description("Background and education.")]
    public string About(McpServer server) => Tracked(server, "portfolio://about", new
    {
        background = "First-Class MEng Computer Science with AI graduate (University of Leeds, 2026) with a focus on AI, medical technology and multidisciplinary problem-solving",
        education = "MEng Computer Science with Artificial Intelligence, University of Leeds (2022-2026), First-Class Honours",
        specializations = new[] { "AI & Machine Learning", "Medical Technology", "Scientific Computing", "Distributed Systems", "Graphics Programming" },
        technicalStrengths = HomeContent.Stack.SelectMany(g => g.Items),
        domains = new[] { "Healthcare AI", "Medical Imaging", "Robotics", "CFD Simulation", "Computer Vision", "High Performance Computing" },
        note = "Portfolio developed during a period of long-term illness, demonstrating resilience and commitment to learning",
    });

    [McpServerResource(UriTemplate = "portfolio://ai-integration", Name = "ai-integration", Title = "AI on this site", MimeType = "application/json")]
    [Description("The site's assistant and this MCP server, as described on mkkai.dev/ai-integration.")]
    public string AiIntegration(McpServer server) => Tracked(server, "portfolio://ai-integration", new
    {
        assistant = "The site's Ask AI assistant runs on DeepSeek Flash, knows the site's profile and projects, uses the same tools as this server, and can pass a message on (name, email, subject, message).",
        mcpServer = new
        {
            endpoint = "https://mkkai.dev/api/mcp",
            transport = "Streamable HTTP (stateless), no sign-in, built on the official MCP C# SDK",
            tools = new[] { "search_projects (keyword, category, technology; read-only)", "submit_contact (name, email, subject, message; company and role optional; a few an hour)" },
            page = "https://mkkai.dev/ai-integration",
        },
        howIWork = "Writing code has got fast; deciding what to build hasn't. I put most of my time into understanding the problem and the design. I use AI agents every day, and I treat what they write like a colleague's pull request: I read it, test it, and don't ship what I can't explain.",
    });

    [McpServerResource(UriTemplate = "portfolio://projects/all", Name = "projects", Title = "All projects", MimeType = "application/json")]
    [Description("Every project, with its page on mkkai.dev.")]
    public async Task<string> AllProjects(McpServer server)
    {
        Read(server, "portfolio://projects/all");
        return Json((await _projects.GetProjectsAsync()).Select(PortfolioTools.Summary));
    }

    [McpServerResource(UriTemplate = "portfolio://projects/{id}", Name = "project", Title = "One project", MimeType = "application/json")]
    [Description("One project by its page name: portfolio://projects/hireforge is mkkai.dev/projects/hireforge.")]
    public async Task<string> Project(McpServer server, [Description("The project's page name, such as hireforge or pnanolocz")] string id)
    {
        Read(server, $"portfolio://projects/{id}");
        var project = (await _projects.GetProjectsAsync())
            .FirstOrDefault(p => p.ProjectUrl.TrimEnd('/').Split('/').Last().Equals(id, StringComparison.OrdinalIgnoreCase));
        return project is null
            ? throw new McpProtocolException($"No project called '{id}'. portfolio://projects/all lists them.", McpErrorCode.ResourceNotFound)
            : Json(project);
    }
}
