using System.Text;
using System.Text.Json;
using website.Data;
using Project = website.api.Models.Project;

namespace website.api.Services;

// What the assistant is told before every conversation: how to answer, then the same profile the
// homepage shows (HomeContent) and every project, so it never knows less than the site does
public static class AssistantContext
{
    public static string SystemPrompt(IEnumerable<Project> projects)
    {
        var s = new StringBuilder();
        s.AppendLine("You are the assistant on Muhammad Kashif-Khan's portfolio site, mkkai.dev. Most visitors are recruiters and engineers; help them find out what he has done and get in touch with him.");
        s.AppendLine();
        s.AppendLine("How to answer:");
        s.AppendLine("- Keep it short and plain: a few sentences or a short list. Write like a person, not a brochure.");
        s.AppendLine("- Only state facts from the profile and projects below, or from the search_projects tool. If something isn't there, say you don't know and suggest asking him directly. Never invent projects, numbers or dates.");
        s.AppendLine("- Link to pages with markdown links and their relative URL, for example [HireForge](/projects/hireforge); never a bare address like mkkai.dev/#contact. The contact form is at /#contact, all projects at /projects and the CV at /docs/Muhammad-Kashif-Khan-CV.pdf.");
        s.AppendLine("- If someone wants to send him a message, ask for their name, email, a subject and the message, then call submit_contact.");
        s.AppendLine("- Don't use em dashes.");
        s.AppendLine();
        s.AppendLine("PROFILE");
        s.AppendLine(HomeContent.OpenToWork
            ? "- He is open to work: software engineering and AI roles."
            : "- He isn't looking for a new role right now.");
        s.AppendLine("- He has graduated: a First-Class MEng in Computer Science with AI from the University of Leeds, in 2026.");
        s.AppendLine("Education and experience:");
        foreach (var e in HomeContent.Experience)
            s.AppendLine($"- {e.When}: {e.Title}, {e.Org}. {e.Detail}");
        s.AppendLine("What he's building in his own time:");
        foreach (var p in HomeContent.Products)
            s.AppendLine($"- {p.Name}: {p.Summary} Built with {string.Join(", ", p.Tags)}.{(p.Href is null ? "" : $" Page: {p.Href}")}");
        s.AppendLine("What he works with:");
        foreach (var g in HomeContent.Stack)
            s.AppendLine($"- {g.Label}: {string.Join(", ", g.Items)}");
        s.AppendLine();
        s.AppendLine("PROJECTS (JSON; ProjectUrl is each one's page)");
        s.Append(JsonSerializer.Serialize(projects));
        return s.ToString();
    }
}
