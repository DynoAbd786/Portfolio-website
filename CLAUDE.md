# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Full-stack personal portfolio website built as a .NET 10 Blazor Web App: pages are rendered on the server (static SSR) by ASP.NET Core, which also hosts the Web API, and a few components run interactively in the browser via WebAssembly. Features a project showcase (academic, professional, personal), PDF downloads, a contact form and an AI assistant.

## Architecture Overview

### Solution Structure
- **`website.api/`** - The host: ASP.NET Core app that renders pages (`Components/App.razor`, `Components/Routes.razor`), serves static assets and the `/api/*` controllers
- **`website/`** - Blazor WebAssembly client project (referenced by the host): all pages (`Pages/`), layout and shared components (`Shared/`), static assets (`wwwroot/`)
- **Solution file**: `website.sln` manages all projects

### Render Modes
- **Static SSR (default)**: every page renders to plain HTML on the server; no WebAssembly needed to see content
- **Interactive WebAssembly** (`@rendermode InteractiveWebAssembly`): only `ImageGallery` (medical simulation and machine learning pages) and `AiTerminal`. The .NET runtime (about 3 MB) loads only on pages with one of these, so keep pages static where possible (a test lists the pages allowed to start it)
- Interactive components are also prerendered on the server, so don't call `HttpClient` or JS from `OnInitialized` in them; do it in `OnAfterRender(firstRender)` or event handlers
- The layout is static: the mobile menu and the chat panel open/close via `wwwroot/js/site.js`. The assistant (`AiTerminal`, `prerender: false` because it reads localStorage) lives on its own page, `/chat-panel` (`BareLayout`, noindex), which `mkChat` loads into an iframe the first time the panel opens; the panel is `data-permanent`, so the frame and conversation survive enhanced navigation. The panel's header (`MainLayout`) is the assistant's only header: its past-chats and new-chat buttons reach into the frame through `mkChat.command` → `mkChatFrame` (site.js) → `AiTerminal.Command`
- `ContactForm` is a static SSR form (`EditForm` with `FormName`, `Enhance`): it posts to the server, which emails through `website.Data.IContactSender` (`website.api/Services/EmailContactSender.cs`)

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

Render: the live service (`Portfolio-website`, Frankfurt, free plan) was created in the dashboard, so its settings live there, not in `render.yaml` (kept in sync by hand). Its health check polls `/health` — keep that endpoint, or a deploy times out and Render keeps the previous version.

## Backend API Architecture

### Controllers & Endpoints
- **`ProjectsController`**: `/api/projects` - project data
  - `GET /api/projects` - All projects
  - `GET /api/projects/by-url?url={url}` - Project by URL
  - `GET /api/projects/categories/{category}` - Projects by category
- **`ContactController`**: `/api/contact` - Email functionality via Resend
  - `POST /api/contact` - Send contact form email
- **`ChatController`**: `/api/chat` - AI assistant, always DeepSeek Flash (`DeepSeekAgentService`, key `DeepSeek__ApiKey`; without it the assistant says it is unavailable). Its system prompt (`AssistantContext`) is built from `HomeContent` (experience, products, stack, open to work) and every project, so it knows what the homepage shows; replies render through `ChatMarkdown`, which opens site links in the main window, not the chat frame.
- **MCP server**: `/api/mcp`, not a controller: the official C# SDK (`ModelContextProtocol.AspNetCore`, `app.MapMcp`), see "Model Context Protocol" below

### Services (Dependency Injection)
Registered in `website.api/Program.cs`:
- **`IProjectService`**: Project data (single source of truth)
- **`IEmailService`**: Email sending via Resend (`EmailService`, a typed `HttpClient`; key `Resend__ApiKey`, sender `Resend:FromEmail`, which must be on a domain verified in Resend). A send counts only when Resend returns an email id; otherwise it logs Resend's reply and the form shows an error
- **`PortfolioTools`**: what an agent can do (search projects, send a contact message); the MCP tools and the assistant both call it
- **`website.Data.IProjectSource` / `website.Data.ProjectService`**: project data for server-rendered pages
- Typed `HttpClient`s for `EmailService` and `DeepSeekAgentService`. `AddHttpClient` also registers the default transient `HttpClient` that prerendered interactive components resolve — don't register a scoped `HttpClient` (a singleton would capture it)

### Middleware Pipeline
Order matters (`website.api/Program.cs`):
1. **Forwarded Headers** - proxy/CDN (Render, Cloudflare)
2. **HEAD as GET** - HEAD requests run as GET with the body dropped (pages and API GETs only match GET). Before status code pages, whose re-execute does its own routing
3. **Status code pages** - unknown page URLs re-execute `/not-found`; disabled for `/api/*` so API 404s keep their own bodies. Must stay early so the re-executed request passes through auth and antiforgery
4. **`UseRouting`** - explicit, so it runs after the HEAD rewrite
5. **Swagger** - Development only
6. **CORS** - AllowMcpClients policy globally
7. **Authorization**
9. **Antiforgery** - required by Razor component endpoints
10. **`MapStaticAssets`** - fingerprinted assets cached as immutable; others (e.g. PDFs) revalidate via ETag
11. **`/health`** (health checks), **Controllers**, then **`MapRazorComponents<App>`** with the WebAssembly render mode and the `website` assembly

### Key API Features
- **CORS Policies**: MCP clients may call from any origin
- **Environment Variables**: DotNetEnv loads `website.api/.env` for local development

## Frontend Architecture

### Look
Moving between pages crossfades the content (a view transition around Blazor's enhanced navigation: `mkPageFade` in `site.js`, styles at the end of `app.css`); the header (`.site-header`) and chat panel are their own layers and stay still. Not for form posts or with reduced motion.
Dark ink background, one cyan accent, Inter and JetBrains Mono. Colour tokens (`ink`, `ink-2`, `line`, `cy`, `lamp`) live in `website/tailwind.config.js`; shared classes (`mono-label`, `btn-primary`, `btn-ghost`, `chip`, `surface`, `grid-bg`, `reveal`) in `website/Styles/tailwind.css`. Use these rather than raw colours. `.reveal` fades content in on scroll, but only once the head script has added `reveal-ready` (so content stays visible without JavaScript).

### Homepage
- **`website/Pages/Index.razor`**: the workshop hero, the tech stack, selected work, what I'm building, experience and the contact form (everything after the hero is wrapped in `.hero-cover`)
- **The hero** (css/app.css): on desktop (≥900px wide and ≥500px tall) it is sticky under the header and fills the screen; the room fills it behind the text, centred at `--frame-x` (the middle of the space right of `.hero-copy`, set by `mkHeroFrame()` in `site.js`), and the page slides over it (`--cover`, `mkHeroCover()`: the room dims, and the scene pauses once covered). Narrower or shorter screens stack it: name (`.hero-head`), room (4:3, edge to edge), then `.hero-body`. Buttons: See my work, Download CV, Get in touch (`btn-primary btn-shine`, the tracing light)
- **The intro** (`wwwroot/js/intro/`): the name build (`name-build.js`, a point cloud wired into a mesh with Delaunator, then lit facets, then text) is the loading screen for the scene. App.razor's head script adds `intro-pending` on a tab's first full load of `/` and on every reload of it (no hash, no `?debug=`, no reduced motion; `sessionStorage['mk-intro-seen']`, tests set `mk-intro-off`). `mkIntro` (site.js) runs `intro.js`: scrolling waits until the name has flown to its place and the room's entrance has played out (`.intro-settling`, `mkWorkshop.entrance()`); the build starts once `mkWorkshop.loaded()` (the scene's script and room are built), the held scene draws its first frame (`mkWorkshop.firstFrame()`) only after the build, and the name lands on the `<h1>` once `mkWorkshop.settled()` (or after 10 s), so the scene's heavy moments never stall the animation; a click, Tab or leaving the page ends it at once; then `mkWorkshop.startIntro()` plays the scene's held entrance. A reload of `/` starts at the top; other pages keep the browser's usual reload behaviour. Delaunator and Inter 800 are served from `wwwroot/lib/delaunator/` and `wwwroot/fonts/`; `?debug=namebuild` exposes the build on its own
- **`website/Data/HomeContent.cs`**: everything the homepage says that isn't project data (the tech stack, selected work, products, experience, and the workshop's 11 hotspots). `WorkshopJson()` embeds the hotspots in the page for the 3D scene
- **The workshop** (`website/wwwroot/js/workshop/`, three.js 0.160.0 served from `wwwroot/lib/three/`, mapped in the import map in `App.razor`):
  - `workshop.js` — `mount(root, data, { holdIntro })`: renderer, camera, frame loop, intro flight (held until `startIntro()`); on desktop it fits the room to the stage height or the space right of the text, whichever is tighter (`camera.zoom`, film offset, a vertical projection shift), and centres a clicked object right of its card; `?debug=workshop` exposes `root.__workshop` (pins, forceFps, snapshot, roomRect, project, introT, frames), `?still=1` renders one frame
  - `room.js` (the room, desk, monitors, robot, bookcase…), `props.js` (stent, microscope cart, Azure cloud), `screens.js` (the Synergy and HireForge monitors), `hotspots.js` (clickable objects and camera poses), `ui.js` (hover tag, cards, tour), `post.js` (outline + bloom), `quality.js` (adaptive quality and the too-slow check), `kit.js` (shared materials and geometry)
  - `window.mkWorkshop` in `site.js` mounts and disposes the scene across enhanced navigation (`#hero` is `data-permanent`). On no WebGL, a failed load or a device too slow for it (remembered for the session), the page keeps the still image with link pins; under 640px the pins become a link list
  - The still image (`wwwroot/images/workshop/still.webp` and `@2x`) is rendered from the live scene: run the site, then `uv run --with playwright python tools/render-workshop-still.py http://127.0.0.1:5199`. It prints new `PinX`/`PinY` values for `HomeContent` and `--still-room` for `app.css`; re-run it whenever the scene or its framing changes

### Pages & Components
- **Project Pages**: one page per project in `website/Pages/` (`*Project.razor`), all on the shared template (see below)
- **`/projects`** (`Projects.razor`): one grid of `ProjectCard`s, filtered by plain links `?field=<slug>&category=<personal|professional|academic>`; unknown values are ignored
- **Layout**: `MainLayout.razor` with responsive navigation; sets the default `<PageTitle>` (a page's own `<PageTitle>` wins)
- **Document head**: `website.api/Components/App.razor` (meta tags, per-page canonical URL, CDN fonts/icons, stylesheets). `tailwind.css` is the last stylesheet (as the old CDN script injected it) so utilities override `app.css`, Font Awesome, KaTeX and highlight.js
- **Not found**: `website/Pages/NotFound.razor` (`/not-found`)

### Project Data Flow
1. A page injects `website.Data.ProjectService`
2. On the server, `ApiProjectSource` maps `IProjectService` data to `website.Data.Project`
3. Each project has a `ProjectCategory` (Personal, Professional, Academic), one or more `ProjectField`s (AI & ML, computer vision & graphics, scientific & biomedical, cloud & distributed, systems & infrastructure, product & business; labels and URL slugs in `website/Data/ProjectFields.cs`) and a one-sentence `Result` shown under its title

### Project Page Template
Every project page is `<ProjectPage Url="/projects/..." Kicker="Academic Project · Year 2">` wrapping `<ProjectSection Title="..." Subtitle="..." Icon="fas fa-...">` panels. `ProjectPage` renders the breadcrumb, title, result and chips from the project data, plus the next-project link, so a page only writes its sections. Inside sections use the shared classes (`chip`, `surface`, `text-slate-*`, `border-line`, `text-cy`), not rainbow colours; `StyleTests` fails on any old palette colour inside `main` (a short list of colours that carry meaning, such as form errors and success messages, is allowed). Interactive bits are their own components (`ImageGallery`), never a page-level render mode.

## Document Management System

### PDF Generation & Storage
- **PowerShell Scripts**: Convert Word documents to PDF for academic projects
- **Storage Location**: `website/wwwroot/docs/` for downloadable documents
- **Naming Convention**: Descriptive names (e.g., `medical-ai-research-proposal.pdf`)

## Critical Configuration

### Environment Configuration
Create `.env` file in `website.api/` for local development:
```
Resend__ApiKey=re_...   # a sending-only key is enough
```

### Project Data Management
- **Single Source of Truth**: Project data defined in `website.api/Services/ProjectService.cs`
- **Pages**: Read it in-process through `IProjectSource`; external clients use `/api/projects`
- **Error Handling**: `ProjectService` returns an empty list if loading fails

## Common Development Tasks

### Adding a New Project
1. Add project data to `website.api/Services/ProjectService.cs`, including `Fields` and a one-sentence `Result`
2. Create its page in `website/Pages/` on the template: `<ProjectPage Url=...>` with `<ProjectSection>` panels
3. Add its route to `Routes` in `tests/website.tests/ProjectPageTemplateTests.cs` (a test fails until you do)
4. Convert any documents to PDF into `website/wwwroot/docs/`

### Tests
Two suites, both from the repository root (the .NET 10 SDK must be on `PATH`):
```bash
# Host tests: xUnit + WebApplicationFactory + AngleSharp, the site in memory (routes, content, data, templates)
dotnet test tests/website.tests

# Browser tests: Playwright in Chrome against the real site on port 5199 (the workshop, fallbacks, interactive islands)
uv run -q --with pytest --with pytest-playwright pytest tests/browser -q
```
See `tests/browser/README.md`. The browser fixture refuses to start if port 5199 is already taken. Browser tests start with the homepage intro already seen (conftest's `context` fixture); mark a test `@pytest.mark.intro` to run with it (`test_intro.py`).

### Testing API Endpoints
```bash
# Start API and test endpoints
cd website.api && dotnet run
curl http://localhost:5154/api/projects
curl "http://localhost:5154/api/projects/by-url?url=/projects/data-mining"
```

## Analytics (Umami)

Cookieless analytics with [Umami Cloud](https://cloud.umami.is) (website ID in `appsettings.json`, `Umami:WebsiteId`); the footer's `.privacy-note` says what's counted, including that Ask AI questions are recorded.
- **Page views**: Umami's script, added by `App.razor` only in Production (never local runs or tests) and never on `/chat-panel`; `data-domains` limits it to mkkai.dev, and it follows enhanced navigation itself (it wraps `pushState`). `data-exclude-hash` keeps in-page glides from counting
- **Browser events**: `mkTrack(name, data)` in `site.js` (a no-op without Umami): `cv-download` (from), `pdf-download`, `email-click`, `outbound`, `project-open` (from, homepage section), `home-section`, `ask-ai-open`, `ask-ai-question` (the text, from the chat frame through `mkChatFrame.track`), `room-object` (`ui.js`), `intro` (watched or skipped and why, `intro.js`), `scene-fallback`
- **Server events**: `website.api/Services/Analytics.cs` posts to Umami's API in the background (Production, or `Umami:Enabled=true` in tests): `contact` (via form, api, assistant or mcp; result; subject), `mcp-tool` and `mcp-resource` (with the client's name), `ask-ai-tool`
- Tests: `tests/website.tests/AnalyticsTests.cs` (Umami faked), `tests/browser/test_analytics.py` and the intro ones (a recording `window.umami`)

## Model Context Protocol (MCP) Integration

AI agents (Claude Code, Cursor, VS Code, the MCP Inspector…) can use the portfolio over MCP. The public page about it is `/ai-integration`.

### Architecture
- **SDK**: the official C# SDK, `ModelContextProtocol.AspNetCore` 2.3.0, registered in `Program.cs` (`AddMcpServer().WithHttpTransport().WithTools<PortfolioMcpTools>().WithResources<PortfolioMcpResources>()`) and mapped with `app.MapMcp("/api/mcp")`
- **Transport**: Streamable HTTP, stateless (the SDK's default from the 2026-07-28 spec): no sessions, any instance can answer any request. No legacy SSE, no sign-in
- **Tools and resources**: `website.api/Mcp/PortfolioMcp.cs`. The work is in `website.api/Services/PortfolioTools.cs`, which `DeepSeekAgentService` also calls, so the site's assistant and outside agents behave the same
- **Tools**: `search_projects` (read-only, idempotent, closed world; structured output with each project's page) and `submit_contact` (not read-only, not destructive, open world; needs name, a valid email, subject and message, and at most `ContactLimiter.PerVisitorPerHour` per visitor an hour, 30 in all)
- **Resources**: `portfolio://profile` (from `HomeContent`, like the assistant's prompt), `contact`, `about`, `ai-integration`, `projects/all`, and the template `portfolio://projects/{id}` named after each page (`hireforge`); an unknown project is `ResourceNotFound`
- **Tests**: `tests/website.tests/McpTests.cs` drives it with the official MCP client (email faked)

### Adding a tool or resource
1. Put the logic in `PortfolioTools` if the assistant should have it too
2. Add a method to `PortfolioMcpTools` with `[McpServerTool(...)]` (name, title and the `ReadOnly`/`Destructive`/`Idempotent`/`OpenWorld` hints) and a `[Description]`, or to `PortfolioMcpResources` with `[McpServerResource(UriTemplate = ...)]`
3. For the assistant, add the tool to DeepSeek's tool list and `RunToolAsync` in `DeepSeekAgentService`
4. Cover it in `McpTests` through the client, and update `/ai-integration`

## Important Note

*This project was completed during a period of long-term illness, which impacted development timeline and some features may remain incomplete. The core functionality and technical implementation remain solid despite these challenging circumstances.*

GitHub Repository: [Portfolio-website](https://github.com/DynoAbd786/Portfolio-website)
