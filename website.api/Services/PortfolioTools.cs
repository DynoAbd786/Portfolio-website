using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using website.api.Models;

namespace website.api.Services;

public record ProjectSummary(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("page")] string Page,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("result")] string Result,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("tags")] string[] Tags,
    [property: JsonPropertyName("inDevelopment")] bool InDevelopment);

public record ProjectSearchResult([property: JsonPropertyName("projects")] IReadOnlyList<ProjectSummary> Projects);

public record ContactOutcome(bool Sent, string Message);

// What an agent can do with the site: search the projects and send Muhammad a message. The MCP server
// (Mcp/PortfolioMcp.cs) and the site's own assistant (DeepSeekAgentService) both call this, so they
// behave the same.
public class PortfolioTools
{
    private readonly IProjectService _projects;
    private readonly IEmailService _email;
    private readonly ContactLimiter _limiter;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<PortfolioTools> _logger;

    public PortfolioTools(IProjectService projects, IEmailService email, ContactLimiter limiter, IHttpContextAccessor http, ILogger<PortfolioTools> logger)
    {
        _projects = projects;
        _email = email;
        _limiter = limiter;
        _http = http;
        _logger = logger;
    }

    // name@domain.tld: EmailAddressAttribute only checks for an @, so "ada@nope" got through to the sender
    private static readonly Regex Email = new(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.Compiled);
    private static bool LooksLikeAnEmail(string email) => Email.IsMatch(email.Trim());

    public static ProjectSummary Summary(Project p) => new(
        p.Title, "https://mkkai.dev" + p.ProjectUrl, p.Category.ToString(), p.Result, p.Description, p.Tags, p.IsInDevelopment);

    // Every filter is optional; together they narrow the list
    public async Task<ProjectSearchResult> SearchAsync(string? keyword, string? category, string? technology)
    {
        IEnumerable<Project> found = await _projects.GetProjectsAsync();
        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<ProjectCategory>(category, true, out var cat))
            found = found.Where(p => p.Category == cat);
        if (!string.IsNullOrWhiteSpace(technology))
            found = found.Where(p => p.Tags.Any(t => t.Contains(technology, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(keyword))
            found = found.Where(p => p.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || p.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || p.Result.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        return new ProjectSearchResult(found.Select(Summary).ToList());
    }

    // Sends only a complete message, and only a few an hour per visitor (ContactLimiter)
    public async Task<ContactOutcome> SubmitContactAsync(string? name, string? email, string? subject, string? message, string? company, string? role)
    {
        var missing = new[] { ("name", name), ("email", email), ("subject", subject), ("message", message) }
            .Where(f => string.IsNullOrWhiteSpace(f.Item2)).Select(f => f.Item1).ToList();
        if (missing.Count > 0)
            return new(false, $"Missing: {string.Join(", ", missing)}. Ask for these, then try again.");
        if (!LooksLikeAnEmail(email!))
            return new(false, "That email address doesn't look right. Ask for it again.");

        var visitor = _http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!_limiter.TryTake(visitor))
        {
            _logger.LogWarning("Contact limit reached for {Visitor}", visitor);
            return new(false, "Too many messages in the last hour. Please try again later, or use the contact form at https://mkkai.dev/#contact.");
        }

        var sent = await _email.SendContactEmailAsync(new ContactRequest
        {
            Name = name!.Trim(), Email = email!.Trim(), Subject = subject!.Trim(), Message = message!.Trim(),
            Company = string.IsNullOrWhiteSpace(company) ? null : company.Trim(),
            Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim(),
        });
        return sent
            ? new(true, "Message sent. Muhammad will reply by email.")
            : new(false, "The message couldn't be sent just now. Please try the contact form at https://mkkai.dev/#contact or email contact@mkkai.dev.");
    }
}
