using System.Text.Json;

namespace website.Data
{
    public record ProofFact(string Value, string Label);

    // Title, result and tags come from the project data
    public record SelectedWorkItem(string FieldLabel, string ProjectUrl);

    public record ProductItem(string Name, string Summary, string[] Tags, string? Href);

    public record ExperienceItem(string When, string Title, string Org, string Detail);

    // One clickable object in the workshop scene. Action is "link" or "assistant" (opens the chat).
    // PinX/PinY place its link over the still image, as percentages of the stage
    // (printed by tools/render-workshop-still.py).
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
            new ProofFact("50% faster", "pNanoLocz vs the MATLAB original, 2024 internship"),
            new ProofFact("Stent simulation", "flow diverters in patient vessel geometry, 2025 internship"),
            new ProofFact("1.56 cm", "camera error in 3D from one video, master's group project"),
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
                "Turns a codebase into a graph you can query: what calls what, what a change will break, how two versions differ. It runs on your own machine, and coding agents can use it over MCP.",
                new[] { "Python", "React", "TypeScript", "SQLite", "tree-sitter", "MCP" },
                "/projects/synergy"),
            new ProductItem("HireForge",
                "Coding assessments run by AI agents. One agent builds a small codebase with a planted bug, the candidate fixes it, and a separate agent reviews their work and writes the scorecard.",
                new[] { "Python", "FastAPI", "PostgreSQL", "TypeScript", "Docker" },
                null),
        };

        public static IReadOnlyList<ExperienceItem> Experience { get; } = new[]
        {
            new ExperienceItem("2022 – 2026", "MEng Computer Science with AI", "University of Leeds",
                "First-Class Honours. I led my master's group project, a pipeline that reconstructs 3D scenes from a single video."),
            new ExperienceItem("Jun – Aug 2025", "Summer internship: stent deployment simulation", "University of Leeds",
                "Built a GPU-accelerated tool for simulating flow-diverter stents in blood vessels, with stent models and joint-level expansion forces."),
            new ExperienceItem("Jun – Aug 2024", "Summer internship: pNanoLocz", "University of Leeds",
                "Rebuilt a MATLAB microscopy analysis tool in Python, about 50% faster than the original. I led the team and mentored a junior intern."),
            new ExperienceItem("At university", "Core Executive Leadership Team", "Leeds Artificial Intelligence Society",
                "Organised and led technical workshops and helped run the society."),
        };

        public static IReadOnlyList<WorkshopHotspot> Workshop { get; } = new[]
        {
            new WorkshopHotspot("synergy", "Synergy", "code intelligence platform",
                "something I'm building · developer tools", "Synergy",
                "Turns a codebase into a graph you can query, for developers and for AI coding agents. It runs on your own machine.",
                new[]
                {
                    "Parses 8 languages with tree-sitter into one graph of modules, symbols and dependencies",
                    "Keeps a history of every analysis and shows what changed between versions, biggest impact first",
                    "An MCP server with 21 tools that coding agents can ask instead of searching the code",
                },
                new[] { "Python", "React", "TypeScript", "SQLite", "MCP" },
                "/projects/synergy", "Read about Synergy →", "link", 51.88, 23.46),
            new WorkshopHotspot("hireforge", "HireForge", "AI-run coding assessments",
                "something I'm building · AI systems", "HireForge",
                "Coding assessments run end to end by AI agents.",
                new[]
                {
                    "One agent builds a codebase with a single planted bug",
                    "The candidate works through a realistic task on it",
                    "A separate agent reviews the work and writes the scorecard",
                },
                new[] { "Python", "FastAPI", "PostgreSQL", "TypeScript", "Docker" },
                "#building", "More on what I'm building →", "link", 62.85, 25.43),
            new WorkshopHotspot("thesis", "MEng project", "3D reconstruction",
                "master's group project · computer vision", "3D from a single video",
                "My master's group project: a pipeline that turns one ordinary video into a 3D mesh at real-world scale. I led the team of five.",
                new[]
                {
                    "Dense depth from Depth Anything V3, point tracks from CoTracker3",
                    "Bundle adjustment makes the two agree, then TSDF fusion builds the mesh",
                    "1.56 cm camera error on the Replica benchmark, from colour video alone",
                },
                new[] { "Python", "computer vision", "point tracking", "dense depth" },
                "/projects/3d-reconstruction", "Read the case study →", "link", 37.28, 56.38),
            new WorkshopHotspot("stent", "Stent simulation", "internship · 2025",
                "research internship · biomedical", "Flow diverter stent deployment",
                "A physics simulation of flow-diverter stents opening inside blood vessels, built during my 2025 internship at the University of Leeds.",
                new[]
                {
                    "GPU-accelerated 3D visualisation with PyVista, VTK and VMTK",
                    "Models the forces on each wire joint as the stent opens",
                },
                new[] { "Python", "VMTK", "PyVista", "GPU" },
                "/projects/medical-simulation", "Read the case study →", "link", 64.77, 34.14),
            new WorkshopHotspot("afm", "pNanoLocz", "internship · 2024",
                "research internship · scientific software", "pNanoLocz",
                "Software for analysing atomic force microscope data. We rebuilt it from MATLAB in Python and made it about 50% faster.",
                new[]
                {
                    "A PyQt6 desktop app built around plugins",
                    "Parallel processing for the heavy steps",
                    "I led the development team and mentored a junior intern",
                },
                new[] { "Python", "PyQt6", "parallel computing" },
                "/projects/pnanolocz", "Read the case study →", "link", 74.94, 47.92),
            new WorkshopHotspot("homelab", "Home lab", "local LLMs · agents",
                "infrastructure", "Home lab & local LLMs",
                "I run local LLMs and coding agents on my own hardware.",
                new[]
                {
                    "Llama.cpp, Hermes and containerised services on an Ubuntu server and a Raspberry Pi",
                    "Reachable from anywhere over an encrypted Tailscale network",
                    "Tuned for long inference runs on an RTX 5090",
                },
                new[] { "Llama.cpp", "Docker", "Tailscale", "Linux" },
                "/projects/home-lab", "Read the case study →", "link", 34.55, 48.72),
            new WorkshopHotspot("azure", "Azure inventory", "serverless · cloud",
                "cloud · distributed systems", "Azure serverless inventory",
                "An inventory system on Azure's serverless services that reacts to stock changes as they happen.",
                new[]
                {
                    "Azure Functions and Cosmos DB, with change feeds tracking stock in real time",
                    "Automatic stock alerts through API workflows",
                    "Benchmarked to cut cold-start time and to handle many calls at once",
                },
                new[] { "Azure Functions", "Cosmos DB", "Event Grid" },
                "/projects/distributed-systems", "Read the case study →", "link", 34, 33.85),
            new WorkshopHotspot("whiteboard", "Internships", "2024 · 2025",
                "experience", "Experience",
                "Two summer research internships at the University of Leeds.",
                new[]
                {
                    "2025: stent deployment simulation for flow-diverter devices",
                    "2024: pNanoLocz, about 50% faster than the MATLAB original",
                    "Team lead and mentor on pNanoLocz",
                },
                new[] { "research software", "Python", "GPU" },
                "#experience", "See my experience →", "link", 65.74, 65.17),
            new WorkshopHotspot("certificate", "Education", "MEng CS & AI · Leeds",
                "education", "First-Class MEng",
                "MEng Computer Science with Artificial Intelligence at the University of Leeds, 2022–2026, First-Class Honours.",
                new[]
                {
                    "Final-year modules at 80–88%: Deep Learning, Data Science, Advanced Software Engineering, Cloud Computing",
                    "Led the master's group project: 3D reconstruction from a single video",
                    "Core executive team of the Leeds AI Society",
                },
                new[] { "First-Class Honours", "AI", "software engineering" },
                "/about", "More about me →", "link", 25.54, 39.3),
            new WorkshopHotspot("bookcase", "All projects", "projects & coursework",
                "the bookcase", "Everything else",
                "The rest of my coursework and side projects.",
                new[]
                {
                    "Robot navigation with ROS2, SLAM and YOLOv8",
                    "A real-time OpenGL renderer and a 2D rasteriser in C++",
                    "GPU blood-flow simulation, parallel computing and an xv6 kernel",
                },
                new[] { "C++", "CUDA", "ROS2", "OpenGL" },
                "/projects", "Browse all projects →", "link", 76.17, 36.83),
            new WorkshopHotspot("robot", "Assistant", "ask me about my work",
                "this website · AI", "Ask the assistant",
                "This site has an AI assistant that answers questions about my work, and an MCP server that AI agents can query. Try asking:",
                new[]
                {
                    "What does Synergy do?",
                    "What did you build as an intern?",
                    "What did your master's project reconstruct?",
                },
                new[] { ".NET 10", "MCP", "AI agents" },
                "#", "Open the chat →", "assistant", 68.42, 32.92),
        };

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        // Embedded in the page for the 3D scene. The default encoder escapes < and >, so the
        // result is safe inside a <script> element.
        public static string WorkshopJson() => JsonSerializer.Serialize(Workshop, JsonOptions);
    }
}
