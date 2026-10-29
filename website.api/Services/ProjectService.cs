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
                    Title = "This Website (mkkai.dev)",
                    Description = "My portfolio, built in .NET 10 with Blazor. Pages are rendered on the server, the homepage has an interactive 3D workshop made with three.js, and there is an AI assistant plus an MCP server that AI agents can query. Tested with xUnit and Playwright and deployed on Render.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Portfolio",
                    ProjectUrl = "/projects/portfolio",
                    Tags = new[] { ".NET 10", "Blazor", "C#", "three.js", "Tailwind CSS", "MCP" },
                    Category = ProjectCategory.Personal,
                    Fields = new[] { ProjectField.SystemsInfrastructure, ProjectField.AiMl },
                    Result = "Built this site in .NET 10, with an AI assistant and an MCP server that AI agents can query.",
                    IsFeatured = true
                },
                new Project
                {
                    Title = "Formula Student AI - Autonomous Racing Systems Development",
                    Description = "Autonomous racing system for the Formula Student AI competition. Developed SLAM and path-planning algorithms using ROS2, C++, and Python on Nvidia DGX hardware. A multidisciplinary engineering effort validating autonomous subsystems in simulated and real-world racing environments.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=FS-AI",
                    ProjectUrl = "/projects/formula-student-ai",
                    Tags = new[] { "Formula Student AI", "ROS2", "C++", "Python", "Autonomous Vehicles", "Simulation", "SLAM", "Ubuntu 24.04", "Docker", "Nvidia DGX Orin", "Team Project", "Localization", "Perception", "Path Planning", "Vehicle Control" },
                    Category = ProjectCategory.Personal,
                    Fields = new[] { ProjectField.AiMl, ProjectField.SystemsInfrastructure },
                    Result = "Joined a team building an autonomous race car, working with ROS2, SLAM and path planning.",
                    IsFeatured = false,
                    IsInDevelopment = false
                },
                new Project
                {
                    Title = "Advanced 3D Reconstruction System",
                    Description = "Innovative hybrid framework fusing neural networks (depth) with optimization models (pose) for industrial 3D capture. Leverages Python and HPC resources to solve fundamental computer vision trade-offs. Targeted for VR and medical imaging applications with a focus on production-ready deployment.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=3D+Recon",
                    ProjectUrl = "/projects/3d-reconstruction",
                    GitHubUrl = "",
                    Tags = new[] { "3D Reconstruction", "Computer Vision", "Deep Learning", "Python", "Team Project", "Agile Methodology", "Project Management", "Research & Development", "VR/AR", "Medical Imaging", "Industrial Applications", "HPC Computing", "Neural Networks", "Optimization" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.VisionGraphics, ProjectField.AiMl },
                    Result = "Built a 3D/4D reconstruction pipeline that combines point tracking with dense depth, accurate to the centimetre.",
                    IsFeatured = true,
                    IsInDevelopment = false
                },
                new Project
                {
                    Title = "Synergy: Code Intelligence Platform",
                    Description = "Builds a graph of a codebase in any of 8 languages and lets you query it from a web dashboard or an AI coding agent. It keeps snapshots over time, shows what changed between them, and gives agents 21 MCP tools to ask about the code directly. Python backend, React and TypeScript dashboard.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Synergy",
                    ProjectUrl = "/projects/synergy",
                    Tags = new[] { "Python", "React", "TypeScript", "SQLite", "tree-sitter", "MCP" },
                    Category = ProjectCategory.Personal,
                    Fields = new[] { ProjectField.ProductBusiness, ProjectField.AiMl },
                    Result = "Building a tool that turns a codebase in any of 8 languages into a graph that developers and AI agents can query.",
                    IsFeatured = true,
                    IsInDevelopment = true
                },
                new Project
                {
                    Title = "Flow Diverter Deployment & Medical Simulation Platform",
                    Description = "Biomedical simulation platform for predicting stent deployment in patient-specific vascular geometries. Built with Python, VMTK, and GPU-accelerated visualization to aid surgical planning. Extends clinical research capabilities with realistic physics modeling and seamless data export.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Medical+Sim",
                    ProjectUrl = "/projects/medical-simulation",
                    Tags = new[] { "Python 3.10", "VMTK", "PyVista", "PyQt6", "Medical Imaging", "GPU Computing", "Biomedical Engineering", "Docker" },
                    Category = ProjectCategory.Professional,
                    Fields = new[] { ProjectField.ScientificBiomedical, ProjectField.VisionGraphics },
                    Result = "Built a GPU-accelerated simulation of flow-diverter stents opening inside blood vessels, with collision and force models.",
                    IsFeatured = true
                },
                new Project
                {
                    Title = "pNanoLocz: Python AFM Analysis Platform Development",
                    Description = "Modern Python-based analysis platform for Atomic Force Microscopy (AFM), migrating a legacy MATLAB tool. Features a modular PyQt6 GUI and multi-format file support to streamline scientific workflows. empowers researchers with robust data visualization and processing capabilities.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=pNanoLocz",
                    ProjectUrl = "/projects/pnanolocz",
                    Tags = new[] { "Python", "PyQt6", "NumPy", "MATLAB", "AFM Analysis", "Scientific Computing", "GUI Development", "File Parsing" },
                    Category = ProjectCategory.Professional,
                    Fields = new[] { ProjectField.ScientificBiomedical },
                    Result = "Led the rebuild of a MATLAB microscopy tool in Python, making it about 50% faster, and mentored an intern.",
                    IsFeatured = true
                },
                new Project
                {
                    Title = "Medical AI Research: Patient-Centric EHR System",
                    Description = "Patient-centric AI system designed to reduce NHS administrative burden via automated documentation. Integrates GPT-4 and speech-to-text to streamline electronic health records while ensuring GDPR compliance. Validated through pilot studies to enhance clinical workflow efficiency.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=NHS+AI+System",
                    ProjectUrl = "/projects/data-mining",
                    Tags = new[] { "Data Mining", "Text Analytics", "GPT-4 API", "Healthcare AI", "NHS Integration", "SpaCy NER", "CRISP-DM", "GDPR Compliance", "Speech-to-Text", "Medical Documentation" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.AiMl, ProjectField.ScientificBiomedical },
                    Result = "Designed a framework for using GPT-4 to mine NHS clinical data, with a GDPR plan and a cost-benefit model.",
                    IsFeatured = true
                },
                new Project
                {
                    Title = "Numerical Methods for Predator-Prey Systems",
                    Description = "High-performance differential equation solver analyzing predator-prey system stability. Implemented custom Euler and Runge-Kutta methods in Python with rigorous error analysis. Demonstrates scientific computing expertise through CPU-isolated benchmarking and mathematical convergence proof.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Numerical+Methods",
                    ProjectUrl = "/projects/numerical-computation",
                    Tags = new[] { "Python", "NumPy", "Matplotlib", "Numerical Analysis", "Differential Equations", "Scientific Computing", "Performance Analysis", "Mathematical Modeling" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.ScientificBiomedical },
                    Result = "Implemented Euler and Runge-Kutta solvers in Python and analysed their error and stability on predator-prey models.",
                    IsFeatured = false
                },
                new Project
                {
                    Title = "AI-Powered Mental Health App Design",
                    Description = "Strategic technical design for a scalable mental health platform targeting global university students. Proposes a secure, AI-integrated (ChatGPT-4) architecture with strict privacy controls. Comprehensive engineering roadmap focusing on ethical compliance, API integration, and system scalability.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Mental+Health+App",
                    ProjectUrl = "/projects/mental-health-app",
                    Tags = new[] { "Software Engineering", "AI Integration", "ChatGPT-4", "System Design", "UML", "Agile Development", "Ethics & Privacy", "Healthcare Technology" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.ProductBusiness, ProjectField.AiMl },
                    Result = "Designed an AI-assisted mental health app for university students, including how their data is kept private.",
                    IsFeatured = true
                },
                new Project
                {
                    Title = "TerraTracker - GPS Route Tracking Platform",
                    Description = "Full-stack GPS tracking application built by a 6-person agile team. Utilizes Flask, PostgreSQL, and Leaflet.js for real-time route sharing and analytics. Showcases professional DevOps practices including CI/CD pipelines, Docker containerization, and rigorous version control.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=TerraTracker",
                    ProjectUrl = "/projects/terratracker",
                    Tags = new[] { "Team Project", "Flask", "Python", "PostgreSQL", "Agile", "Git Workflow", "Docker", "CI/CD", "Leaflet.js", "RESTful API" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.CloudDistributed },
                    Result = "Shipped a GPS route-tracking web app in a 6-person agile team, with Flask, PostgreSQL, CI/CD and Docker.",
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Project Synapse - AI Healthcare Documentation",
                    Description = "Strategic business plan for an AI healthcare documentation system, addressing the £2bn NHS digitalization market. Analyzes financial viability and competitive strategy (Blue Ocean) for a speech-to-text solution. Targets reduced clinician burnout through automated patient record generation.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Project+Synapse",
                    ProjectUrl = "/projects/business-plan",
                    Tags = new[] { "Business Planning", "Healthcare Technology", "AI Documentation", "NHS Integration", "Market Research", "Blue Ocean Strategy", "Entrepreneurship", "Speech-to-Text" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.ProductBusiness, ProjectField.AiMl },
                    Result = "Wrote a business plan for an AI tool that turns NHS consultations into notes, with market research and financials.",
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Advanced CFD Blood Flow Simulation (XLB)",
                    Description = "High-performance CFD framework simulating blood flow in aneurysms using GPU acceleration (CUDA). Extended the XLB library to achieve 2,340+ MLUPS with physiological accuracy. Enables rapid clinical risk assessment significantly faster than traditional methods.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=XLB+CFD+Simulation",
                    ProjectUrl = "/vascular-blood-flow",
                    Tags = new[] { "Python", "XLB Framework", "CUDA", "CFD", "Lattice Boltzmann Method", "GPU Computing", "Medical Simulation", "Blood Flow", "Aneurysm Risk Assessment", "ParaView", "Docker" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.ScientificBiomedical },
                    Result = "Extended the XLB GPU fluid solver to simulate blood flow in aneurysms at over 2,340 MLUPS.",
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Machine Learning: Regression & Dimensionality Reduction",
                    Description = "Implementation of advanced dimensionality reduction (PCA/ICA) and regression algorithms from scratch. Applied to complex high-dimensional datasets to extract meaningful features. Demonstrates deep understanding of statistical modeling and mathematical foundations of ML.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Machine+Learning",
                    ProjectUrl = "/projects/machine-learning",
                    Tags = new[] { "Python", "Machine Learning", "Regression Analysis", "PCA", "ICA", "Statistical Modeling", "Data Science", "NumPy", "Scikit-learn", "Dimensionality Reduction" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.AiMl },
                    Result = "Implemented regression with custom basis functions and PCA from scratch, applied to 3D medical data.",
                    IsFeatured = true
                },
                new Project
                {
                    Title = "Azure Serverless Inventory Management System",
                    Description = "Cloud-native inventory management system built on Azure serverless architecture. Orchestrates Azure Functions, Cosmos DB, and Event Grid for a scalable, event-driven workflow. Exemplifies modern microservices design and automated DevOps deployment standards.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Distributed+Systems",
                    ProjectUrl = "/projects/distributed-systems",
                    Tags = new[] { "Azure", "Serverless", "Distributed Systems", "Cloud Computing", "Microservices", "Event-Driven Architecture", "DevOps", "API Management", "Cosmos DB", "Azure Functions" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.CloudDistributed },
                    Result = "Built an event-driven serverless inventory system on Azure Functions and Cosmos DB, benchmarked for cold starts and throughput.",
                    IsFeatured = true
                },
                new Project
                {
                    Title = "AI Search Algorithms: Sliding Block Puzzle Analysis",
                    Description = "Comparative analysis of AI search algorithms (A*, BFS, DFS) solving complex puzzle states. Optimized heuristic functions to inspect search space complexity and efficiency. Demonstrates strong algorithmic problem-solving skills in constraint satisfaction scenarios.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=AI+Search",
                    ProjectUrl = "/projects/ai-search-algorithms",
                    Tags = new[] { "Python", "Artificial Intelligence", "Search Algorithms", "A* Algorithm", "Heuristics", "Problem Solving", "State Space Search", "Algorithm Analysis", "BFS", "DFS" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.AiMl },
                    Result = "Compared A*, BFS and DFS on sliding-block puzzles and designed heuristics to shrink the search space.",
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Machine Learning: Decision Trees for Fraud Detection",
                    Description = "Custom implementation of ID3 decision tree algorithms for financial fraud detection. Optimized information gain and entropy calculations for accurate classification. Showcases practical application of supervised learning to improved security systems.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Decision+Trees",
                    ProjectUrl = "/projects/ai-decision-trees",
                    Tags = new[] { "Python", "Machine Learning", "Decision Trees", "Fraud Detection", "Classification", "ID3 Algorithm", "Data Mining", "Financial Security", "Supervised Learning", "Feature Selection" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.AiMl },
                    Result = "Implemented ID3 decision trees from scratch to classify fraudulent financial transactions.",
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Advanced Parallel Computing: OpenMP, MPI & OpenCL",
                    Description = "High-performance computing project spanning MPI, OpenMP, and OpenCL paradigms. Achieved nearly linear (3.09x) speedup on distributed systems and accelerated heat equation solvers on GPUs. Proves mastery of parallel architecture, memory optimization, and thread synchronization.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Parallel+Computing",
                    ProjectUrl = "/projects/parallel-computing",
                    Tags = new[] { "OpenMP", "MPI", "OpenCL", "C Programming", "Parallel Computing", "GPU Programming", "Distributed Systems", "Performance Analysis", "High Performance Computing", "CUDA" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.SystemsInfrastructure, ProjectField.CloudDistributed },
                    Result = "Parallelised solvers with MPI, OpenMP and OpenCL, reaching a near-linear 3.09x speedup on distributed runs.",
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Autonomous Robot Navigation & Object Detection",
                    Description = "ROS2-based navigation system aiming for intelligent object interactions in simulated environments. Integrates SLAM, YOLOv8 object detection, and motion planning algorithms. Delivers a robust autonomous agent capable of mapping and navigating dynamic spaces.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Autonomous+Robotics",
                    ProjectUrl = "/projects/autonomous-robotics",
                    Tags = new[] { "ROS2", "Python", "Computer Vision", "SLAM", "YOLOv8", "OpenCV", "TurtleBot3", "Gazebo", "Motion Planning", "Autonomous Systems" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.AiMl, ProjectField.VisionGraphics },
                    Result = "Built a ROS2 robot that maps its surroundings with SLAM and finds objects with YOLOv8.",
                    IsFeatured = true
                },
                new Project
                {
                    Title = "3D OpenGL Renderer with Advanced Graphics Pipeline",
                    Description = "Real-time 3D rendering engine built with C++ and modern OpenGL. Features a complete programmable pipeline with custom shaders, terrain generation, and advanced lighting. Demonstrates core graphics programming skills and optimization for visual fidelity.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=OpenGL+Renderer",
                    ProjectUrl = "/projects/opengl-renderer",
                    Tags = new[] { "OpenGL", "C++", "Computer Graphics", "3D Rendering", "Shader Programming", "Terrain Visualization", "Real-time Rendering", "Collaborative Project" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.VisionGraphics },
                    Result = "Built a real-time C++ OpenGL renderer with custom shaders, terrain generation and lighting.",
                    IsFeatured = false
                },
                new Project
                {
                    Title = "xv6 Operating System Development & Custom Shell",
                    Description = "Kernel-level development extending the xv6 educational operating system. Implemented a custom Unix-style shell, system calls, and process management features. Provides a deep dive into low-level memory management and OS architecture.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=xv6+OS",
                    ProjectUrl = "/projects/xv6-operating-system",
                    Tags = new[] { "C Programming", "Operating Systems", "Unix Shell", "Kernel Development", "System Calls", "Process Management", "Memory Management", "xv6" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.SystemsInfrastructure },
                    Result = "Extended the xv6 kernel with new system calls and process management, and wrote a Unix-style shell.",
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Quantum Dot Investment Case",
                    Description = "Technical business case analyzing the commercial viability of quantum dot manufacturing. Explores applications in photonic quantum computing and future displays. Bridges deep tech research with market impact assessment for emerging nanotechnologies.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Quantum+Dots",
                    ProjectUrl = "/projects/quantum-dots",
                    Tags = new[] { "Nanotechnology", "Quantum Computing", "Business Analysis", "Investment Strategy", "Market Research", "Photonic Computing", "Quantum Dots", "Technology Assessment" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.ProductBusiness },
                    Result = "Wrote an investment case for a quantum dot spin-out, aimed at photonic quantum computing.",
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Custom Image Format & Compression System",
                    Description = "Low-level image processing system with custom binary file formats and compression algorithms. Written in pure C to optimize memory usage and processing speed. Demonstrates strong systems programming fundamentals and bit-level data manipulation.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=C+Programming",
                    ProjectUrl = "/projects/c-programming",
                    Tags = new[] { "C Programming", "Image Processing", "Compression Algorithms", "File Formats", "Memory Management", "Systems Programming", "Bit Manipulation", "Algorithm Optimization" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.SystemsInfrastructure, ProjectField.VisionGraphics },
                    Result = "Wrote a custom binary image format and compression system in plain C.",
                    IsFeatured = false
                },
                new Project
                {
                    Title = "2D Graphics Fundamentals & Rasterization Engine",
                    Description = "Fundamental 2D rasterization engine built from scratch in C++. Implements line drawing and rendering algorithms to visualize geometric primitives. Validates core mathematical concepts behind modern computer graphics systems.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=2D+Graphics",
                    ProjectUrl = "/projects/graphics-fundamentals",
                    Tags = new[] { "C++", "Computer Graphics", "2D Rendering", "Rasterization", "Algorithm Implementation", "Performance Analysis", "Graphics Primitives", "Mathematical Modeling" },
                    Category = ProjectCategory.Academic,
                    Fields = new[] { ProjectField.VisionGraphics },
                    Result = "Built a 2D rasterisation engine from scratch in C++, drawing lines and primitives pixel by pixel.",
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Home Lab & Remote Infrastructure",
                    Description = "A desktop with an RTX 5090 running local LLMs (llama.cpp and Hermes) and coding agents, and an always-on Raspberry Pi, linked to my laptop and phone over Tailscale. Friends can use the GPU for research through a shared, access-controlled workspace.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Home+Lab",
                    ProjectUrl = "/projects/home-lab",
                    Tags = new[] { "Tailscale", "Linux", "llama.cpp", "Docker", "Raspberry Pi", "Self-Hosting" },
                    Category = ProjectCategory.Personal,
                    Fields = new[] { ProjectField.SystemsInfrastructure, ProjectField.AiMl },
                    Result = "Set up local LLMs and coding agents on a home GPU server, reachable anywhere over an encrypted Tailscale mesh.",
                    IsFeatured = true
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