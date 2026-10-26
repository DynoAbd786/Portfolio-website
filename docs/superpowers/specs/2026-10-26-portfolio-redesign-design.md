# Portfolio redesign: the workshop

Date: 2026-10-26
Status: draft for approval (builder check done)

## Why

mkkai.dev is aimed at recruiters and hiring managers. The current site reads as a student
portfolio: a plain hero ("4th Year MEng/BSc CS & AI Student", now out of date; Muhammad
graduated with a First in July 2026), a long About Me block, then a project list. It needs to
catch a recruiter's eye in the first few seconds and leave them with one clear idea.

Second purpose: give Synergy and HireForge, two products Muhammad is building and may turn
into businesses, a place on the site. They must read as side projects he is proud of, not a
startup pitch, so that a hiring manager isn't worried he's about to leave.

## The one message

> Give him a problem he's never seen, and he'll ship working software for it.

The evidence is the range in his CV: GPU-accelerated stent deployment simulation, a microscope
analysis platform 50% faster than its MATLAB predecessor, a 3D/4D reconstruction pipeline with
centimetre-accurate error, a serverless Azure inventory system and a home lab running local
LLMs and coding agents, across biomedicine, scientific software, computer vision, cloud and
infrastructure. Every part of the homepage should support this message.

The source of truth for facts is the CV: `website/wwwroot/docs/Muhammad-Kashif-Khan-CV.pdf`.

## Goals

- A homepage hero that makes a recruiter stop: an interactive 3D workshop that reacts to the mouse.
- A recruiter can get the CV or make contact without touching the 3D at all.
- The whole site shares the new look: homepage, Projects, all project pages, About, Ethos,
  AI Integration, Not Found.
- Synergy and HireForge are visible and described, with a calm tone.

## Non-goals

- No change to the backend, API, MCP server, chat assistant behaviour, contact email flow or hosting.
  The exception is data-only edits (my call):
  - Adding the new field data to `ProjectService`, which is additive in `/api/projects`.
  - The Synergy title change.
  - Correcting out-of-date profile facts, such as the "4th Year CS & AI Student" text in
    `website.api/Services/McpService.cs`.

  These flow into `/api/projects`, MCP output and the chat assistant's prompt
  (`GeminiAgentService` builds it from `ProjectService`). That's intended: everything should
  tell the same story.
- No new project content beyond the short lines this design needs (card one-liners, proof facts,
  HireForge description). Existing write-ups on project pages are kept as they are.
- No 3D on any page except the homepage.
- No CMS or content editing tools.

## Anti-goals (failure even if it works)

- **Spooking hirers:** no pricing, sign-up, waitlist, "founder" headline, or anything that says
  "I'm starting a company". On the CV Synergy is "Technical Founder & Software Architect". The
  site should frame it as "something I'm building".
- **Formula Student AI in the showcase:** it must not appear in the workshop scene, the proof
  strip, Selected work or "What I'm building". Muhammad has barely worked on it. Its project
  page and its entry in the Projects grid stay, restyled like the rest.
- **A blank or broken first screen:** a recruiter on an old laptop, a phone, a locked-down
  corporate browser or with WebGL off must still see a good page with working links.
- **Slower pages:** pages other than the homepage must not get slower than they are today.
- **Broken links:** all existing URLs keep working: every `/projects/...` route,
  `/vascular-blood-flow`, `/docs/*.pdf` (the CV link is shared), `/api/*`, `/health`, MCP endpoints.
- **Hiding the illness note:** not a goal either way. The CV's note on Year 3 results doesn't
  need to appear on the site.

## Constraints

- .NET 10 Blazor Web App with static SSR (see `CLAUDE.md`). Pages render on the server as plain
  HTML; only `ContactForm`, `MedicalSimulationProject`, `HomeLabProject` and `AiTerminal` are
  interactive (WebAssembly). The redesign stays in this stack.
- Tailwind v3 via the standalone CLI. Class names must appear literally in `.razor`, `.cs` or
  `site.js` files. `tailwind.css` stays the last stylesheet.
- Blazor enhanced navigation swaps page content without a full reload. The 3D scene must start
  when the homepage is entered by enhanced navigation and must stop and free its GPU resources
  when the user navigates away. Listening only to `DOMContentLoaded` is not enough.
- The chat panel lives in a `data-permanent` container in `MainLayout` and is opened with
  `mkChat.toggle()`. Keep both.
- Hosted on Render's free plan (single small instance, cold starts). All 3D work happens in the
  browser; nothing new on the server.
- Changes go in through PRs; Muhammad has allowed merging to `main` once checks pass.

## Visual language (all pages)

Taken from the workshop prototype screen (`shots/portal.png` in the prototype folder):

- Background: very dark navy, with a faint perspective grid behind the homepage hero and a
  subtler, flat version (or none) elsewhere.
- Type: big, heavy, tight sans-serif headings (the prototype uses Inter); small monospace
  labels in cyan, written like code comments (`// software engineer · ai systems`).
- Accent: cyan for primary buttons and links; the warm orange of the desk lamp as a secondary
  accent, used sparingly.
- Navigation (my call):
  - Left: the `mk` monogram and "kashif-khan".
  - Right: Work, About, Contact and the "Ask AI" chat button (the only way into the chat on
    pages other than the homepage), plus icon links for GitHub, LinkedIn and CV. On phones,
    these sit behind a menu button.
  - Work goes to `/projects`, About to `/about`, and Contact to `/#contact`.
- Footer on every page (my call): contact links, CV, and links to Ethos and AI Integration
  (which are otherwise unlinked). About also links to both.
- Motion outside the homepage is light: sections fade up as they scroll in, cards lift on hover.
  Everything respects `prefers-reduced-motion`.
- The existing Font Awesome, KaTeX and highlight.js stay (project pages use them).

## Homepage, top to bottom

### 1. The workshop (hero)

Left side, server-rendered HTML (visible before any 3D loads):
- `// software engineer · ai systems`
- Name, very large.
- One line: what he builds plus "MEng Computer Science & AI, University of Leeds — First-Class Honours".
- Buttons: "See my work" (scrolls to Selected work) and "Download CV".
- Status pill: "Open to graduate software & AI roles". Only while that's true, so it should be easy to switch off.

Right side: the 3D workshop, a cutaway isometric room at night.

**Reference implementation:** the workshop half of
`/home/mk/code/personal/experiments/3d-portfolio-prototypes/portal.html` (three.js r160).
Use it as the starting point:
- Keep its room, lighting, post-processing, adaptive quality and interaction.
- Remove everything to do with the dive into Codebase City (the city scene, the transition,
  "← back to the desk").
- Clicking the Synergy monitor zooms in on its live graph the same way clicking any other
  object zooms in. For how the zoom-in looks without the city, see `diorama.html` in the same
  folder; ignore its race car and track.

Interaction:
- **Move:** the desk lamp follows the cursor, sweeping warm light across the room.
- **Hover:** an object lifts and glows, and a small label names the project.
- **Click:** the camera zooms to the object and a project card opens (title, one-line result,
  tech tags, "Open project →" link to its page). Esc or a close button returns.
- A small hint bar: `move` aim the lamp · `hover` inspect · `click` zoom in, and an
  "explored n/N" counter.
- On touch: tap = hover, second tap = open.

Objects and where they lead (the prototype already has most of these):

| Object | Project | Link |
|--------|---------|------|
| Left monitor (live graph) | Synergy | `/projects/workflow-graph-platform` |
| Right monitor (terminal session) | HireForge | the "What I'm building" section on the homepage (`#building`) |
| Scanner rig with turntable | MEng thesis, 3D/4D reconstruction | `/projects/3d-reconstruction` |
| Stent / vessel model | Flow diverter stent simulation (internship 2025) | `/projects/medical-simulation` |
| Microscope / AFM item | pNanoLocz (internship 2024) | `/projects/pnanolocz` |
| Server rack | Home lab & local LLMs | `/projects/home-lab` |
| Cloud / Azure item | Azure serverless inventory | `/projects/distributed-systems` |
| Whiteboard | Two internships | `#experience` |
| Framed certificate | Education, First-Class MEng | `/about` |
| Bookcase | All other projects | `/projects` |
| Robot | AI assistant | opens the chat panel (`mkChat.toggle()`) |

No race car, track or anything else from Formula Student AI.

Fallbacks:
- **No WebGL, or the scene fails to start:** show a still image of the room (rendered from the
  scene), with the same objects as plain links positioned over it or listed beside it.
- **Phones and weak GPUs:** the prototype's adaptive quality tiers (lower resolution, lighter
  bloom, fewer particles, shadows updated less often). On small screens the room sits below the
  text, not beside it.
- **`prefers-reduced-motion`:** no camera flights or idle animation; the lamp still follows the
  cursor; clicking opens the card without the zoom.
- The scene's JavaScript loads only on the homepage, after the text has rendered. The page must be
  readable and usable while it loads.

### 2. Proof strip

Four short facts in a row:
- First-Class MEng, Leeds
- 50% faster than the MATLAB original (pNanoLocz)
- Two research internships
- Centimetre-accurate 3D reconstruction

### 3. Selected work (`#work`)

Five cards, one per field, to show range. Each card has:
- the field label (`// biomedical`)
- the title
- one line on the result, from the CV
- tech tags
- a link to the existing project page

| Field | Project | Page |
|-------|---------|------|
| Biomedical | Flow diverter stent deployment simulation | `/projects/medical-simulation` |
| Scientific software | pNanoLocz AFM analysis platform | `/projects/pnanolocz` |
| Computer vision | MEng thesis: 3D/4D reconstruction pipeline | `/projects/3d-reconstruction` |
| Cloud | Azure serverless inventory | `/projects/distributed-systems` |
| Infrastructure | Home lab & local LLM orchestration | `/projects/home-lab` |

Then "All projects →" to `/projects`.

### 4. What I'm building (`#building`)

A short intro line in the voice of "things I'm building on my own time", then two cards:
- **Synergy:** a local-first code intelligence platform that turns a codebase into a queryable
  graph for developers and coding agents. Links to `/projects/workflow-graph-platform`.
  Rename that page's title to lead with "Synergy"; keep the URL.
- **HireForge:** LLM-run technical hiring assessments. One agent generates a codebase with a
  seeded defect, the candidate works on it, and an independent agent reviews the work and
  produces a scorecard. There is no project page for it; the card has no outbound link.

Facts and tech tags for these two come from their repos, not the CV, because the CV is older.
The same text is used in the workshop cards and on the monitors.

**Synergy.** Source: `/home/mk/code/personal/synergy/SYNERGY_BUSINESS_BRIEF.md` §1.
- Python backend, React 19 / Vite / TypeScript frontend, SQLite.
- tree-sitter parsing of 8 languages.
- MCP server with 17 tools.
- Revision history and structural diffs.

The C#/.NET + Blazor + Neo4j design on the current page was replaced on purpose.

**HireForge.** Source: `/home/mk/code/personal/hireforge/README.md`.
- Python, FastAPI, PostgreSQL, TypeScript frontend, Docker.
- Two independent LLM agents: one generates the codebase, one reviews the candidate's work.

**The Synergy page** (`/projects/workflow-graph-platform`, Muhammad's decision): add a
"Current status" section at the top describing where Synergy is today, from the brief:
- What works today: polyglot parsing into a typed graph, incremental analysis, revision
  history and commit snapshots, structural diffs with blast-radius ranking, the web UI views,
  the MCP server.
- What it's built with, and that it's in development and local-first.
- What's next, in engineering terms: the issues the brief lists, such as setup for agents and
  packaging. These are engineering next steps, not business plans.

Don't explain or justify the change of stack (Muhammad's decision); just describe the current
one. The existing write-up stays below it, relabelled "How it started". No market, pricing or
competitor content from the brief.

The prototype's card and terminal text for both are placeholders. Replace them with these
facts; the HireForge terminal acts out generate → candidate works → review → scorecard.

Both cards carry an "in development" label. No pricing, sign-ups or waitlists.

### 5. Experience (`#experience`)

A short timeline, newest first:
- MEng Computer Science with AI, University of Leeds, 2022–2026, First-Class Honours
- Summer Internship, University of Leeds, stent deployment simulation, Jun–Aug 2025
- Summer Internship, University of Leeds, pNanoLocz, Jun–Aug 2024
- Core Executive Leadership Team, Leeds AI Society

### 6. Contact (`#contact`)

The existing `ContactForm` component restyled, plus email, GitHub, LinkedIn and CV links.

The long About Me text currently on the homepage moves to `/about`.

## Rest of the site

### Projects page (`/projects`)

One grid of all 25 projects, with two filters:
- field (new)
- the existing category (Academic, Professional, Personal)

Fields:
- AI & ML
- Computer vision & graphics
- Scientific & biomedical
- Cloud & distributed
- Systems & infrastructure
- Product & business

A project can have more than one field. Add the field to the project data in
`website.api/Services/ProjectService.cs` and carry it through `website.Data.Project`. The
mapping of projects to fields is the builder's call.

Filtering must work without WebAssembly; plain JS or query-string links are fine. Featured /
in-development markers stay.

### Project pages (25)

Each project page is currently hand-built (~200–650 lines). Introduce one shared template
component and move every page onto it. The template:

- a header: breadcrumb, field label, title, the one-line result, tech tags, status
  (e.g. in development), links (GitHub, documents) where the page has them
- a body: the page's existing content, restyled. Keep text, images, equations (KaTeX), code
  blocks and PDF downloads. Re-lay it out, but don't rewrite it.
- a footer: "Next project" and "Back to work"

The interactive parts of `MedicalSimulationProject` (image gallery) and `HomeLabProject`
(bridge status) keep working.

### About, Ethos, AI Integration, Not Found

Restyle to match. About also gets the long About Me text from the homepage. Update anything
still describing Muhammad as a current student.

## Accessibility and SEO

- The canvas is decorative to screen readers (`aria-hidden`); everything it links to is also a
  normal link on the page (Selected work, What I'm building, Experience).
- Visible focus styles; keyboard users can reach every link without the 3D.
- Text contrast meets WCAG AA on the dark background.
- All homepage text is server-rendered HTML, so search engines and link previews see it.
  Keep the per-page canonical URLs and titles.

## Performance

My calls; the builder can tighten them:
- **Homepage:** name, buttons and section text are readable before the 3D script runs. The 3D
  JS and assets load after first paint and don't block it.
- **Other pages:** load no three.js and no scene code. They should be no heavier than today.
- **three.js:** host it from the site (pinned r160 in `wwwroot`) rather than a CDN, so a CDN
  outage or a corporate block can't break the hero.
- **CPU:** the scene pauses when the hero scrolls off-screen or the tab is hidden.
- **Testing:** the prototypes were only measured with software rendering (18 fps desktop,
  30 fps phone for the portal).
  - Before the homepage PR merges, run that branch and give Muhammad a link he can open on an
    ordinary laptop with integrated graphics. Use one his laptop can reach: the local network,
    or Tailscale, which he already uses.
  - That PR waits for his OK. Other PRs follow the normal merge-when-checks-pass rule.

## Decisions left to the builder

- File and component structure. For example: a JS module for the scene plus a small Razor
  wrapper, and the shared project page component.
- How to split the work into PRs. It should land in reviewable steps; the homepage doesn't
  have to wait for all 25 project pages.
- Exact copy for the hero line, card one-liners and section intros, taken from the CV and
  matching its facts.
- The field each project belongs to.
- Exact colours, spacing and type scale within the visual language above.
- How the no-WebGL still image is produced and how its links are laid out.
- Whether `vanilla-tilt.min.js` and the old hero CSS in `app.css` are still needed.

## Out of scope

- The CV's skills list still says ".NET 8"; the site runs .NET 10. That's a CV edit for Muhammad.
- The other prototypes (city, building, islands, scanner, neural core) are kept in
  `/home/mk/code/personal/experiments/3d-portfolio-prototypes/` for possible use elsewhere.
  They're not part of this site.
