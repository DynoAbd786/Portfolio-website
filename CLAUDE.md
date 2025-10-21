# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Full-stack personal portfolio website with Blazor WebAssembly frontend and ASP.NET Core Web API backend. Features comprehensive project showcase with academic coursework, professional work, and personal projects, including PDF document downloads and email contact functionality.

## Architecture Overview

### Solution Structure
- **`website/`** - Blazor WebAssembly client (frontend)
- **`website.api/`** - ASP.NET Core Web API server (backend)
- **`McpServer/`** - Standalone Model Context Protocol server (not actively used)
- **Solution file**: `website.sln` manages all projects

### Client-Server Communication
- **Development**: Client (`localhost:5195`) calls API (`localhost:5154`)
- **Production**: API serves both API endpoints and static files from single container
- **API Base URL**: Configured in `website/Program.cs:15-17` with environment-specific logic
  - Development: `http://localhost:5154/`
  - Production: Same-origin (API serves frontend)

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
# Full development environment (API + frontend)
docker-compose up --build
# Accessible at: http://localhost:8080

# API-only development mode (for separate frontend dev)
docker-compose --profile dev up api-dev
# API at: https://localhost:7233 or http://localhost:7232

# Production container
docker build -t portfolio-app .
docker run -p 8080:8080 portfolio-app

# Stop all services
docker-compose down
```

## Backend API Architecture

### Controllers & Endpoints
- **`ProjectsController`**: `/api/projects` - CRUD operations for project data
  - `GET /api/projects` - All projects
  - `GET /api/projects/by-url?url={url}` - Project by URL
  - `GET /api/projects/categories/{category}` - Projects by category
- **`ContactController`**: `/api/contact` - Email functionality via Postmark
  - `POST /api/contact` - Send contact form email
- **`McpController`**: `/api/mcp` - Model Context Protocol JSON-RPC 2.0 server
  - `POST /api/mcp` - Main MCP endpoint for AI agents (Claude Desktop)
  - `GET /api/mcp` - Initialize endpoint
  - `GET /api/mcp/tools` - Tools list endpoint
  - `GET /api/mcp/info` - Server info endpoint

### Services (Dependency Injection)
All services registered as scoped in `website.api/Program.cs:23-26`:
- **`IProjectService`**: Business logic for project data management
- **`IEmailService`**: Email sending via Postmark (requires POSTMARK_SERVER_TOKEN in .env)
- **`IMcpService`**: JSON-RPC 2.0 protocol handler for MCP requests
- **`IOAuthService`**: OAuth 2.0 authentication for MCP clients

### Middleware Pipeline
Critical ordering in `website.api/Program.cs:78-118`:
1. **Forwarded Headers** - Handle proxy/CDN scenarios (line 78)
2. **Swagger** - Development only (lines 81-84)
3. **CORS** - AllowMcpClients policy globally (line 91)
4. **Request Logging** - Custom middleware for debugging (line 94)
5. **Authentication** - JWT Bearer (line 97)
6. **MCP OAuth** - Custom OAuth middleware (line 98)
7. **Authorization** (line 99)
8. **Static Files** - Serve frontend assets (lines 103-112)
9. **Controller Mapping** (line 115)
10. **SPA Fallback** - index.html for non-API routes (line 118)

### Key API Features
- **CORS Policies**: Two separate policies for Blazor client and MCP clients
- **Static File Serving**: API serves frontend assets in production with caching headers
- **SPA Fallback Routing**: Non-API routes serve `index.html` for client-side routing
- **Environment Variables**: Uses DotNetEnv for .env file loading (line 10-13)
- **OAuth 2.0**: Custom implementation for MCP client authentication
- **Forwarded Headers**: Production configuration for proxy scenarios (lines 65-73)

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
// API (website.api/Program.cs:23-26)
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IMcpService, McpService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();

// Client (website/Program.cs:19-20)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseAddress) });
builder.Services.AddScoped<website.Data.ProjectService>();
```

### Environment Configuration
Create `.env` file in `website.api/` for local development:
```
POSTMARK_SERVER_TOKEN=your_token_here
# Add OAuth tokens if using MCP features
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
- **Academic**: University coursework (25+ individual project pages in `website/Pages/`)

## Model Context Protocol (MCP) Integration

This portfolio implements a custom MCP server that allows AI agents (like Claude Desktop) to interact with the portfolio:

### MCP Architecture
- **Protocol**: JSON-RPC 2.0 over HTTP POST
- **Endpoint**: `/api/mcp` (see `website.api/Controllers/McpController.cs`)
- **Service**: `McpService` handles protocol implementation (`website.api/Services/McpService.cs`)
- **Authentication**: Custom OAuth 2.0 middleware (`website.api/Middleware/McpOAuthMiddleware.cs`)

### MCP Capabilities
1. **Resources**: Access to profile, projects, contact info, ethos, and AI integration philosophy
2. **Tools**:
   - `submit_contact` - Send contact form messages
   - `search_projects` - Search projects by category, technology, or keyword

### MCP Protocol Flow
1. Client sends `initialize` request → Server responds with capabilities
2. Client sends `notifications/initialized` → Handshake complete
3. Client can call `tools/list` → Discover available tools
4. Client can call `tools/call` → Execute tools with parameters
5. Client can call `resources/list` → List available resources
6. Client can call `resources/read` → Read specific resources

### Adding New MCP Tools
To add a new tool:
1. Add tool definition to `HandleToolsList()` in `McpService.cs:368`
2. Add tool handler to switch statement in `HandleToolCall()` at line 461
3. Implement handler method following pattern of `HandleSubmitContact()` or `HandleSearchProjects()`

## Important Note

*This project was completed during a period of long-term illness, which impacted development timeline and some features may remain incomplete. The core functionality and technical implementation remain solid despite these challenging circumstances.*

GitHub Repository: [Portfolio-website](https://github.com/DynoAbd786/Portfolio-website)
