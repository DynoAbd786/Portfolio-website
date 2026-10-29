# mkkai.dev

My portfolio, live at **[mkkai.dev](https://mkkai.dev)**. It's where I show my projects, from internships and my master's degree to the things I'm building now, and it's a project in its own right: a .NET 10 site with a 3D homepage, an AI assistant and an MCP server that AI agents can connect to.

## What's in it

- **A 3D homepage.** A small room built with three.js, with an object for each project: click one and the camera moves in and opens a card about it. While the room loads, my name builds itself out of a triangle mesh and flies into place. Devices without WebGL, or too slow for it, get a still image with the same links.
- **31 project pages** on one shared template, filterable by field and by category (personal, professional, academic).
- **An assistant** ("Ask AI" in the header) that answers questions about my work. It runs on DeepSeek Flash and is given the same profile and project data the pages are built from.
- **An MCP server** at `https://mkkai.dev/api/mcp`, so an agent such as Claude Code, Cursor or a Claude connector can search my projects, read my profile or send me a message. How to connect: [mkkai.dev/ai-integration](https://mkkai.dev/ai-integration).
- **A contact form** that emails me through Resend.

## Built with

.NET 10 · ASP.NET Core · Blazor · Tailwind CSS · three.js · the official MCP C# SDK · DeepSeek · Resend · Docker · Render

## How it's put together

- **`website.api/`** is the host: it renders every page on the server (Blazor static SSR), serves the API and the MCP server, and sends email.
- **`website/`** holds the pages, components and static assets. Only two components run in the browser with WebAssembly (the image gallery and the chat), so most pages load as plain HTML with no runtime.
- **Project data** lives in one place (`website.api/Services/ProjectService.cs`) and feeds the pages, the public API (`/api/projects`), the assistant and the MCP server. Everything else the homepage says is in `website/Data/HomeContent.cs`.
- **The assistant and the MCP server share their tools** (`website.api/Services/PortfolioTools.cs`), so they always give the same answers.
- **The 3D scene** is in `website/wwwroot/js/workshop/` and the name-build intro in `website/wwwroot/js/intro/`.

`CLAUDE.md` has the detail: render modes, the middleware order, the homepage, the MCP server and how to add a project.

## Running it

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
cd website.api
dotnet run          # http://localhost:5154
```

Tailwind is compiled during the build by its standalone CLI, downloaded once into `.tools/` (no Node needed). The contact form and the assistant need keys in `website.api/.env`; without them the site still runs, and both say they're unavailable:

```
Resend__ApiKey=re_...
DeepSeek__ApiKey=...
```

With Docker, the same container Render runs:

```bash
docker build -t portfolio-app .
docker run -p 8080:10000 portfolio-app    # http://localhost:8080
```

## Tests

```bash
# Server-side: every page, the API, the MCP server (through the official MCP client), project data and templates
dotnet test tests/website.tests

# In Chrome: the 3D scene and its fallbacks, the intro, phone layouts, text contrast, page transitions, the chat panel
uv run -q --with pytest --with pytest-playwright pytest tests/browser -q
```

About 360 server-side tests and 150 browser tests. See `tests/browser/README.md` for the browser suite.

## Deployment

Render builds the `Dockerfile` and deploys every push to `main`; its health check polls `/health`.

## How I built it

I use AI coding agents (Claude Code) heavily, and I treat what they write like a colleague's pull request: I read it, test it, and don't ship what I can't explain. The test suites above are a large part of how.

## Contact

**Muhammad Kashif-Khan**: [mkkai.dev](https://mkkai.dev/#contact) · [LinkedIn](https://linkedin.com/in/dynoabd786) · [GitHub](https://github.com/DynoAbd786)
