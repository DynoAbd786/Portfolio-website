using System.Net.Http.Json;

namespace website.Data
{
    public class ProjectService
    {
        private readonly HttpClient _httpClient;

        public ProjectService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Project>> GetProjectsAsync()
        {
            try
            {
                var projects = await _httpClient.GetFromJsonAsync<List<Project>>("api/projects");
                return projects ?? new List<Project>();
            }
            catch (Exception ex)
            {
                // Fallback to hardcoded data if API is not available
                Console.WriteLine($"API call failed: {ex.Message}. Using fallback data.");
                return GetFallbackProjects();
            }
        }

        private static List<Project> GetFallbackProjects()
        {
            return new List<Project>
            {
                new Project
                {
                    Title = "TaskFlow - Distributed Task Processing System",
                    Description = "Enterprise-grade distributed task processing framework with 34 Python modules spanning 10,000+ lines. Features Redis priority queues, PostgreSQL persistence, FastAPI REST APIs, WebSocket real-time updates, and Prometheus monitoring. Includes async/sync task execution, auto-scaling workers, circuit breakers, and comprehensive CLI with 20+ admin commands. Built to systematically compare agentic AI capabilities, revealing Claude Code Pro's superior architectural reasoning over Gemini CLI.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=TaskFlow",
                    ProjectUrl = "/projects/taskflow",
                    Tags = new[] { "Python", "Redis", "PostgreSQL", "FastAPI", "Distributed Systems", "Claude Code Pro", "Gemini CLI", "AI Research" },
                    Category = ProjectCategory.Personal
                },
                new Project
                {
                    Title = "Personal Portfolio & C# Learning Journey",
                    Description = "Comprehensive C# and .NET portfolio showcasing modern development with Blazor WebAssembly frontend and ASP.NET Core Web API backend. Features advanced C# language concepts, async/await patterns, dependency injection, component architecture, and professional email integration via Postmark. Includes custom Model Context Protocol (MCP) server implementation enabling AI agents to interact with portfolio data using JSON-RPC 2.0 protocol - successfully tested with MCP Inspector but Claude Desktop integration still under development. Built with Claude Code assistance to demonstrate both full-stack web development and cutting-edge AI integration capabilities.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Portfolio",
                    ProjectUrl = "/projects/ai-research",
                    Tags = new[] { "C#", ".NET 8", "Blazor WebAssembly", "ASP.NET Core", "Web API", "MCP", "JSON-RPC", "AI Integration", "Postmark", "Claude Code" },
                    Category = ProjectCategory.Personal
                },
                new Project
                {
                    Title = "Flow Diverter Deployment Algorithm & Medical Simulation Platform",
                    Description = "Advanced biomedical research platform for simulating flow diverter stent deployment in patient-specific vascular geometries. Built during 8-week internship, extending the 2016 Paliwal Virtual Stenting Workflow algorithm. Features physics-based stent expansion, VMTK vessel analysis, GPU-accelerated PyVista/VTK visualization, and professional PyQt6 interface. Supports multiple stent models (Enterprise, Pipeline, Silk) with realistic wire mechanics, HDF5 export, and Docker containerization for cross-platform deployment.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Medical+Sim",
                    ProjectUrl = "/projects/medical-simulation",
                    Tags = new[] { "Python 3.10", "VMTK", "PyVista", "PyQt6", "Medical Imaging", "GPU Computing", "Biomedical Engineering", "Docker" },
                    Category = ProjectCategory.Professional
                },
                new Project
                {
                    Title = "pNanoLocz: Python AFM Analysis Platform Development",
                    Description = "Initial development phase of converting the acclaimed NanoLocz MATLAB application into a modern Python-based AFM (Atomic Force Microscopy) analysis platform. Designed modular PyQt6-based architecture with extensible file reading system supporting 8+ AFM formats (.spm, .asd, .jpk, .ibw, .ARIS, .nhf, .gwy, .tiff). Implemented core data management, GUI foundation with responsive layout, and video player infrastructure for AFM data visualization. Established scalable framework for complete migration of research-grade scientific application used by AFM researchers worldwide.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=pNanoLocz",
                    ProjectUrl = "/projects/pnanolocz",
                    Tags = new[] { "Python", "PyQt6", "NumPy", "MATLAB", "AFM Analysis", "Scientific Computing", "GUI Development", "File Parsing" },
                    Category = ProjectCategory.Professional
                }
            };
        }
    }
}
