using website.api.Models;

namespace website.api.Services
{
    public interface IProjectService
    {
        Task<List<Project>> GetProjectsAsync();
        Task<Project?> GetProjectByUrlAsync(string projectUrl);
    }

    public class ProjectService : IProjectService
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
                    Tags = new[] { "Python", "Redis", "PostgreSQL", "FastAPI", "Distributed Systems", "Claude Code Pro", "Gemini CLI", "AI Research" },
                    Category = ProjectCategory.Personal
                },
                new Project
                {
                    Title = "Personal Portfolio & C# Learning Journey",
                    Description = "Blazor WebAssembly portfolio built with .NET 8 and C# to master modern web development fundamentals. Features client-side rendering, responsive Tailwind CSS design, component architecture, dependency injection, and Font Awesome icons. Includes dynamic project showcase, mobile navigation, and 3D tilt effects via Vanilla Tilt.js. Developed entirely with Claude Code assistance to demonstrate AI-accelerated learning and document the C# development journey for employers and fellow developers.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Portfolio",
                    ProjectUrl = "/projects/ai-research",
                    Tags = new[] { "Blazor WebAssembly", ".NET 8", "C#", "Tailwind CSS", "Learning Journey", "Claude Code" },
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
                    Title = "Numerical Methods for Predator-Prey Systems",
                    Description = "Advanced numerical computation coursework implementing and analyzing differential equation solvers for predator-prey models (Lotka-Volterra equations). Built custom Euler method and compared performance against SSPRK3 and Runge-Kutta methods across multiple time steps. Features comprehensive error analysis with absolute error calculations, Euclidean norm computations, and stability assessment. Includes CPU core isolation for accurate performance benchmarking, mathematical convergence analysis, and professional scientific computing practices. Demonstrates exceptional understanding of numerical methods theory and implementation complexity for undergraduate-level coursework.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Numerical+Methods",
                    ProjectUrl = "/projects/numerical-computation",
                    Tags = new[] { "Python", "NumPy", "Matplotlib", "Numerical Analysis", "Differential Equations", "Scientific Computing", "Performance Analysis", "Mathematical Modeling" },
                    Category = ProjectCategory.Academic
                },
                new Project
                {
                    Title = "AI-Powered Mental Health App Design",
                    Description = "Comprehensive software engineering design brief for a £100,000 mental health app targeting university students globally. Features advanced ChatGPT-4 Turbo API integration, multi-stakeholder analysis, and sophisticated system architecture. Includes university system integration, crisis intervention protocols, and comprehensive privacy controls. Demonstrates professional software engineering methodology with agile development planning, ethical considerations, and legal compliance (GDPR, Data Protection Act). Designed for global scalability across universities with local encryption, cultural personalization, and healthcare service integration (BetterHelp, NHS).",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Mental+Health+App",
                    ProjectUrl = "/projects/software-engineering",
                    Tags = new[] { "Software Engineering", "AI Integration", "ChatGPT-4", "System Design", "UML", "Agile Development", "Ethics & Privacy", "Healthcare Technology" },
                    Category = ProjectCategory.Academic
                },
                new Project
                {
                    Title = "Project Synapse - AI Healthcare Documentation System",
                    Description = "Comprehensive business plan for an AI-powered healthcare documentation system targeting NHS doctors and healthcare professionals. Features advanced speech-to-text technology, automated patient record generation, and seamless NHS system integration. Addresses critical NHS administrative burden affecting 1.8 million healthcare workers with £2 billion government digitalization support. Includes detailed market feasibility analysis, industry research using IBISWorld data, Blue Ocean Strategy competitive analysis, and financial viability assessment. Demonstrates professional business planning methodology with stakeholder analysis, target market segmentation, and scalable implementation strategy.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Project+Synapse",
                    ProjectUrl = "/projects/business-plan",
                    Tags = new[] { "Business Planning", "Healthcare Technology", "AI Documentation", "NHS Integration", "Market Research", "Blue Ocean Strategy", "Entrepreneurship", "Speech-to-Text" },
                    Category = ProjectCategory.Academic
                }
            };

            return Task.FromResult(projects);
        }

        public async Task<Project?> GetProjectByUrlAsync(string projectUrl)
        {
            var projects = await GetProjectsAsync();
            return projects.FirstOrDefault(p => p.ProjectUrl.Equals(projectUrl, StringComparison.OrdinalIgnoreCase));
        }
    }
}