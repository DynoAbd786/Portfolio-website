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
                    ProjectUrl = "/projects/portfolio",
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
                },
                new Project
                {
                    Title = "Quantum Dot Investment Report: Photonic Quantum Computing",
                    Description = "Comprehensive nanotechnology business case for venture capital investment in quantum dot manufacturing for photonic quantum computing applications. Researched and analyzed quantum dot technology (2-10nm semiconductor nanocrystals), molecular beam epitaxy manufacturing processes, and the revolutionary potential of light-based quantum computation. Proposed three-phase business strategy targeting the emerging photonic quantum computing market, identifying quantum dots as critical components with superior advantages over traditional electron-based systems: speed-of-light processing, lower energy requirements, enhanced scalability, and elimination of cryogenic cooling needs.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Quantum+Dots",
                    ProjectUrl = "/projects/quantum-dots",
                    Tags = new[] { "Nanotechnology", "Quantum Computing", "Business Strategy", "Photonics", "Semiconductor Physics", "Investment Analysis", "Technical Writing" },
                    Category = ProjectCategory.Academic
                },
                new Project
                {
                    Title = "Custom Image Format & Compression System",
                    Description = "Sophisticated C programming project implementing custom image file formats (EBF, EBU, EBC) with advanced lossless compression algorithms. Built 16 executable programs across two courseworks, featuring bit-level manipulation for 5/8 compression ratio, block-based lossy compression achieving 9x size reduction, and complex 3x3 pixel processing with seeded randomization. Demonstrates advanced systems programming concepts including memory management, modular architecture, comprehensive error handling, and professional development practices with Git workflow integration and rigorous testing frameworks.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=C+Programming",
                    ProjectUrl = "/projects/c-programming",
                    Tags = new[] { "C Programming", "Image Processing", "Compression Algorithms", "Bit Manipulation", "Systems Programming", "Memory Management", "File Formats", "Academic Project" },
                    Category = ProjectCategory.Academic
                },
                new Project
                {
                    Title = "xv6 Operating System Development & Custom Shell",
                    Description = "Advanced operating systems coursework implementing a complete Unix-style shell and extending the xv6 educational operating system. Built sophisticated command-line interface with multi-element pipelines, I/O redirection, and sequential command execution across 582 lines of C code. Created custom system calls, implemented process communication with pipes, and developed memory management optimizations. Features include recursive pipeline handling, dynamic tokenization, comprehensive error handling, and kernel-level programming for RISC-V architecture. Demonstrates enterprise-level systems programming skills typically found in OS development and embedded systems.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=xv6+OS",
                    ProjectUrl = "/projects/xv6-operating-system",
                    Tags = new[] { "C Programming", "Operating Systems", "xv6", "Unix Shell", "RISC-V", "System Calls", "Process Management", "Memory Management", "Kernel Development", "Academic Project" },
                    Category = ProjectCategory.Academic
                }
            };
        }
    }
}
