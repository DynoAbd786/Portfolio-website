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
                    Category = ProjectCategory.Personal,
                    IsFeatured = true
                },
                new Project
                {
                    Title = "Personal Portfolio & C# Learning Journey",
                    Description = "Blazor WebAssembly portfolio built with .NET 8 and C# to master modern web development fundamentals. Features client-side rendering, responsive Tailwind CSS design, component architecture, dependency injection, and Font Awesome icons. Includes dynamic project showcase, mobile navigation, and 3D tilt effects via Vanilla Tilt.js. Developed entirely with Claude Code assistance to demonstrate AI-accelerated learning and document the C# development journey for employers and fellow developers.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Portfolio",
                    ProjectUrl = "/projects/portfolio",
                    Tags = new[] { "Blazor WebAssembly", ".NET 8", "C#", "Tailwind CSS", "Learning Journey", "Claude Code" },
                    Category = ProjectCategory.Personal,
                    IsFeatured = true
                },
                new Project
                {
                    Title = "Workflow-Centric Graph Platform for AI-Enhanced Code Understanding",
                    Description = "MEng flagship research project developing a graph-based platform that transforms codebases into interactive, multi-level visual representations with AI-generated documentation. Features workflow-centric organization of structural and behavioral graphs, version-specific package analysis for open-source libraries, and dual-interface system with web visualization and MCP API for LLM integration. Built on 7-service microservices architecture using Neo4j, Doxygen, and OpenAI APIs. Includes comprehensive validation framework with LLM before/after evaluation targeting 75-80% token reduction and advanced features like hierarchical LLM agents, cross-language architecture generalization, and security threat modeling. Currently in feasibility assessment stage for MEng final year group project.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Graph+Analysis",
                    ProjectUrl = "/projects/workflow-graph-platform",
                    Tags = new[] { "MEng Research", "Graph Databases", "Neo4j", "Python", "Doxygen", "LLM Integration", "MCP", "FastAPI", "React", "Software Architecture", "AI Documentation", "Open Source Analysis" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = true,
                    IsInDevelopment = true
                },
                new Project
                {
                    Title = "Flow Diverter Deployment Algorithm & Medical Simulation Platform",
                    Description = "Advanced biomedical research platform for simulating flow diverter stent deployment in patient-specific vascular geometries. Built during 8-week internship, extending the 2016 Paliwal Virtual Stenting Workflow algorithm. Features physics-based stent expansion, VMTK vessel analysis, GPU-accelerated PyVista/VTK visualization, and professional PyQt6 interface. Supports multiple stent models (Enterprise, Pipeline, Silk) with realistic wire mechanics, HDF5 export, and Docker containerization for cross-platform deployment.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Medical+Sim",
                    ProjectUrl = "/projects/medical-simulation",
                    Tags = new[] { "Python 3.10", "VMTK", "PyVista", "PyQt6", "Medical Imaging", "GPU Computing", "Biomedical Engineering", "Docker" },
                    Category = ProjectCategory.Professional,
                    IsFeatured = true
                },
                new Project
                {
                    Title = "pNanoLocz: Python AFM Analysis Platform Development",
                    Description = "Initial development phase of converting the acclaimed NanoLocz MATLAB application into a modern Python-based AFM (Atomic Force Microscopy) analysis platform. Designed modular PyQt6-based architecture with extensible file reading system supporting 8+ AFM formats (.spm, .asd, .jpk, .ibw, .ARIS, .nhf, .gwy, .tiff). Implemented core data management, GUI foundation with responsive layout, and video player infrastructure for AFM data visualization. Established scalable framework for complete migration of research-grade scientific application used by AFM researchers worldwide.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=pNanoLocz",
                    ProjectUrl = "/projects/pnanolocz",
                    Tags = new[] { "Python", "PyQt6", "NumPy", "MATLAB", "AFM Analysis", "Scientific Computing", "GUI Development", "File Parsing" },
                    Category = ProjectCategory.Professional,
                    IsFeatured = true
                },
                new Project
                {
                    Title = "Medical AI Research: Patient-Centric EHR System Development",
                    Description = "Comprehensive Data Mining research project developing a patient-centric AI system for enhanced electronic health record integration within the NHS. Features GPT-4 Turbo API integration, advanced speech-to-text recognition, SpaCy-based pseudonymisation for GDPR compliance, and automated clinical documentation generation. Implements CRISP-DM methodology with comprehensive pilot study using rheumatoid arthritis case scenarios. Addresses critical NHS administrative burden affecting healthcare professionals who spend one-third of working hours on documentation. Includes detailed 6-month implementation timeline, stakeholder analysis, and comprehensive evaluation framework for seamless NHS workflow integration.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=NHS+AI+System",
                    ProjectUrl = "/projects/data-mining",
                    Tags = new[] { "Data Mining", "Text Analytics", "GPT-4 API", "Healthcare AI", "NHS Integration", "SpaCy NER", "CRISP-DM", "GDPR Compliance", "Speech-to-Text", "Medical Documentation" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Numerical Methods for Predator-Prey Systems",
                    Description = "Advanced numerical computation coursework implementing and analyzing differential equation solvers for predator-prey models (Lotka-Volterra equations). Built custom Euler method and compared performance against SSPRK3 and Runge-Kutta methods across multiple time steps. Features comprehensive error analysis with absolute error calculations, Euclidean norm computations, and stability assessment. Includes CPU core isolation for accurate performance benchmarking, mathematical convergence analysis, and professional scientific computing practices. Demonstrates exceptional understanding of numerical methods theory and implementation complexity for undergraduate-level coursework.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Numerical+Methods",
                    ProjectUrl = "/projects/numerical-computation",
                    Tags = new[] { "Python", "NumPy", "Matplotlib", "Numerical Analysis", "Differential Equations", "Scientific Computing", "Performance Analysis", "Mathematical Modeling" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = false
                },
                new Project
                {
                    Title = "AI-Powered Mental Health App Design",
                    Description = "Comprehensive software engineering design brief for a £100,000 mental health app targeting university students globally. Features advanced ChatGPT-4 Turbo API integration, multi-stakeholder analysis, and sophisticated system architecture. Includes university system integration, crisis intervention protocols, and comprehensive privacy controls. Demonstrates professional software engineering methodology with agile development planning, ethical considerations, and legal compliance (GDPR, Data Protection Act). Designed for global scalability across universities with local encryption, cultural personalization, and healthcare service integration (BetterHelp, NHS).",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Mental+Health+App",
                    ProjectUrl = "/projects/mental-health-app",
                    Tags = new[] { "Software Engineering", "AI Integration", "ChatGPT-4", "System Design", "UML", "Agile Development", "Ethics & Privacy", "Healthcare Technology" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = true
                },
                new Project
                {
                    Title = "TerraTracker - GPS Route Tracking Platform",
                    Description = "Collaborative 6-person team project developing a full-stack GPS route tracking and sharing application using professional agile methodologies. Built with Flask, PostgreSQL, and Leaflet.js for interactive mapping. Features comprehensive sprint documentation, Git workflow with branches and pull requests, Docker containerization, and CI/CD pipeline. Demonstrates professional software engineering practices including version control, issue tracking, wiki documentation, and team collaboration. Includes user authentication, route visualization, social sharing features, and activity analytics for outdoor enthusiasts.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=TerraTracker",
                    ProjectUrl = "/projects/terratracker",
                    Tags = new[] { "Team Project", "Flask", "Python", "PostgreSQL", "Agile", "Git Workflow", "Docker", "CI/CD", "Leaflet.js", "RESTful API" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Project Synapse - AI Healthcare Documentation System",
                    Description = "Comprehensive business plan for an AI-powered healthcare documentation system targeting NHS doctors and healthcare professionals. Features advanced speech-to-text technology, automated patient record generation, and seamless NHS system integration. Addresses critical NHS administrative burden affecting 1.8 million healthcare workers with £2 billion government digitalization support. Includes detailed market feasibility analysis, industry research using IBISWorld data, Blue Ocean Strategy competitive analysis, and financial viability assessment. Demonstrates professional business planning methodology with stakeholder analysis, target market segmentation, and scalable implementation strategy.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Project+Synapse",
                    ProjectUrl = "/projects/business-plan",
                    Tags = new[] { "Business Planning", "Healthcare Technology", "AI Documentation", "NHS Integration", "Market Research", "Blue Ocean Strategy", "Entrepreneurship", "Speech-to-Text" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Advanced CFD Blood Flow Simulation using XLB",
                    Description = "GPU-accelerated Lattice Boltzmann Method (LBM) framework for simulating pulsatile blood flow in arterial geometries with aneurysms. Extended XLB framework with custom boundary conditions, non-Newtonian Carreau-Yasuda rheology model, and wall shear stress computation for clinical risk assessment. Achieved 2,340+ MLUPS performance on RTX 3060 Mobile with time-dependent cardiac cycle profiles and physiological accuracy. Developed custom collision operators, 64-point pulsatile inlet conditions, and comprehensive visualization workflows using ParaView and Jupyter notebooks. Establishes foundation for transitioning LBM simulations from research to clinical aneurysm rupture risk assessment, addressing critical need for faster CFD analysis compared to traditional finite element methods.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=XLB+CFD+Simulation",
                    ProjectUrl = "/vascular-blood-flow",
                    Tags = new[] { "Python", "XLB Framework", "CUDA", "CFD", "Lattice Boltzmann Method", "GPU Computing", "Medical Simulation", "Blood Flow", "Aneurysm Risk Assessment", "ParaView", "Docker" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = true
                },
                new Project
                {
                    Title = "Advanced Machine Learning: Regression & Dimensionality Reduction",
                    Description = "Comprehensive machine learning coursework implementing advanced regression techniques and dimensionality reduction algorithms for complex data analysis. Features polynomial regression, ridge regression, principal component analysis (PCA), and independent component analysis (ICA) implementations. Demonstrates mastery of statistical modeling, feature extraction, data preprocessing, and model evaluation techniques. Includes practical applications to real-world datasets with comprehensive performance analysis and visualization of high-dimensional data structures.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Machine+Learning",
                    ProjectUrl = "/projects/machine-learning",
                    Tags = new[] { "Python", "Machine Learning", "Regression Analysis", "PCA", "ICA", "Statistical Modeling", "Data Science", "NumPy", "Scikit-learn", "Dimensionality Reduction" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = true
                },
                new Project
                {
                    Title = "Azure Serverless Inventory Management System",
                    Description = "Enterprise-grade distributed systems project implementing a cloud-native inventory management solution using Azure serverless architecture. Features Azure Functions for compute, Cosmos DB for data persistence, Event Grid for event-driven communication, and API Management for scalable API endpoints. Demonstrates mastery of microservices architecture, event-driven design patterns, cloud deployment strategies, and modern DevOps practices. Includes comprehensive monitoring, logging, and automated deployment pipelines for production-ready distributed applications.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Distributed+Systems",
                    ProjectUrl = "/projects/distributed-systems",
                    Tags = new[] { "Azure", "Serverless", "Distributed Systems", "Cloud Computing", "Microservices", "Event-Driven Architecture", "DevOps", "API Management", "Cosmos DB", "Azure Functions" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = true
                },
                new Project
                {
                    Title = "AI Search Algorithms: Sliding Block Puzzle Analysis",
                    Description = "Artificial intelligence coursework implementing and analyzing various search algorithms for solving sliding block puzzles and robot worker scenarios. Features breadth-first search, depth-first search, A* algorithm, and heuristic optimization techniques. Demonstrates understanding of search space complexity, algorithm efficiency, and intelligent problem-solving strategies. Includes comprehensive performance analysis comparing different search approaches and optimization of heuristic functions for complex state-space exploration.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=AI+Search",
                    ProjectUrl = "/projects/ai-search-algorithms",
                    Tags = new[] { "Python", "Artificial Intelligence", "Search Algorithms", "A* Algorithm", "Heuristics", "Problem Solving", "State Space Search", "Algorithm Analysis", "BFS", "DFS" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Machine Learning: Decision Trees for Fraud Detection",
                    Description = "Advanced artificial intelligence project implementing decision tree algorithms for financial fraud detection systems. Features ID3 algorithm implementation, entropy calculation, information gain optimization, and tree pruning techniques. Demonstrates mastery of supervised learning, classification algorithms, and practical machine learning applications in financial security. Includes comprehensive evaluation metrics, cross-validation techniques, and performance optimization for real-world fraud detection scenarios.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Decision+Trees",
                    ProjectUrl = "/projects/ai-decision-trees",
                    Tags = new[] { "Python", "Machine Learning", "Decision Trees", "Fraud Detection", "Classification", "ID3 Algorithm", "Data Mining", "Financial Security", "Supervised Learning", "Feature Selection" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Advanced Parallel Computing: OpenMP, MPI & OpenCL",
                    Description = "Comprehensive parallel computing specialization spanning three major paradigms: shared memory (OpenMP), distributed systems (MPI), and GPU acceleration (OpenCL). Implemented Conway's Game of Life with OpenMP parallelization, developed scalable MPI applications with multi-machine performance analysis achieving 3.09x speedup, and created GPU-accelerated heat equation solvers using OpenCL kernels. Demonstrates mastery of data dependencies, red-black patterns, message passing, and heterogeneous computing across the complete parallel programming ecosystem. Includes rigorous performance benchmarking and optimization techniques for high-performance computing applications.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Parallel+Computing",
                    ProjectUrl = "/projects/parallel-computing",
                    Tags = new[] { "OpenMP", "MPI", "OpenCL", "C Programming", "Parallel Computing", "GPU Programming", "Distributed Systems", "Performance Analysis", "High Performance Computing", "CUDA" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Autonomous Robot Navigation & Object Detection",
                    Description = "Advanced ROS2-based autonomous robotics project implementing computer vision, motion planning, and intelligent navigation for TurtleBot3 simulation with sophisticated exploration strategies and precision object interaction. Features SLAM (Simultaneous Localization and Mapping), YOLOv8 object detection, OpenCV image processing, and advanced pathfinding algorithms. Demonstrates integration of multiple AI and robotics technologies including sensor fusion, behavior trees, and real-time decision making for complex autonomous navigation tasks.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Autonomous+Robotics",
                    ProjectUrl = "/projects/autonomous-robotics",
                    Tags = new[] { "ROS2", "Python", "Computer Vision", "SLAM", "YOLOv8", "OpenCV", "TurtleBot3", "Gazebo", "Motion Planning", "Autonomous Systems" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = true
                },
                new Project
                {
                    Title = "3D OpenGL Renderer with Advanced Graphics Pipeline",
                    Description = "Computer graphics coursework implementing a comprehensive 3D rendering application using modern OpenGL with complete projective graphics pipeline. Features terrain visualization using real-world elevation data, advanced lighting models, texture mapping, and interactive space launch simulation. Built as collaborative project demonstrating mastery of 3D mathematics, shader programming, and real-time rendering techniques with focus on performance optimization and visual fidelity for complex scene rendering.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=OpenGL+Renderer",
                    ProjectUrl = "/projects/opengl-renderer",
                    Tags = new[] { "OpenGL", "C++", "Computer Graphics", "3D Rendering", "Shader Programming", "Terrain Visualization", "Real-time Rendering", "Collaborative Project" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = false
                },
                new Project
                {
                    Title = "xv6 Operating System Development & Custom Shell",
                    Description = "Advanced operating systems coursework implementing a complete Unix-style shell and extending the xv6 educational operating system with custom system calls and kernel modifications. Features comprehensive shell functionality including command parsing, process management, I/O redirection, and signal handling. Demonstrates deep understanding of operating system internals, kernel programming, and system-level C development with focus on memory management, process scheduling, and inter-process communication.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=xv6+OS",
                    ProjectUrl = "/projects/xv6-operating-system",
                    Tags = new[] { "C Programming", "Operating Systems", "Unix Shell", "Kernel Development", "System Calls", "Process Management", "Memory Management", "xv6" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Quantum Dot Investment Case for Photonic Quantum Computing",
                    Description = "Comprehensive nanotechnology business case exploring quantum dot manufacturing for the revolutionary potential of light-based quantum computation. Features detailed market analysis, investment feasibility assessment, and technical evaluation of quantum dot applications in photonic quantum computing systems. Demonstrates understanding of emerging nanotechnology markets, quantum computing principles, and strategic business planning for cutting-edge technology investments with focus on commercialization potential and scalability challenges.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=Quantum+Dots",
                    ProjectUrl = "/projects/quantum-dots",
                    Tags = new[] { "Nanotechnology", "Quantum Computing", "Business Analysis", "Investment Strategy", "Market Research", "Photonic Computing", "Quantum Dots", "Technology Assessment" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = false
                },
                new Project
                {
                    Title = "Custom Image Format & Compression System",
                    Description = "Sophisticated systems programming project implementing custom image file formats with advanced compression algorithms and bit-level manipulation. Features comprehensive image processing capabilities including custom binary formats, lossless compression techniques, and efficient memory management. Demonstrates mastery of low-level C programming, file I/O operations, and algorithm optimization with focus on performance-critical systems programming and data structure design.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=C+Programming",
                    ProjectUrl = "/projects/c-programming",
                    Tags = new[] { "C Programming", "Image Processing", "Compression Algorithms", "File Formats", "Memory Management", "Systems Programming", "Bit Manipulation", "Algorithm Optimization" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = false
                },
                new Project
                {
                    Title = "2D Graphics Fundamentals & Rasterization Engine",
                    Description = "Computer graphics coursework implementing fundamental 2D graphics algorithms including line drawing, triangle rasterization, and image processing with comprehensive performance analysis and testing frameworks. Features pixel manipulation, geometric rendering algorithms, and mathematical foundations of computer graphics. Built from scratch in C++ demonstrating mastery of graphics primitives, algorithm implementation, and performance optimization for real-time rendering applications.",
                    ImageUrl = "https://placehold.co/600x400/1a1a1a/ffffff?text=2D+Graphics",
                    ProjectUrl = "/projects/graphics-fundamentals",
                    Tags = new[] { "C++", "Computer Graphics", "2D Rendering", "Rasterization", "Algorithm Implementation", "Performance Analysis", "Graphics Primitives", "Mathematical Modeling" },
                    Category = ProjectCategory.Academic,
                    IsFeatured = false
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