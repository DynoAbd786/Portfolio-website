# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Full-stack personal portfolio website with Blazor WebAssembly frontend and ASP.NET Core Web API backend. Features comprehensive project showcase with academic coursework, professional work, and personal projects, including PDF document downloads and email contact functionality.

## Architecture Overview

### Two-Project Solution Structure
- **`website/`** - Blazor WebAssembly client (frontend)
- **`website.api/`** - ASP.NET Core Web API server (backend)
- **Solution file**: `website.sln` manages both projects

### Client-Server Communication
- **Development**: Client (`localhost:5195`) calls API (`localhost:5154`)
- **Production**: API serves both endpoints and static files from single container
- **API Base URL**: Configured in `website/Program.cs` with environment-specific logic

## Development Commands

### Both Projects (Full-Stack Development)
```bash
# Terminal 1 - Start API server
cd website.api
dotnet run

# Terminal 2 - Start Blazor client
cd website
dotnet run

# Client: http://localhost:5195
# API: http://localhost:5154 (with Swagger at /swagger)
```

### Individual Projects
```bash
# Build both projects
dotnet build

# Restore dependencies for solution
dotnet restore

# Run specific project
cd website && dotnet run
cd website.api && dotnet run

# Watch mode for development
cd website && dotnet watch
cd website.api && dotnet watch
```

### Docker Commands
```bash
# Development with Docker Compose
docker-compose up --build

# Production container
docker build -t portfolio-app .
docker run -p 8080:10000 portfolio-app
```

## Backend API Architecture

### Controllers & Endpoints
- **`ProjectsController`**: `/api/projects` - CRUD operations for project data
- **`ContactController`**: `/api/contact` - Email functionality via Postmark
- **`McpController`**: `/api/mcp` - Model Context Protocol server for AI agents

### Services (Dependency Injection)
- **`IProjectService`**: Business logic for project data management
- **`IEmailService`**: Email sending via Postmark (configured in appsettings)
- **`IMcpService`**: JSON-RPC 2.0 protocol implementation for AI integration

### Key API Features
- **CORS Configuration**: Allows Blazor client origins in development
- **Static File Serving**: API serves frontend assets in production
- **SPA Fallback Routing**: Non-API routes serve `index.html` for client-side routing
- **Environment Variables**: Uses DotNetEnv for development configuration

## Frontend Architecture

### Core Structure
- **Entry Point**: `website/Program.cs` - Configures HttpClient with API base address
- **Service Registration**: `ProjectService` for API communication
- **Routing**: `App.razor` with MainLayout for consistent page structure

### Pages & Components
- **Project Pages**: Individual pages for each project (Academic, Professional, Personal)
- **Shared Components**: `ProjectCard.razor` for consistent project display
- **Layout**: `MainLayout.razor` with responsive navigation and styling

### Project Data Flow
1. Client `ProjectService` calls API via HttpClient
2. API `ProjectsController` uses `IProjectService` for business logic
3. Projects categorized by `ProjectCategory` enum (Personal, Professional, Academic)
4. Frontend displays projects in category-grouped sections

## Document Management System

### PDF Generation & Storage
- **PowerShell Scripts**: Convert Word documents to PDF for academic projects
- **Storage Location**: `website/wwwroot/docs/` for downloadable documents
- **Naming Convention**: Descriptive names (e.g., `medical-ai-research-proposal.pdf`)

### Academic Project Structure
Each academic project follows standardized format:
- **Typography**: Consistent heading sizes (`text-6xl`, `text-2xl`, `text-xl`)
- **Layout**: Breadcrumbs, header section, hero visual, content sections, downloads
- **Styling**: Tailwind CSS with dark theme and colored accents

## Critical Configuration

### API Port Configuration
- **Client expects API on**: `http://localhost:5154/` (set in `website/Program.cs`)
- **API runs on**: `http://localhost:5154` (set in `website.api/Properties/launchSettings.json`)
- **Production**: Same-origin requests when API serves static files

### Service Registration Patterns
```csharp
// API (website.api/Program.cs)
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Client (website/Program.cs)
builder.Services.AddScoped<website.Data.ProjectService>();
```

### Project Data Management
- **Single Source of Truth**: Project data defined in `website.api/Services/ProjectService.cs`
- **Client Service**: Makes HTTP calls to API, no fallback data
- **Error Handling**: Returns empty list if API unavailable

## Common Development Tasks

### Adding New Academic Projects
1. Add project data to `website.api/Services/ProjectService.cs`
2. Create individual project page in `website/Pages/`
3. Convert academic documents to PDF using PowerShell scripts
4. Follow established typography and layout patterns

### Testing API Endpoints
```bash
# Start API and test endpoints
cd website.api && dotnet run
curl http://localhost:5154/api/projects
curl "http://localhost:5154/api/projects/by-url?url=/projects/data-mining"
```

### Project Categories
- **Personal**: Independent development projects (TaskFlow, Portfolio)
- **Professional**: Internship and work-related projects (Medical Simulation, pNanoLocz)
- **Academic**: University coursework (Data Mining, Software Engineering, Business Plan)