using System.Collections.Generic;
using System.Threading.Tasks;

namespace website.Data
{
    public class ProjectService
    {
        public Task<List<Project>> GetProjectsAsync()
        {
            
            var projects = new List<Project>
            {
                new Project
                {
                    Title = "TaskFlow - Distributed Task Processing System",
                    Description = "Enterprise-grade distributed task processing framework with 34 Python modules spanning 10,000+ lines. Features Redis priority queues, PostgreSQL persistence, FastAPI REST APIs, WebSocket real-time updates, and Prometheus monitoring. Includes async/sync task execution, auto-scaling workers, circuit breakers, and comprehensive CLI with 20+ admin commands. Built to systematically compare agentic AI capabilities, revealing Claude Code Pro's superior architectural reasoning over Gemini CLI.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=TaskFlow",
                    ProjectUrl = "/projects/taskflow",
                    Tags = new[] { "Python", "Redis", "PostgreSQL", "FastAPI", "Distributed Systems", "Claude Code Pro", "Gemini CLI", "AI Research" }
                },
                new Project
                {
                    Title = "Personal Portfolio & C# Learning Journey",
                    Description = "Blazor WebAssembly portfolio built with .NET 8 and C# to master modern web development fundamentals. Features client-side rendering, responsive Tailwind CSS design, component architecture, dependency injection, and Font Awesome icons. Includes dynamic project showcase, mobile navigation, and 3D tilt effects via Vanilla Tilt.js. Developed entirely with Claude Code assistance to demonstrate AI-accelerated learning and document the C# development journey for employers and fellow developers.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Portfolio",
                    ProjectUrl = "/projects/ai-research",
                    Tags = new[] { "Blazor WebAssembly", ".NET 8", "C#", "Tailwind CSS", "Learning Journey", "Claude Code" }
                }
            };


            return Task.FromResult(projects);
        }
    }
}
