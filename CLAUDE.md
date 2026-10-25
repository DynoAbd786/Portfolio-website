# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Full-stack personal portfolio website built as a .NET 10 Blazor Web App: pages are rendered on the server (static SSR) by ASP.NET Core, which also hosts the Web API, and a few components run interactively in the browser via WebAssembly. Features a project showcase (academic, professional, personal), PDF downloads, a contact form and an AI assistant.

## Architecture Overview

### Solution Structure
- **`website.api/`** - The host: ASP.NET Core app that renders pages (`Components/App.razor`, `Components/Routes.razor`), serves static assets and the `/api/*` controllers
- **`website/`** - Blazor WebAssembly client project (referenced by the host): all pages (`Pages/`), layout and shared components (`Shared/`), static assets (`wwwroot/`)
- **`McpServer/`** - Standalone Model Context Protocol server (not actively used)
- **Solution file**: `website.sln` manages all projects

### Render Modes
- **Static SSR (default)**: every page renders to plain HTML on the server; no WebAssembly needed to see content
- **Interactive WebAssembly** (`@rendermode InteractiveWebAssembly`): `ContactForm`, `MedicalSimulationProject` (image gallery), `HomeLabProject` (bridge status polling), and `AiTerminal` (in `MainLayout`, `prerender: false` because it reads localStorage)
- Interactive components are also prerendered on the server, so don't call `HttpClient` or JS from `OnInitialized` in them; do it in `OnAfterRender(firstRender)` or event handlers
- The layout is static: the mobile menu and the chat panel open/close via `wwwroot/js/site.js`; the chat panel sits in a `data-permanent` container so enhanced navigation keeps it open and running

### Client-Server Communication
- One origin in development and production: the site and API both run on `website.api`
- Server-rendered pages get project data through `website.Data.IProjectSource`, implemented by `website.api/Services/ApiProjectSource.cs` (reads `IProjectService` directly, no HTTP)
- Interactive components call the API with `HttpClient` (base address = site origin, set in `website/Program.cs`)

## Development Commands

```bash
# Run the site (pages + API) — http://localhost:5154, Swagger at /swagger
cd website.api
dotnet run

# Watch mode
cd website.api && dotnet watch

# Build / restore everything
dotnet build
dotnet restore
```

Requires the .NET 10 SDK. Tailwind CSS (v3.4, standalone CLI, no Node) is compiled during the `website` build into `website/wwwroot/css/tailwind.css` (generated, gitignored); the CLI is downloaded once into `.tools/`. Config: `website/tailwind.config.js`, input `website/Styles/tailwind.css`. Classes must appear literally in `.razor`/`.cs`/`site.js` files to be generated. `dotnet watch` hot reload doesn't recompile Tailwind; while editing markup, run the CLI in watch mode alongside it from `website/`: `../.tools/tailwindcss/v3.4.17/tailwindcss-linux-x64 -c tailwind.config.js -i Styles/tailwind.css -o wwwroot/css/tailwind.css --watch` (the binary appears after the first build).

### Docker Commands
```bash
# Production container (what Render builds)
docker build -t portfolio-app .
docker run -p 8080:10000 portfolio-app   # http://localhost:8080

# docker-compose: production-like site on :8080, or watch mode on :7232
docker-compose up --build
docker-compose --profile dev up api-dev
docker-compose down
```

The listening URL comes from `ASPNETCORE_URLS` (Render/Docker: port 10000); without it the app listens on `http://0.0.0.0:5154`.

## Backend API Architecture

### Controllers & Endpoints
- **`ProjectsController`**: `/api/projects` - project data
  - `GET /api/projects` - All projects
  - `GET /api/projects/by-url?url={url}` - Project by URL
  - `GET /api/projects/categories/{category}` - Projects by category
- **`ContactController`**: `/api/contact` - Email functionality via Postmark
  - `POST /api/contact` - Send contact form email
- **`ChatController`**: `/api/chat/*` - AI assistant (Gemini, Groq, local Ollama via the bridge)
- **`McpController`**: `/api/mcp` - Model Context Protocol JSON-RPC 2.0 server
  - `POST /api/mcp` - Main MCP endpoint for AI agents (Claude Desktop)
  - `GET /api/mcp` - Initialize endpoint
  - `GET|POST /api/mcp/sse`, `POST /api/mcp/message` - SSE transport
  - `GET /api/mcp/status/bridge` - Local bridge status (used by the HomeLab page)

### Services (Dependency Injection)
Registered in `website.api/Program.cs`:
- **`IProjectService`**: Project data (single source of truth)
- **`IEmailService`**: Email sending via Postmark (requires POSTMARK_SERVER_TOKEN in .env)
- **`IMcpService`**: JSON-RPC 2.0 protocol handler for MCP requests
- **`IOAuthService`**: OAuth 2.0 authentication for MCP clients
- **`website.Data.IProjectSource` / `website.Data.ProjectService`**: project data for server-rendered pages
- Typed `HttpClient`s for the Gemini/Groq/Discord/Ollama services. `AddHttpClient` also registers the default transient `HttpClient` that prerendered interactive components resolve — don't register a scoped `HttpClient` (the singleton `DiscordService` would capture it)

### Middleware Pipeline
Order matters (`website.api/Program.cs`):
1. **Forwarded Headers** - proxy/CDN (Render, Cloudflare)
2. **HEAD as GET** - HEAD requests run as GET with the body dropped (pages and API GETs only match GET). Before status code pages, whose re-execute does its own routing
3. **Status code pages** - unknown page URLs re-execute `/not-found`; disabled for `/api/*` so API 404s keep their own bodies. Must stay early so the re-executed request passes through auth and antiforgery
4. **`UseRouting`** - explicit, so it runs after the HEAD rewrite
5. **Swagger** - Development only
6. **CORS** - AllowMcpClients policy globally
7. **Request Logging** - custom middleware
8. **Authentication** → **MCP OAuth** → **Authorization**
9. **Antiforgery** - required by Razor component endpoints
10. **`MapStaticAssets`** - fingerprinted assets cached as immutable; others (e.g. PDFs) revalidate via ETag
11. **Controllers**, then **`MapRazorComponents<App>`** with the WebAssembly render mode and the `website` assembly

### Key API Features
- **CORS Policies**: MCP clients may call from any origin
- **Environment Variables**: DotNetEnv loads `website.api/.env` for local development
- **OAuth 2.0**: Custom implementation for MCP client authentication

## Frontend Architecture

### Pages & Components
- **Project Pages**: Individual pages for each project (Academic, Professional, Personal) in `website/Pages/`
- **Shared Components**: `ProjectCard.razor` for consistent project display
- **Layout**: `MainLayout.razor` with responsive navigation; sets the default `<PageTitle>` (a page's own `<PageTitle>` wins)
- **Document head**: `website.api/Components/App.razor` (meta tags, per-page canonical URL, CDN fonts/icons, stylesheets). `tailwind.css` is the last stylesheet (as the old CDN script injected it) so utilities override `app.css`, Font Awesome, KaTeX and highlight.js
- **Not found**: `website/Pages/NotFound.razor` (`/not-found`)

### Project Data Flow
1. A page injects `website.Data.ProjectService`
2. On the server, `ApiProjectSource` maps `IProjectService` data to `website.Data.Project`
3. Projects categorized by `ProjectCategory` enum (Personal, Professional, Academic)
4. Pages display projects in category-grouped sections

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

### Environment Configuration
Create `.env` file in `website.api/` for local development:
```
POSTMARK_SERVER_TOKEN=your_token_here
# Add OAuth tokens if using MCP features
```

### Project Data Management
- **Single Source of Truth**: Project data defined in `website.api/Services/ProjectService.cs`
- **Pages**: Read it in-process through `IProjectSource`; external clients use `/api/projects`
- **Error Handling**: `ProjectService` returns an empty list if loading fails

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
