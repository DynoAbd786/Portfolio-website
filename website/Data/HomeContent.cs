using System.Text.Json;

namespace website.Data
{
    public record ProofFact(string Value, string Label);

    // Title, result and tags come from the project data
    public record SelectedWorkItem(string FieldLabel, string ProjectUrl);

    public record ProductItem(string Name, string Summary, string[] Tags, string? Href);

    public record ExperienceItem(string When, string Title, string Org, string Detail);

    // One clickable object in the workshop scene. Action is "link" or "assistant" (opens the chat).
    // PinX/PinY place its link over the still image, as percentages of the stage.
    public record WorkshopHotspot(
        string Key, string Name, string Sub, string Kicker, string Title, string Desc,
        string[] Points, string[] Chips, string Href, string Cta, string Action,
        double PinX, double PinY);

    // Everything the homepage says that isn't project data. Facts come from the CV, and for
    // Synergy and HireForge from their own repositories.
    public static class HomeContent
    {
        public static bool OpenToWork => true;

        public static IReadOnlyList<ProofFact> Proof { get; } = new[]
        {
            new ProofFact("First-Class", "MEng CS & AI, Leeds"),
            new ProofFact("50% faster", "than the MATLAB original (pNanoLocz)"),
            new ProofFact("2", "research internships"),
            new ProofFact("cm-accurate", "3D/4D reconstruction"),
        };

        public static IReadOnlyList<SelectedWorkItem> SelectedWork { get; } = new[]
        {
            new SelectedWorkItem("biomedical", "/projects/medical-simulation"),
            new SelectedWorkItem("scientific software", "/projects/pnanolocz"),
            new SelectedWorkItem("computer vision", "/projects/3d-reconstruction"),
            new SelectedWorkItem("cloud", "/projects/distributed-systems"),
            new SelectedWorkItem("infrastructure", "/projects/home-lab"),
        };

        public static IReadOnlyList<ProductItem> Products { get; } = new[]
        {
            new ProductItem("Synergy",
                "A local-first code intelligence platform that turns a codebase into a queryable graph for developers and coding agents.",
                new[] { "Python", "React", "TypeScript", "SQLite", "tree-sitter", "MCP" },
                "/projects/workflow-graph-platform"),
            new ProductItem("HireForge",
                "Technical hiring assessments run by LLM agents: one generates a codebase with a seeded defect, the candidate works on it, and an independent agent reviews the work and writes a scorecard.",
                new[] { "Python", "FastAPI", "PostgreSQL", "TypeScript", "Docker" },
                null),
        };

        public static IReadOnlyList<ExperienceItem> Experience { get; } = new[]
        {
            new ExperienceItem("2022 – 2026", "MEng Computer Science with AI", "University of Leeds",
                "First-Class Honours. Master's thesis on a modular 3D/4D computer vision reconstruction pipeline."),
            new ExperienceItem("Jun – Aug 2025", "Summer internship: stent deployment simulation", "University of Leeds",
                "A GPU-accelerated simulation of flow-diverter stents deploying in vessel geometries, with collision and force models."),
            new ExperienceItem("Jun – Aug 2024", "Summer internship: pNanoLocz", "University of Leeds",
                "A Python microscopy analysis platform about 50% faster than the MATLAB original. Led the team and mentored a junior intern."),
            new ExperienceItem("At university", "Core Executive Leadership Team", "Leeds Artificial Intelligence Society",
                "Organised and led technical workshops and helped run the society."),
        };

        public static IReadOnlyList<WorkshopHotspot> Workshop { get; } = new[]
        {
            new WorkshopHotspot("synergy", "Synergy", "code intelligence platform",
                "something I'm building · developer tools", "Synergy",
                "A local-first code intelligence platform that turns a codebase into a queryable graph, for developers and for AI coding agents.",
                new[]
                {
                    "tree-sitter parsing of 8 languages into one typed graph of modules, symbols and dependencies",
                    "Revision history and structural diffs that rank changes by blast radius",
                    "An MCP server with 17 tools, so agents reason about structure instead of grepping text",
                },
                new[] { "Python", "React", "TypeScript", "SQLite", "MCP" },
                "/projects/workflow-graph-platform", "Read about Synergy →", "link", 0, 0),
            new WorkshopHotspot("hireforge", "HireForge", "AI-run coding assessments",
                "something I'm building · AI systems", "HireForge",
                "Technical hiring assessments run end to end by LLM agents.",
                new[]
                {
                    "One agent generates a codebase with a single seeded defect",
                    "The candidate works a realistic scenario against it",
                    "A second, independent agent reviews the work and writes a scorecard",
                },
                new[] { "Python", "FastAPI", "PostgreSQL", "TypeScript", "Docker" },
                "#building", "More on what I'm building →", "link", 0, 0),
            new WorkshopHotspot("thesis", "MEng thesis", "3D / 4D reconstruction",
                "master's thesis · computer vision", "Hybrid 3D/4D reconstruction",
                "A modular pipeline that rebuilds scenes in 3D and over time, benchmarked to centimetre accuracy.",
                new[]
                {
                    "Iterative 2D point tracking gives accurate coordinate trajectories across frames",
                    "Dense depth estimation fills in the geometry between the tracked points",
                    "Benchmarked across standard test scenes, with centimetre-accurate error",
                },
                new[] { "Python", "computer vision", "point tracking", "dense depth" },
                "/projects/3d-reconstruction", "Read the case study →", "link", 0, 0),
            new WorkshopHotspot("stent", "Stent simulation", "internship · 2025",
                "research internship · biomedical", "Flow diverter stent deployment",
                "A physics-driven simulation of flow-diverter stents deploying inside vessel geometries, built at the University of Leeds.",
                new[]
                {
                    "GPU-accelerated 3D visualisation pipeline with PyVista, VTK and VMTK",
                    "Joint-level deployment mechanics, collision detection and force simulation for the wire",
                    "A scalable foundation for medical device R&D",
                },
                new[] { "Python", "VMTK", "PyVista", "GPU" },
                "/projects/medical-simulation", "Read the case study →", "link", 0, 0),
            new WorkshopHotspot("afm", "pNanoLocz", "internship · 2024",
                "research internship · scientific software", "pNanoLocz",
                "An analysis platform for atomic force microscope data, rebuilt in Python and about 50% faster than the legacy MATLAB code.",
                new[]
                {
                    "PyQt6 desktop app with a modular plugin architecture",
                    "Parallel computing for the heavy processing steps",
                    "Led the development team and mentored a junior intern",
                },
                new[] { "Python", "PyQt6", "parallel computing" },
                "/projects/pnanolocz", "Read the case study →", "link", 0, 0),
            new WorkshopHotspot("homelab", "Home lab", "local LLMs · agents",
                "infrastructure", "Home lab & local LLMs",
                "Local LLM inference and autonomous coding agents, running on my own hardware.",
                new[]
                {
                    "Llama.cpp, Hermes and containerised services across an Ubuntu server and a Raspberry Pi",
                    "An encrypted Tailscale mesh for remote access from anywhere",
                    "Tuned for sustained inference on an RTX 5090",
                },
                new[] { "Llama.cpp", "Docker", "Tailscale", "Linux" },
                "/projects/home-lab", "Read the case study →", "link", 0, 0),
            new WorkshopHotspot("azure", "Azure inventory", "serverless · cloud",
                "cloud · distributed systems", "Azure serverless inventory",
                "An event-driven inventory system built on Azure serverless.",
                new[]
                {
                    "Azure Functions and Cosmos DB, with change feeds for real-time stock tracking",
                    "Automated, API-based alert workflows",
                    "Benchmarked to cut cold-start latency and verify high-throughput concurrent invocation",
                },
                new[] { "Azure Functions", "Cosmos DB", "Event Grid" },
                "/projects/distributed-systems", "Read the case study →", "link", 0, 0),
            new WorkshopHotspot("whiteboard", "Internships", "2024 · 2025",
                "experience", "Two research internships",
                "Two summers at the University of Leeds, both making research software fast and usable.",
                new[]
                {
                    "2025: stent deployment simulation for flow-diverter devices",
                    "2024: pNanoLocz, about 50% faster than the MATLAB original",
                    "Team lead and mentor on pNanoLocz",
                },
                new[] { "research software", "Python", "GPU" },
                "#experience", "See my experience →", "link", 0, 0),
            new WorkshopHotspot("certificate", "Education", "MEng CS & AI · Leeds",
                "education", "First-Class MEng",
                "MEng Computer Science with Artificial Intelligence at the University of Leeds, 2022–2026, First-Class Honours.",
                new[]
                {
                    "Final-year modules at 80–88%: Deep Learning, Data Science, Advanced Software Engineering, Cloud Computing",
                    "Master's thesis in hybrid 3D/4D computer vision reconstruction",
                    "Core executive team of the Leeds AI Society",
                },
                new[] { "First-Class Honours", "AI", "software engineering" },
                "/about", "More about me →", "link", 0, 0),
            new WorkshopHotspot("bookcase", "All projects", "projects & coursework",
                "the bookcase", "Everything else",
                "Coursework and side projects across AI, graphics, systems, scientific computing and the cloud.",
                new[]
                {
                    "Robot navigation with ROS2, SLAM and YOLOv8",
                    "A real-time OpenGL renderer and a 2D rasteriser in C++",
                    "GPU blood-flow simulation, parallel computing and an xv6 kernel",
                },
                new[] { "C++", "CUDA", "ROS2", "OpenGL" },
                "/projects", "Browse all projects →", "link", 0, 0),
            new WorkshopHotspot("robot", "Assistant", "ask me about my work",
                "this website · AI", "Ask the assistant",
                "This site has an AI assistant that answers questions about my work, and an MCP server that AI agents can query. Try asking:",
                new[]
                {
                    "What does Synergy do?",
                    "What did you build as an intern?",
                    "What did your thesis reconstruct?",
                },
                new[] { ".NET 10", "MCP", "AI agents" },
                "#", "Open the chat →", "assistant", 0, 0),
        };

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        // Embedded in the page for the 3D scene. The default encoder escapes < and >, so the
        // result is safe inside a <script> element.
        public static string WorkshopJson() => JsonSerializer.Serialize(Workshop, JsonOptions);
    }
}
