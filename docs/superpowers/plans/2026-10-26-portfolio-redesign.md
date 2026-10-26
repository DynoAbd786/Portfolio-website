# Portfolio Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restyle mkkai.dev around an interactive 3D "workshop" homepage hero, and carry the
new look through every page.

**Architecture:**
- Pages stay static-SSR Razor components in `website/`.
- Homepage facts live in one C# content class (`HomeContent`). It renders the sections and is
  embedded as JSON for the 3D scene.
- The scene is a set of browser ES modules in `website/wwwroot/js/workshop/`, ported from the
  prototype. `site.js` mounts and disposes it across Blazor enhanced navigation.
- The scene loads three.js r160 from the site itself, via the Blazor import map.
- A server-rendered still image with link pins is the fallback and the first paint.
- The 25 project pages move onto one shared `ProjectPage` template.

**Tech Stack:**
- .NET 10 Blazor Web App (static SSR + a few WebAssembly islands)
- Tailwind 3.4 standalone CLI
- three.js 0.160.0
- Tests:
  - xUnit + `Microsoft.AspNetCore.Mvc.Testing` + AngleSharp for server-rendered HTML
  - pytest + Playwright (Python, run with `uv`, Chrome channel) for browser behaviour

**Spec:** `docs/superpowers/specs/2026-10-26-portfolio-redesign-design.md`. Read it before any
task. Reference prototype: `/home/mk/code/personal/experiments/3d-portfolio-prototypes/portal.html`
(the workshop half), with `diorama.html` for the zoom-in without the portal.

## Global Constraints

- **Out of the showcase:** no Formula Student AI content in the workshop scene, proof strip,
  Selected work or "What I'm building". The `/projects/formula-student-ai` page and its Projects
  grid entry stay.
- **Products tone:** Synergy and HireForge are framed as "things I'm building". No pricing,
  sign-up, waitlist or "founder" headline anywhere on the site.
- **URLs:** every existing URL keeps returning 200 with its current path:
  - all routes listed in Task 1
  - `/docs/*.pdf`
  - `/api/projects`, `/health`, `/api/mcp`
- **No 3D elsewhere:** pages other than `/` load no three.js and no `js/workshop/*` module.
- **Tailwind:** class names must appear literally in `.razor`, `.cs` or `site.js` (add
  `./wwwroot/js/workshop/**/*.js` to `tailwind.config.js` content if those modules use utility
  classes). `tailwind.css` stays the last stylesheet in `App.razor`.
- **Prerendering:** interactive components are prerendered, so no `HttpClient` or JS calls in
  `OnInitialized`.
- **Chat:** the chat stays in the `data-permanent` `#chat-root` and opens via `mkChat.toggle()`.
- **Reduced motion:** every animation respects `prefers-reduced-motion: reduce`.
- **Facts:**
  - The CV (`website/wwwroot/docs/Muhammad-Kashif-Khan-CV.pdf`) is the source of facts.
  - Synergy facts come from `/home/mk/code/personal/synergy/SYNERGY_BUSINESS_BRIEF.md` §1.
  - HireForge facts come from `/home/mk/code/personal/hireforge/README.md`.
  - Never invent a metric.
- **Copy:**
  - Status pill: "Open to graduate software & AI roles".
  - Hero degree line: "MEng Computer Science & AI, University of Leeds — First-Class Honours".
  - Mono label: `// software engineer · ai systems`.
- **Visual tokens:** add these to `tailwind.config.js` `theme.extend`:
  - colours: `ink` `#0b1020` (page background), `ink-2` `#111a2e` (raised surfaces),
    `line` `#1f2a44` (borders), `cy` `#22d3ee` (primary accent), `lamp` `#f5a35c` (warm accent)
  - fonts: Inter for sans (Google Fonts, weights 400/600/800), JetBrains Mono for mono (400/600)
- **Shipping:**
  - One branch and PR per numbered PR group below, merged to `main` with
    `gh pr merge --squash` once `dotnet test` and the browser tests pass.
  - The homepage PR (PR C) additionally waits for Muhammad's OK after he has tried it on a
    laptop (Task 10).
- **Commits** end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. PR bodies end
  with `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.
- **Environment:** `export PATH=$HOME/.dotnet:$PATH DOTNET_ROOT=$HOME/.dotnet` before any `dotnet` command.

## Review Focus

1. **Repeated enhanced navigation:** going `/` → `/projects` → `/` twenty times must leave
   exactly one live scene and no "Too many active WebGL contexts" warning.
   Test: `test_workshop_survives_repeated_navigation` (Task 7).
2. **Resize while running:** going from desktop width (1440) to phone width (390) must
   re-stack the hero (room below text) and resize the canvas to its stage, with no stretched
   frame.
   Test: `test_workshop_resizes_to_phone` (Task 7).
3. **Keys typed in the chat:** with a workshop card open, ArrowLeft/ArrowRight/Escape typed
   into the chat input or contact form must not move or close the card.
   Test: `test_workshop_ignores_keys_in_inputs` (Task 7).
4. **Slow GPU:** a device that can't hold 20 fps at the lowest quality tier for 5 s gets the
   still image, not a stuttering scene.
   Test: `test_slow_device_falls_back_to_still` (Task 9).
5. **Anchor links from another page:** an enhanced-nav link to `/#contact` or `/#work` lands
   on that section while the scene loads above it.
   Test: `test_anchor_from_other_page_lands_on_section` (Task 6).

---

## PR A: foundation (branch `redesign/foundation`)

### Task 1: Test harnesses

**Files:**
- Create: `tests/website.tests/website.tests.csproj` (net10.0; xunit, xunit.runner.visualstudio,
  Microsoft.NET.Test.Sdk, Microsoft.AspNetCore.Mvc.Testing 10.*, AngleSharp; ProjectReference
  `../../website.api/website.api.csproj`)
- Create: `tests/website.tests/SiteFactory.cs`, `tests/website.tests/Html.cs`, `tests/website.tests/RouteTests.cs`
- Create: `tests/browser/conftest.py`, `tests/browser/test_smoke.py`, `tests/browser/README.md`
- Modify: `website.sln` (add the test project), `.gitignore` (`tests/browser/.pytest_cache/`, `__pycache__/`)
- Modify (only if the test project can't see `Program`): `website.api/Program.cs`, appending `public partial class Program;`

**Interfaces:**
- Produces: `SiteFactory : WebApplicationFactory<Program>` (environment `Development`).
- Produces: `Html.GetAsync(HttpClient client, string path) -> Task<AngleSharp.Dom.IDocument>`.
  It asserts a 200 status and parses the body.
- Produces: `RouteTests.AllPageRoutes` (`TheoryData<string>`), the 31 `@page` routes:
  - `/`, `/about`, `/ai-integration`, `/ethos`, `/not-found`, `/projects`
  - the 24 `/projects/...` routes
  - `/vascular-blood-flow`
- Produces: pytest fixture `site_url` (session scope). It builds once
  (`dotnet build website.api`), starts `dotnet run --no-build --project website.api` with
  `ASPNETCORE_URLS=http://127.0.0.1:5199`, waits for `/health` to return 200 (timeout 90 s),
  yields `http://127.0.0.1:5199`, and kills the process on teardown.
- Produces: pytest fixture `page_errors(page)`, a list that collects `pageerror` events and
  console messages of type `error`.
- Produces: the Playwright browser launched with `channel="chrome"`.

- [ ] **Step 1: Write the route tests.**
  - `RouteTests.Page_returns_200(string path)` over `AllPageRoutes`.
  - `Unknown_page_returns_404_with_not_found_page`: `/nope` returns 404 and the body contains
    the NotFound page's heading.
  - `Api_and_health_still_respond`: `/api/projects` returns 200 with a JSON array of 25 items,
    and `/health` returns 200 "Healthy".
  - `Cv_pdf_is_served`: `/docs/Muhammad-Kashif-Khan-CV.pdf` returns 200 `application/pdf`.
- [ ] **Step 2: Run** `dotnet test tests/website.tests`. Expected: all PASS (this pins today's behaviour).
- [ ] **Step 3: Write `test_smoke.py::test_home_loads_without_errors`.** Go to `site_url + "/"`,
  wait for `load`, and assert `page_errors == []`.
- [ ] **Step 4: Run** `uv run -q --with pytest --with pytest-playwright pytest tests/browser -q`.
  Expected: PASS. Document this command in `tests/browser/README.md`.
- [ ] **Step 5: Commit.**
  `git add tests website.sln .gitignore website.api/Program.cs && git commit -m "test: add page route and browser smoke tests"`

### Task 2: Visual language, layout, nav and footer

**Files:**
- Modify: `website/tailwind.config.js` (tokens from Global Constraints)
- Modify: `website/Styles/tailwind.css`: `@layer base` sets the body to `bg-ink text-slate-200 font-sans`.
  `@layer components` adds:
  - `.mono-label`: mono, `text-xs`, cyan, tracking-wide
  - `.btn-primary`: cyan fill with a glow
  - `.btn-ghost`: outlined
  - `.chip`
  - `.surface`: `ink-2` background, `line` border, rounded-xl
  - `.grid-bg`: a faint 40 px grid using two linear-gradients at about 4% opacity
  - `.reveal` / `.reveal.is-in`: fade-up, disabled under reduced motion
- Modify: `website.api/Components/App.razor`
  - Add the Inter and JetBrains Mono Google Fonts.
  - Update the meta, OG and Twitter description text: graduate, not student, e.g.
    "Software engineer and First-Class MEng CS & AI graduate (Leeds) building developer tools,
    AI systems and scientific software."
- Modify: `website/Shared/MainLayout.razor`:
  - Header:
    - `mk` monogram tile + "kashif-khan".
    - Links: Work `/projects`, About `/about`, Contact `/#contact`.
    - "Ask AI" button with `onclick="mkChat.toggle()"`.
    - Icon links: GitHub, LinkedIn, CV (`/docs/Muhammad-Kashif-Khan-CV.pdf?v=2026-07`).
    - Phones get the existing mobile menu, restyled.
  - New `<footer>`:
    - contact email `Abdua786@outlook.com`, GitHub, LinkedIn, CV
    - links to `/ethos` and `/ai-integration`
    - © year
  - Keep `#chat-root data-permanent` and its contents unchanged apart from colours.
- Modify: `website/wwwroot/js/site.js`. Add `mkReveal()`: an IntersectionObserver that adds
  `.is-in` to `.reveal` elements. It runs on `DOMContentLoaded` and on Blazor `enhancedload`
  (`Blazor.addEventListener('enhancedload', …)`, guarded for `window.Blazor` existing).
- Create: `tests/website.tests/LayoutTests.cs`

**Interfaces:**
- Produces: CSS classes `mono-label`, `btn-primary`, `btn-ghost`, `chip`, `surface`, `grid-bg`,
  `reveal`, and colour utilities `bg-ink`, `bg-ink-2`, `border-line`, `text-cy`, `text-lamp`.
  Later tasks use only these plus stock Tailwind.

- [ ] **Step 1: Write `LayoutTests`.** Run each test for `/` and `/projects/machine-learning`.
  - `Nav_has_work_about_contact_and_chat`: `header a[href="/projects"]`, `header a[href="/about"]`
    and `header a[href="/#contact"]` exist, and a `header button[onclick="mkChat.toggle()"]` exists.
  - `Footer_links_ethos_and_ai_integration`: `footer a[href="/ethos"]` and
    `footer a[href="/ai-integration"]` exist.
  - `Chat_root_is_permanent`: `#chat-root[data-permanent] #chat-panel` exists.
  - `Meta_description_no_longer_says_student`: `meta[name=description]` content doesn't contain "student" (case-insensitive).
- [ ] **Step 2: Run** `dotnet test tests/website.tests --filter LayoutTests`. Expected: FAIL (old nav).
- [ ] **Step 3: Implement** the files above.
- [ ] **Step 4: Run** `dotnet test tests/website.tests`, then the browser tests. Expected: all PASS.
- [ ] **Step 5: Check the result visually.** Screenshot `/` and `/projects` at 1440×900 and
  390×844 with Playwright and look at them: the nav is legible, the mobile menu opens, and the
  chat opens from "Ask AI".
- [ ] **Step 6: Commit** (`feat: new visual language, nav and footer`).
- [ ] **Step 7: Open PR A and merge** once tests pass.

## PR B: data (branch `redesign/data`)

### Task 3: Project fields, result lines, Synergy title, profile facts

**Files:**
- Modify: `website.api/Models/Project.cs` and `website/Data/Project.cs`. Add to both:
  - `ProjectField[] Fields`
  - `string Result`

  Add an enum `ProjectField { AiMl, VisionGraphics, ScientificBiomedical, CloudDistributed,
  SystemsInfrastructure, ProductBusiness }` to both namespaces. Its order is the filter order.
- Create: `website/Data/ProjectFields.cs` with:
  - `static string Label(ProjectField f)`, returning "AI & ML", "Computer vision & graphics",
    "Scientific & biomedical", "Cloud & distributed", "Systems & infrastructure" or
    "Product & business".
  - `static string Slug(ProjectField f)`, returning `ai-ml`, `vision-graphics`,
    `scientific-biomedical`, `cloud-distributed`, `systems-infrastructure` or `product-business`.
  - `static ProjectField? FromSlug(string? slug)`.
- Modify: `website.api/Services/ApiProjectSource.cs` (map the two new properties).
- Modify: `website.api/Services/ProjectService.cs`:
  - **Fields:** set `Fields` for all 25 projects. A project may have several.
  - **Result:** set `Result` for all 25: one sentence, at most 140 characters, past tense,
    stating what was built or achieved.
    - Projects on the CV take their result from the CV wording.
    - Synergy and HireForge-related wording comes from their repos.
    - Everything else comes from that project's page.
  - **Synergy:** rename "Workflow-Centric Graph Code Analysis Platform" to
    "Synergy — Code Intelligence Platform".
    - Description: the current product, per the brief.
    - Tags: `Python`, `React`, `TypeScript`, `SQLite`, `tree-sitter`, `MCP`.
  - **Formula Student AI:** set `IsFeatured = false`.
- Modify: `website.api/Services/McpService.cs:244` and `:282`, using CV wording:
  - title: "Software Engineer | MEng CS & AI (First-Class), University of Leeds"
  - background: "First-Class MEng Computer Science with AI graduate (University of Leeds, 2026) …"
- Create: `tests/website.tests/ProjectDataTests.cs`

**Interfaces:**
- Produces: `website.Data.Project.Fields : ProjectField[]` and `website.Data.Project.Result : string`.
- Produces: the `ProjectFields.Label`, `ProjectFields.Slug` and `ProjectFields.FromSlug` helpers.
- Produces: `ProjectService.GetProjectByUrlAsync(string url) -> Task<Project?>`, added to
  `website/Data/ProjectService.cs`. It matches `ProjectUrl` exactly.
- Produces: `ProjectService.GetNextProject(IReadOnlyList<Project> all, string url) -> Project`,
  returning the next project in data order and wrapping around.

- [ ] **Step 1: Write `ProjectDataTests`.** Resolve `website.Data.ProjectService` from
  `SiteFactory.Services` inside a scope.
  - `Every_project_has_a_field_and_a_result`: every project has `Fields.Length >= 1` and a
    `Result` between 1 and 140 characters.
  - `Synergy_is_renamed_and_keeps_its_url`: the project at `/projects/workflow-graph-platform`
    has a `Title` starting "Synergy".
  - `Formula_student_is_not_featured`.
  - `Next_project_wraps`: the next project after the last one is the first.
  - `Field_slugs_round_trip`: `FromSlug(Slug(f)) == f` for every value, and `FromSlug("x")` is null.
  - `Mcp_profile_is_not_a_student`: POST JSON-RPC `resources/read` for the profile resource to
    `/api/mcp`. The response doesn't contain "4th Year" or "student".
  - `Api_projects_exposes_fields`: `/api/projects` JSON items have a `fields` array.
- [ ] **Step 2: Run** the tests. Expected: FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run** `dotnet test tests/website.tests`. Expected: PASS.
- [ ] **Step 5: Commit** (`feat: project fields, result lines, current profile facts`).
- [ ] **Step 6: Open PR B and merge.**

## PR C: homepage and workshop (branch `redesign/home`)

### Task 4: Homepage content

**Files:**
- Create: `website/Data/HomeContent.cs`
- Create: `tests/website.tests/HomeContentTests.cs`

**Interfaces:**
- Produces records in `website.Data`:
  - `ProofFact(string Value, string Label)`
  - `SelectedWorkItem(string FieldLabel, string ProjectUrl)`. Title, result and tags come from the project data.
  - `ProductItem(string Name, string Summary, string[] Tags, string? Href)`
  - `ExperienceItem(string When, string Title, string Org, string Detail)`
  - `WorkshopHotspot(string Key, string Name, string Sub, string Kicker, string Title,
    string Desc, string[] Points, string[] Chips, string Href, string Cta, string Action,
    double PinX, double PinY)`
    - `Action` is `"link"` or `"assistant"`.
    - `PinX`/`PinY` are percentages for the still image. Use 0 until Task 9 fills them.
- Produces: `static class HomeContent` with:
  - `bool OpenToWork` (true)
  - `IReadOnlyList<ProofFact> Proof`
  - `IReadOnlyList<SelectedWorkItem> SelectedWork`
  - `IReadOnlyList<ProductItem> Products`
  - `IReadOnlyList<ExperienceItem> Experience`
  - `IReadOnlyList<WorkshopHotspot> Workshop`
  - `string WorkshopJson()` (System.Text.Json, camelCase)

Values:
- **`Proof`**, in this order:
  1. ("First-Class", "MEng CS & AI, Leeds")
  2. ("50% faster", "than the MATLAB original (pNanoLocz)")
  3. ("2", "research internships")
  4. ("cm-accurate", "3D/4D reconstruction")
- **`SelectedWork`**, in this order:
  1. biomedical, `/projects/medical-simulation`
  2. scientific software, `/projects/pnanolocz`
  3. computer vision, `/projects/3d-reconstruction`
  4. cloud, `/projects/distributed-systems`
  5. infrastructure, `/projects/home-lab`
- **`Products`**: Synergy and HireForge, with the spec's summaries and tags.
  - Synergy `Href` = `/projects/workflow-graph-platform`.
  - HireForge `Href` = null.
- **`Experience`**: the spec's four entries, newest first.
- **`Workshop`**: 11 hotspots in this key order:
  1. `synergy`: link to `/projects/workflow-graph-platform`
  2. `hireforge`: link to `#building`
  3. `thesis`: link to `/projects/3d-reconstruction`
  4. `stent`: link to `/projects/medical-simulation`
  5. `afm`: link to `/projects/pnanolocz`
  6. `homelab`: link to `/projects/home-lab`
  7. `azure`: link to `/projects/distributed-systems`
  8. `whiteboard`: link to `#experience`
  9. `certificate`: link to `/about`
  10. `bookcase`: link to `/projects`
  11. `robot`: assistant

  Card copy:
  - Start from the prototype's `HS` cards, rewritten to the facts in Global Constraints.
  - Synergy: no "Enter the codebase"/portal wording.
  - HireForge: the two-agent flow, not gVisor or hidden tests.
  - Robot: keep the "Try one:" asks as `Points`.

- [ ] **Step 1: Write `HomeContentTests`.**
  - `Selected_work_points_at_real_projects`: every `ProjectUrl` exists in the project data.
  - `Workshop_has_the_eleven_hotspots_in_order`: the keys equal the list above.
  - `Workshop_links_resolve`: every hotspot `Href` starting with `/` returns 200 via `SiteFactory`.
  - `Nothing_mentions_formula_student`: `WorkshopJson()` plus every `Proof`, `Products` and
    `Experience` string, lowercased, contains neither "formula" nor "fs-ai".
  - `No_sales_language`: the same strings contain none of "pricing", "sign up", "waitlist" or "founder".
- [ ] **Step 2: Run** the tests. Expected: FAIL.
- [ ] **Step 3: Implement** `HomeContent`.
- [ ] **Step 4: Run** the tests. Expected: PASS.
- [ ] **Step 5: Commit** (`feat: homepage content model`).

### Task 5: About page takes the long About Me text

**Files:**
- Modify: `website/Pages/About.razor`
  - Merge in the homepage's "About Me" section, from `website/Pages/Index.razor` lines 24–230,
    keeping its text.
  - Restyle the page with the new classes.
  - Add links to `/ethos` and `/ai-integration`.
  - Change any wording that calls Muhammad a current student.
- Test: `tests/website.tests/PageContentTests.cs`

- [ ] **Step 1: Write `About_contains_the_about_me_text`.**
  - `/about` contains the heading "Driving Innovation Through Code" and "Looking Forward".
  - `/about` links to `/ethos` and `/ai-integration`.
  - `/about` contains no "4th Year" or "4th year".
- [ ] **Step 2: Run** the test. Expected: FAIL (links missing).
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run** the test. Expected: PASS.
- [ ] **Step 5: Commit.**

### Task 6: Homepage markup

**Files:**
- Modify: `website/Pages/Index.razor`. Rewrite it to render the sections from
  `HomeContent` plus `ProjectService`. Remove the old hero, About and featured-project grid.
  - **Hero:**
    - Left: mono label, `<h1>` name, degree line, `a.btn-primary[href="#work"]` "See my work",
      `a.btn-ghost[href="/docs/Muhammad-Kashif-Khan-CV.pdf?v=2026-07"]` "Download CV",
      and the status pill if `OpenToWork`.
    - Right: `div#workshop` (described below).
    - Room below text under 900 px wide.
  - `section#proof`
  - `section#work`: 5 cards, each with mono field label, title, `Result`, chips and a link to
    `ProjectUrl`. Then `a[href="/projects"]` "All projects →".
  - `section#building`: intro line "Things I'm building on my own time", 2 cards, an
    "in development" chip, and HireForge with no link.
  - `section#experience`: a timeline.
  - `section#contact`: `<ContactForm />` plus email, GitHub, LinkedIn and CV links.
- `#workshop` markup (server-rendered):

```html
<div id="workshop" class="workshop" data-state="still">
  <div class="workshop-still">
    <img src="/images/workshop/still.webp" srcset="… 1x, …@2x 2x" width="1600" height="1000" alt="An illustrated workshop room: desk with two monitors, a 3D scanner, a server rack, a bookcase and a small robot">
    <!-- one per hotspot: --> <a class="workshop-pin" href="{Href}" style="left:{PinX}%;top:{PinY}%">{Name}</a>
  </div>
  <canvas class="workshop-canvas" aria-hidden="true"></canvas>
  <script type="application/json" id="workshop-data">@((MarkupString)HomeContent.WorkshopJson())</script>
</div>
```

  - The robot's pin is a `<button onclick="mkChat.toggle()">`.
  - Until Task 9 adds the image, `still.webp` may 404; the browser tests for that land in Task 9.
- Modify: `website/wwwroot/css/app.css`: `.workshop` sizing (stage aspect matches the
  prototype's hero) and `.workshop[data-state=live] .workshop-still { opacity: 0 }`. Delete the
  old `.hero-*` rules once nothing references them.
- Create: `tests/website.tests/HomePageTests.cs`
- Create: `tests/browser/test_home.py`

- [ ] **Step 1: Write `HomePageTests`.**
  - `Hero_has_name_ctas_and_status`: an `h1` containing "Muhammad Kashif-Khan", `a[href="#work"]`,
    an `a[href^="/docs/Muhammad-Kashif-Khan-CV.pdf"]`, and the text "Open to graduate software & AI roles".
  - `Sections_in_order`: the ids `proof`, `work`, `building`, `experience`, `contact` appear in that order.
  - `Selected_work_has_five_cards_linking_to_projects`.
  - `Workshop_data_is_valid_json_with_eleven_hotspots`: parse `#workshop-data`.
  - `Every_hotspot_has_a_pin`: the `.workshop-pin` count is 11.
  - `Homepage_never_mentions_formula_student`: the body text contains no "Formula".
  - `Hireforge_card_has_no_link`.
- [ ] **Step 2: Write `test_home.py`.**
  - `test_anchor_from_other_page_lands_on_section`: from `/projects`, click the nav "Contact"
    link (`/#contact`). `#contact` is within the viewport after 1.5 s.
  - `test_see_my_work_scrolls_to_work`.
- [ ] **Step 3: Run** both. Expected: FAIL.
- [ ] **Step 4: Implement.**
- [ ] **Step 5: Run** `dotnet test tests/website.tests` and the browser tests. Expected: PASS.
- [ ] **Step 6: Commit** (`feat: new homepage sections`).

### Task 7: Port the workshop scene

**Files:**
- Create: `website/wwwroot/lib/three/0.160.0/three.module.min.js`, plus the addons under
  `website/wwwroot/lib/three/0.160.0/addons/`. Copy them from the npm tarball
  `https://registry.npmjs.org/three/-/three-0.160.0.tgz` (`build/three.module.min.js`,
  `examples/jsm/**` for the files the scene imports and their own imports only), with
  `LICENSE`.
- Modify: `website.api/Components/App.razor`. Pass `<ImportMap ImportMapDefinition="…">` the
  result of `ImportMapDefinition.Combine(<the endpoint's default>, threeMap)`.
  - `threeMap` maps `three` → `Assets["lib/three/0.160.0/three.module.min.js"]` and
    `three/addons/` → `/lib/three/0.160.0/addons/`.
  - Get the default from `HttpContext.GetEndpoint()?.Metadata.GetMetadata<ImportMapDefinition>()`
    via a `[CascadingParameter] HttpContext`.
- Create, all ES modules under `website/wwwroot/js/workshop/`, split by responsibility from
  `portal.html` lines 374–1898 plus 2784–2860, dropping all city/portal code (`createCity`,
  `makeScreenMap`, `portalGeo`, `XF`, `composite`, `enterPortal`/`exitPortal`/`arrive`/
  `finishBack`, `P`, `cityKey`):
  - `workshop.js`: entry; `mount` and `dispose`; loop; lifecycle
  - `kit.js`: renderer factory, maths helpers, materials, `shade`, palette
  - `room.js`: shell, desk, lamp, plant, rug, bookcase, rack, rig, whiteboard
  - `screens.js`: the Synergy graph and HireForge terminal canvases
  - `hotspots.js`: binds `WorkshopHotspot` data to scene groups by key; picking
  - `ui.js`: tag, card, tour buttons, pointer and keyboard
  - `quality.js`: adaptive tiers, refresh measurement, software-renderer detection
  - `post.js`: composer, bloom, outline
- The rug keeps its texture but must not read as a "portal" hint. Remove the code comments
  that call it one.
- Clicking Synergy behaves like every other object: zoom to the monitor (as `diorama.html`
  does) and open its card.
- Card CTA:
  - for `Action == "link"`, `location.assign(href)`, or let a normal `<a>` navigate so
    Blazor's enhanced navigation handles it
  - for `"assistant"`, call `mkChat.toggle()`
- The hint bar text: `move` aim the lamp · `hover` inspect · `click` zoom in. "explored n/11".
- Modify: `website/wwwroot/js/site.js`. Add `window.mkWorkshop`.
- Create: `tests/browser/test_workshop.py`

**Interfaces:**
- Produces: `export async function mount(root: HTMLElement, data: WorkshopHotspot[]): Promise<{ dispose(): void }>`
  in `workshop.js`.
  - It throws `Error('webgl-unavailable')` if a WebGL2 context can't be created.
  - It sets `root.dataset.state` to `"loading"`, then to `"live"` after the first frame.
  - `dispose()` stops the loop, removes all listeners and observers, disposes every geometry,
    material, texture and render target, and calls `renderer.forceContextLoss()`.
- Produces: `window.mkWorkshop = { active: boolean, mounts: number, disposes: number, sync(): Promise<void> }`
  in `site.js`.
  - `sync()` runs on `DOMContentLoaded` and on Blazor `enhancedload`.
  - It disposes the current instance if its root is no longer connected.
  - If `#workshop` exists and nothing is mounted, it runs
    `await import('/js/workshop/workshop.js')` and mounts with the parsed `#workshop-data`.
  - On error it sets `data-state="failed"`, so the still stays visible.
- Produces: a debug hook, present only when `location.search` includes `debug=workshop`:
  `root.__workshop = { pins(): {key, x, y}[], forceFps(n: number|null): void, tier(): number }`.
  - `pins()` gives each hotspot's anchor as a percentage of the stage.
  - Task 8 and Task 9 tests use it.

- [ ] **Step 1: Write `test_workshop.py`.** Use `?debug=workshop` where pins are needed.
  - `test_scene_goes_live_without_errors`: `#workshop[data-state=live]` within 20 s, and `page_errors == []`.
  - `test_hover_shows_tag_and_click_opens_card`: hover `pins()` for `thesis`, then the tag text
    contains "MEng thesis". Click, then the card title is visible. Escape closes it.
  - `test_robot_opens_chat`: click the robot pin, then the card CTA. `#chat-panel` loses `translate-x-full`.
  - `test_workshop_survives_repeated_navigation`:
    - 20× click nav "Work", wait for `/projects`, then click the monogram home and wait for
      `data-state=live`.
    - Assert `mkWorkshop.mounts - mkWorkshop.disposes == 1`.
    - Assert no console message contains "Too many active WebGL contexts".
  - `test_workshop_resizes_to_phone`: going from 1440×900 to 390×844, the canvas bounding box
    equals `.workshop`'s within 2 px, and the canvas sits below the `h1`.
  - `test_workshop_ignores_keys_in_inputs`: open the card for `thesis`, focus `#contact`
    textarea and press ArrowRight and Escape. The card title is still "Hybrid 3D/4D reconstruction".
  - `test_reduced_motion_opens_card_without_flight`: use `reduced_motion="reduce"`. A click
    opens the card within 300 ms.
  - `test_other_pages_do_not_load_three`: record requests on `/projects` and
    `/projects/home-lab`. None match `/lib/three/` or `/js/workshop/`.
  - `test_tour_buttons_reach_every_hotspot`: there are 11 keyboard tour buttons, and Tab
    reaches them.
- [ ] **Step 2: Run** the tests. Expected: FAIL.
- [ ] **Step 3: Implement** the port.
- [ ] **Step 4: Run** the tests. Expected: PASS. Also run `dotnet test`.
- [ ] **Step 5: Commit** (`feat: workshop 3D scene on the homepage`).

### Task 8: New objects and real screen content

**Files:**
- Modify: `website/wwwroot/js/workshop/room.js` (or a new `props.js` it imports), `screens.js`, `hotspots.js`
- Test: `tests/browser/test_workshop.py`

New hotspot groups. Each is a single merged group of up to 6 draw calls, built with the
prototype's `kit.js` helpers and materials so they match the room's look:
- `stent`: a curved translucent vessel segment with a mesh-lattice stent inside, on a small
  stand on the desk's left end.
- `afm`: a compact microscope (base, arm, stage, probe head) on the bookcase's lowest shelf or
  the floor beside it.
- `azure`: a small glowing cloud-shaped hologram floating above the server rack. It pulses
  slowly, and is static under reduced motion.
- `certificate`: the framed certificate already on the left wall becomes its own hotspot.
- `bookcase`: the existing bookcase, now linking to `/projects`.

Also:
- Split the prototype's `whiteboard` and `homelab` cards so that stent, AFM and Azure get their own.
- `screens.js` Synergy graph: label nodes with real Synergy module names
  (`synergy_models`, `synergy_persistence`, `synergy_analyzer`, `synergy_visualizer`,
  `synergy_mcp`, `synergy_backend`, `synergy_frontend`) and edges in the brief's dependency
  direction.
- `screens.js` HireForge terminal: loop through `generate codebase (seeded defect)` →
  `candidate session` → `independent review` → `scorecard`, with plausible but clearly
  illustrative lines. No numbers presented as real results.

- [ ] **Step 1: Write `test_every_hotspot_is_pickable`.** For each of the 11 keys, hovering its
  `pins()` position shows a tag with that hotspot's `Name`.
- [ ] **Step 2: Run** it. Expected: FAIL for `stent`, `afm`, `azure`, `certificate`.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run** the browser tests. Expected: PASS.
- [ ] **Step 5: Review screenshots.** Take them at 1440×900 and 390×844 and look at them:
  nothing overlaps or floats, and the new objects read as what they are.
- [ ] **Step 6: Commit** (`feat: workshop objects for every field`).

### Task 9: Still image, fallbacks and slow-device bail-out

**Files:**
- Create: `tools/render-workshop-still.py`, run as
  `uv run --with playwright python tools/render-workshop-still.py http://127.0.0.1:5199`.
  - It opens `/?debug=workshop&still=1` at 1600×1000 and at 3200×2000.
  - `still=1` hides the hero text, stops idle animation and holds the home camera pose.
  - It waits for `live`, then saves `website/wwwroot/images/workshop/still.webp` and
    `still@2x.webp` (quality 80) via a canvas `toDataURL('image/webp')`.
  - It prints `pins()` as C# initialisers to paste into `HomeContent.Workshop` `PinX`/`PinY`.
- Modify: `workshop.js`:
  - `still=1` handling.
  - Bail-out: if `quality.js` is at its lowest tier and the median fps over the last 5 s is
    below 20, call `dispose()` and set `data-state="still"`.
  - A failed WebGL context does the same.
- Modify: `website/Data/HomeContent.cs` (the pin values)
- Modify: `app.css`: pins are visible as small labelled dots only while `data-state` is
  `still` or `failed`, and are focusable.
- Test: `tests/browser/test_fallback.py`

- [ ] **Step 1: Write `test_fallback.py`.**
  - `test_no_webgl_shows_still_with_links`: launch with args `--disable-gpu --disable-webgl`.
    `img` inside `.workshop-still` is visible, 11 pins are visible, the `stent` pin's href is
    `/projects/medical-simulation`, and there are no page errors.
  - `test_no_javascript_shows_still`: use a context with `java_script_enabled=False`. The still
    and pins are visible, and the `h1` is visible.
  - `test_slow_device_falls_back_to_still`: `?debug=workshop`, then `forceFps(10)`. Within 12 s,
    `data-state=still`, and `mkWorkshop.active` is false.
  - `test_still_image_loads`: `/images/workshop/still.webp` returns 200 and `naturalWidth` is 1600.
- [ ] **Step 2: Run** the tests. Expected: FAIL.
- [ ] **Step 3: Implement.** Run the still script and paste the pins in.
- [ ] **Step 4: Run** all tests. Expected: PASS.
- [ ] **Step 5: Commit** (`feat: still image fallback and slow-device bail-out`), including the generated images.

### Task 10: Muhammad's laptop check, then merge PR C

- [ ] **Step 1:** Run the branch:
  `ASPNETCORE_URLS=http://0.0.0.0:5199 dotnet run --project website.api` in the background.
- [ ] **Step 2:** Find a reachable address. Try `tailscale ip -4` first, then the LAN IP from
  `hostname -I`. Check `curl -s http://<ip>:5199/health` prints `Healthy`.
- [ ] **Step 3:** Open PR C. Send Muhammad the link `http://<ip>:5199/` and ask him to try it on
  an ordinary laptop: whether it's smooth, and whether anything looks wrong.
- [ ] **Step 4:** Fix what he reports, re-running the tests. Merge only after his OK, then stop
  the server.

## PR D: project pages (branch `redesign/project-pages`)

### Task 11: Template components, baseline, first two pages

**Files:**
- Create: `tests/website.tests/ProjectPageBaseline.json` and
  `tests/website.tests/ProjectPageBaselineTests.cs`.
  - Generate the baseline **before** changing any page, using a one-off test marked
    `[Fact(Skip = "generator")]` that you un-skip once.
  - For each of the 25 project routes, record:
    - the `h2`/`h3` heading texts
    - every `a[href$=".pdf"]` href
    - every `img` src
- Create: `website/Shared/ProjectPage.razor`
- Create: `website/Shared/ProjectSection.razor`
- Create: `website/Shared/ProjectSection.razor.css` only if utilities aren't enough.
- Create: `website/Shared/BridgeStatus.razor` (`@rendermode InteractiveWebAssembly`). This is
  the bridge-status UI and polling moved out of `HomeLabProject.razor`, unchanged.
- Create: `website/Shared/ImageGallery.razor` (`@rendermode InteractiveWebAssembly`), with
  `[Parameter] List<GalleryItem> Items`. This is the gallery moved out of
  `MedicalSimulationProject.razor`; `GalleryItem` moves to `website/Data/GalleryItem.cs`.
- Modify: `website/Pages/MachineLearningProject.razor` and
  `website/Pages/MedicalSimulationProject.razor`. Move both onto the template. Remove the
  page-level `@rendermode` from MedicalSimulation.
- Modify: `website/Pages/HomeLabProject.razor`. Remove the page-level `@rendermode` and use
  `<BridgeStatus />`.

**Interfaces:**
- Produces: `ProjectPage` parameters:
  - `[Parameter, EditorRequired] string Url`
  - `[Parameter] string? Kicker`, e.g. "Academic project · Year 3", shown as a mono label
  - `[Parameter] RenderFragment? Links`, header buttons such as GitHub or documents
  - `[Parameter] RenderFragment ChildContent`

  It renders:
  - breadcrumb (Home / Work / Title)
  - field labels via `ProjectFields.Label`
  - `h1` Title, the `Result` line as lead, chips from `Tags`
  - an "in development" chip when `IsInDevelopment`
  - `Links`
  - the body
  - a footer with `a.next-project` to `GetNextProject` ("Next project →") and `a[href="/projects"]` "Back to work"

  It also sets `<PageTitle>` to Title.
- Produces: `ProjectSection` parameters: `string Title`, `string? Subtitle`, `string? Icon`
  (a Font Awesome class), `RenderFragment ChildContent`. It renders a `.surface` with
  heading row and body.
- Migration rule for every page:
  - Replace the breadcrumb, header, hero-visual, and back button with `<ProjectPage>`.
  - Replace each `bg-gray-900 rounded-lg p-8 border …` section wrapper with `<ProjectSection>`,
    keeping its heading text.
  - Inside sections, swap rainbow pill colours (`bg-blue-600`, `bg-purple-600` …) for `.chip`
    and gradients for `surface`.
  - Keep all text, lists, tables, equations, code, images and PDF links.

- [ ] **Step 1: Generate and commit the baseline** (on the untouched pages). Then add:
  - `ProjectPageBaselineTests.Content_is_preserved(route)`: every baseline heading, PDF href
    and img src is still present.
  - `ProjectPageTemplateTests.Uses_template(route)`: `a.next-project` exists and the `h1` equals
    the project `Title`.

  The template test's `TheoryData` lists only the migrated routes; each later task adds its own.
- [ ] **Step 2: Run** the tests. Expected: the baseline PASSES, and the template test FAILS for the two pages.
- [ ] **Step 3: Implement** the components and migrate the two pages plus the HomeLab extraction.
- [ ] **Step 4: Run** `dotnet test`. Then run the browser tests, adding:
  - `test_project_pages.py::test_medical_gallery_next_works`: click next, then the image src changes.
  - `test_homelab_status_renders`: the bridge status element appears within 10 s.

  Expected: PASS.
- [ ] **Step 5: Commit** (`feat: shared project page template`).

### Task 12: Migrate batch 1

Move these pages onto the template: `ThreeDReconstructionProject`, `PNanoLoczProject`,
`DistributedSystemsProject`, `HomeLabProject` (header and sections), `WorkflowGraphPlatformProject`
and `FormulaStudentAIProject`.

`WorkflowGraphPlatformProject` also gets a "Current status" `ProjectSection` first. It contains:
- what works today, what it's built with, and that it's in development and local-first
- next steps: agent setup, packaging

Keep it engineering-only. Don't explain the stack change; that's Muhammad's decision. Relabel
the existing write-up's first heading area "How it started" (add an `h2` "How it started"
before the old content; the baseline headings stay).

- [ ] **Step 1:** Add these routes to the template test `TheoryData`, plus a test
  `Synergy_page_leads_with_current_status`:
  - the first `ProjectSection` heading is "Current status"
  - the page contains "How it started"
  - it doesn't contain "pricing" or "C# to Python"
- [ ] **Step 2: Run** the tests. Expected: FAIL.
- [ ] **Step 3: Migrate.**
- [ ] **Step 4: Run** `dotnet test`. Expected: PASS, including the baseline.
- [ ] **Step 5: Commit.**

### Task 13: Migrate batch 2

Pages: `DataMiningProject`, `NumericalComputationProject`,
`MentalHealthAppProject`, `TerraTrackerProject`, `BusinessPlanProject`, `VascularBloodFlowProject`,
`AIDecisionTreesProject` and `AISearchProject`.

Find each route with `grep -l '@page "<route>"' website/Pages/*.razor`. Follow the same steps as
Task 12: add routes to the template test, see it fail, migrate, run `dotnet test` (PASS), commit.

### Task 14: Migrate batch 3 and open PR D

Pages: `ParallelComputingProject`, `AutonomousRoboticsProject`, `OpenGLRendererProject`,
`OSXv6Project`, `QuantumDotsProject`, `CProgrammingProject`, `GraphicsFundamentalsProject`,
`TaskFlowProject` and `AIResearchProject` (it serves `/projects/portfolio`).

Follow the same steps. Then:
- The template test's `TheoryData` must equal all 25 project routes. Add the test
  `All_project_routes_use_template`, which compares against the project data's `ProjectUrl`s.
- Take screenshots of 3 random project pages at both widths and look at them.
- Open PR D and merge.

## PR E: remaining pages and cleanup (branch `redesign/pages`)

### Task 15: Projects page with filters

**Files:**
- Modify: `website/Pages/Projects.razor`
  - `[SupplyParameterFromQuery(Name = "field")] string? Field` and
    `[SupplyParameterFromQuery(Name = "category")] string? Category`.
  - Filter chips are plain `<a>` links that build the query string, with the current one
    marked `aria-current="true"`. Show "All" and the 6 fields in enum order, and "All" plus the
    3 categories.
  - Show one grid of `ProjectCard`s.
  - Show an empty state when nothing matches.
- Modify: `website/Shared/ProjectCard.razor`. Restyle it: field label, title, `Result`, chips,
  a featured/in-development marker, and lift on hover. Remove the `vanilla-tilt` use if any.
- Test: `tests/website.tests/ProjectsPageTests.cs`

- [ ] **Step 1: Write the tests.**
  - `All_projects_listed_by_default`: there are 25 cards.
  - `Field_filter`: `/projects?field=cloud-distributed` shows only projects with
    `CloudDistributed`, and the count matches the data.
  - `Category_filter`: `/projects?category=professional` shows 2 cards.
  - `Combined_filters`.
  - `Unknown_filter_is_ignored`: `/projects?field=zzz` shows 25 cards.
  - `Formula_student_is_in_the_grid`.
- [ ] **Step 2: Run** the tests. Expected: FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run** the tests. Expected: PASS.
- [ ] **Step 5: Commit.**

### Task 16: Ethos, AI Integration, Not Found; cleanup; docs

**Files:**
- Modify: `website/Pages/Ethos.razor`, `website/Pages/AIIntegration.razor` and
  `website/Pages/NotFound.razor` (restyle only; keep the text). NotFound gets links to `/` and `/projects`.
- Delete: `website/wwwroot/js/vanilla-tilt.min.js`, if `grep -r vanilla` finds no remaining use.
- Delete: unused rules in `website/wwwroot/css/app.css`. Check each selector with `grep -r`
  across `website/` before removing it.
- Modify: `CLAUDE.md`. Document:
  - `HomeContent`
  - the workshop modules and `mkWorkshop`
  - the still-image script
  - the `ProjectPage`/`ProjectSection` template and migration rule
  - project fields
  - the two test suites and their commands

  Also remove "Project Categories"/"Typography" guidance that no longer applies.
- Test: extend `PageContentTests`.

- [ ] **Step 1: Write the tests.**
  - `Not_found_links_home_and_work`.
  - `Ethos_and_ai_integration_render_in_new_layout`: each has an `h1` and a `.surface`.
- [ ] **Step 2: Run** them. Expected: FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run** the full suites: `dotnet test` and the browser tests. Expected: PASS.
- [ ] **Step 5: Commit, open PR E, and merge.**
- [ ] **Step 6: Check production.** After Render deploys, run
  `curl -s -o /dev/null -w '%{http_code}'` on `/`, `/projects`, `/projects/medical-simulation`,
  `/health` and the CV. Expected: 200 each.
