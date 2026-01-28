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
                    Description = "A robust distributed task processing system designed to benchmark AI agent capabilities. Built with Python, Redis, and FastAPI to handle high-concurrency workloads. Demonstrates enterprise architectural patterns including priority queues, auto-scaling workers, and real-time WebSocket monitoring.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=TaskFlow",
                    ProjectUrl = "/projects/taskflow",
                    Tags = new[] { "Python", "Redis", "PostgreSQL", "FastAPI", "Distributed Systems", "Claude Code Pro", "Gemini CLI", "AI Research" },
                    Category = ProjectCategory.Personal,
                    IsFeatured = true
                },
                new Project
                {
                    Title = "Personal Portfolio & C# Learning Journey",
                    Description = "A modern, responsive portfolio website showcasing full-stack development skills. Built with Blazor WebAssembly (.NET 8) and Tailwind CSS for high-performance client-side rendering. Features 3D interactive elements and a component-based architecture, developed to demonstrate rapid AI-assisted engineering.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Portfolio",
                    ProjectUrl = "/projects/portfolio",
                    Tags = new[] { "Blazor WebAssembly", ".NET 8", "C#", "Tailwind CSS", "Learning Journey", "Claude Code" },
                    Category = ProjectCategory.Personal,
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
                    IsFeatured = true,
                    IsInDevelopment = true
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
                    IsFeatured = true,
                    IsInDevelopment = true
                },
                new Project
                {
                    Title = "Workflow-Centric Graph Code Analysis Platform",
                    Description = "An intelligent architectural analysis platform transforming codebases into queryable graphs. Combines C#/.NET for enterprise APIs and Python for AST analysis to detect architectural violations. enhancing LLM code comprehension by reducing token usage by up to 80%.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Graph+Analysis",
                    ProjectUrl = "/projects/workflow-graph-platform",
                    Tags = new[] { "Research Project", "C#", ".NET 8", "ASP.NET Core", "Blazor WASM", "Python", "Neo4j", "Graph Databases", "Doxygen", "LLM Integration", "MCP", "Redis", "Software Architecture", "Architectural Linter", "AI Documentation", "Microservices" },
                    Category = ProjectCategory.Personal,
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
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Home Lab & Remote Infrastructure",
                    Description = "A self-hosted home lab ecosystem powered by Tailscale and Linux. Features a high-performance PC for local LLM inference (Ollama), connected via a secure mesh network to Android devices running Termux as lightweight servers. Automates Wake-on-LAN via n8n workflows for on-demand remote access.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Home+Lab",
                    ProjectUrl = "/projects/home-lab",
                    Tags = new[] { "Tailscale", "Linux", "Self-Hosting", "n8n", "Termux", "Ollama", "Wake-on-LAN", "Networking", "Home Automation" },
                    Category = ProjectCategory.Personal,
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