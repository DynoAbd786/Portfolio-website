# Hero Takeover Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The homepage hero becomes a full-screen room introduced by a "reconstructed" name build that doubles as the loading screen.

**Architecture:** The hero keeps its server-rendered markup (it is the no-JS and reduced-motion state). A head script decides per page load whether the intro runs and marks `<html>` with `intro-pending`. A small ES module draws the name build on a canvas inside `#hero`. An orchestrator in `site.js` (`mkIntro`) locks scrolling, waits for the build and the scene, then lands the name on the real `<h1>` and starts the scene's own entrance. The scene frames itself from a CSS variable `--frame-x`, so the room (and a focused object) sits in the free space right of the text or card.

**Tech Stack:** ASP.NET Core / Blazor static SSR, Tailwind 3.4, three.js 0.160.0, Delaunator 5.0.1 (vendored), self-hosted Inter 800, xUnit + AngleSharp host tests, Playwright (pytest) browser tests.

**Spec:** `docs/superpowers/specs/2026-10-27-hero-takeover-design.md` (reference prototype: `docs/superpowers/prototypes/2026-10-27-hero-intro.html`, whose `play()` is the reference for the name build)

## Global Constraints

- Shell: `export PATH=$HOME/.dotnet:$PATH DOTNET_ROOT=$HOME/.dotnet`; use `/usr/bin/grep`.
- Host tests: `dotnet test tests/website.tests` (all green before every commit). Browser tests: `uv run -q --with pytest --with pytest-playwright pytest tests/browser -q` (port 5199 must be free).
- Static SSR only; no Blazor render mode on the hero. Changes live in `Index.razor`, `App.razor`, `site.js`, `wwwroot/js/workshop/`, a new `wwwroot/js/intro/`, `app.css`, `Styles/tailwind.css`, `HomeContent.cs`.
- three.js stays 0.160.0 from `wwwroot/lib/three/`. New third-party files are vendored under `wwwroot/lib/` or `wwwroot/fonts/`, never loaded from a CDN, and only on the homepage.
- Colours from the Tailwind tokens (`ink`, `ink-2`, `line`, `cy`, `lamp`); text keeps WCAG AA contrast (`test_contrast.py`).
- No em dashes in page text (`Page_has_no_em_dashes`).
- Exact copy: kicker `// software engineer · ai systems`; loading label `building the workshop`; scroll cue `scroll`; phone hint `tap the room to explore`; buttons in order **See my work** (`#work`, ghost), **Download CV** (ghost), **Get in touch** (`#contact`, primary with the tracing light).
- Name build: 4 stages over 3.3 s (point cloud 0–1.45 s, scan 1.05–1.95 s, facets 1.75–2.75 s, polish 2.75–3.1 s); size `min(170px, 8.5% of width)` on desktop, `15% of width` on the phone layout; mesh cut by one mask holding both lines; outline drift `max(0.9, 1.1% of font size)` px along smooth noise plus 9 chips (5 on the phone layout); no other imperfections.
- Timing: the name lands when the build is done **and** the scene has settled (live, failed, given up or not mounting), or 10 s after navigation start, whichever is first; the scroll lock lifts at landing.
- The intro runs only on a full load of `/` with no hash, first in the tab (`sessionStorage['mk-intro-seen']`), JavaScript on, no reduced motion, no `debug=workshop` in the query. A reload always starts at the top, even with a hash; Back/Forward keep the browser's normal scroll restoration.
- Phone layout = viewport width under 900px, or height under 500px.

## Review Focus

- Enhanced navigation away from `/` while the intro runs: the lock, timers and listeners must be released (a test in Task 4).
- The window resizing or the phone rotating mid-intro: the name must land on the `<h1>`'s current position (a test in Task 4).
- The self-hosted font or Delaunator failing to load: the landed state must show at once, never a stuck lock (a test in Task 4).
- Tab / Shift-Tab during the lock: a keyboard user's deliberate move ends the intro like a header click, instead of focus scrolling the page under the build (a test in Task 4).
- The homepage opened in a background tab: the build must not be "played" unseen; the intro starts when the tab becomes visible and the 10 s limit counts from then (a test in Task 4).

---

### Task 1: Hero layout, buttons and the slide-over

The server-rendered hero in its landed state: full-bleed room on desktop, text column over the left, the new buttons, the phone order, and the page sliding over a sticky hero. No intro yet.

**Files:**
- Modify: `website/Pages/Index.razor:9-75` (hero markup), `website/Pages/Index.razor` (wrap everything after `#hero` in `<div class="hero-cover">`)
- Modify: `website/wwwroot/css/app.css:26-160` (workshop stage, stacked layout), `website/Styles/tailwind.css` (add `.btn-shine`)
- Modify: `website/wwwroot/js/workshop/ui.js` (phone hint text)
- Test: `tests/website.tests/HomePageTests.cs`, `tests/browser/test_hero.py` (new)

**Interfaces:**
- Produces: markup `#hero > section > .hero-copy > (.hero-head [kicker, h1], .hero-body [blurb, .hero-buttons, open-to-roles])`, then `#workshop`, then `.workshop-links`; elements `.hero-loading` (text `building the workshop`) and `.hero-scroll` (text `scroll`) inside `#hero`; `<canvas class="hero-namefx" aria-hidden="true">` inside `#hero`; CSS custom property `--frame-x` on `#hero` (unitless 0–1, default `.62`); `.hero-cover` wrapping the rest of the page; class `.btn-shine`.

- [ ] **Step 1: Write the failing host test**

```csharp
[Fact]
public async Task Hero_buttons_are_see_my_work_cv_then_get_in_touch()
{
    var doc = await Html.GetAsync(_client, "/");
    var buttons = doc.QuerySelectorAll(".hero-buttons a").ToList();
    Assert.Equal(new[] { "See my work", "Download CV", "Get in touch" }, buttons.Select(b => b.TextContent.Trim()));
    Assert.Equal("#work", buttons[0].GetAttribute("href"));
    Assert.StartsWith("/docs/Muhammad-Kashif-Khan-CV.pdf", buttons[1].GetAttribute("href"));
    Assert.Equal("#contact", buttons[2].GetAttribute("href"));
    Assert.Contains("btn-primary", buttons[2].ClassList);
    Assert.Contains("btn-shine", buttons[2].ClassList);
    Assert.All(buttons.Take(2), b => Assert.Contains("btn-ghost", b.ClassList));
    Assert.NotNull(doc.QuerySelector("#hero .hero-head h1"));
    Assert.NotNull(doc.QuerySelector("#hero canvas.hero-namefx"));
    Assert.NotNull(doc.QuerySelector(".hero-cover #work"));
}
```

- [ ] **Step 2: Write the failing browser tests in `tests/browser/test_hero.py`** (homepage at `/?debug=workshop`, so no intro)

- `test_desktop_room_fills_the_hero_height` (1440×900): `#workshop` canvas height ≥ `innerHeight - 64 - 2`; canvas width == `innerWidth`.
- `test_desktop_still_box_sits_right_of_the_text` (1440×900, `#workshop[data-state=live]` not required): `.workshop-still` box left edge ≥ `.hero-copy` right edge − 40, and its height == stage height.
- `test_phone_order_is_name_room_text` at 390×844 and 844×390: vertical order of `.hero-head`, `#workshop`, `.hero-body`, `.workshop-links` is increasing; `#workshop` width == viewport width; aspect 4:3 ± 1px.
- `test_page_slides_over_the_hero` (1440×900): after `scrollTo(0, 500)`, `#hero` top is still 64 and `#work` paints above it (`elementFromPoint` at the viewport centre is inside `.hero-cover`).
- `test_get_in_touch_has_the_tracing_light`: computed `animation-name` of `.btn-shine::before` is `btn-shine-trace`.

- [ ] **Step 3: Run them to verify they fail**

Run: `dotnet test tests/website.tests --filter Hero_buttons` and `uv run -q --with pytest --with pytest-playwright pytest tests/browser/test_hero.py -q`
Expected: FAIL (no `.hero-buttons`, stage is 16:10 in a column).

- [ ] **Step 4: Implement the markup and CSS**

- Desktop (≥900px wide and ≥500px tall): `#hero` is `position: sticky; top: 4rem; height: calc(100svh - 4rem); overflow: hidden`. `#workshop` is absolute and fills `#hero` (full width, under the text). `.hero-copy` stays in the `max-w-7xl` container, vertically centred, `z-index` above the stage. `.workshop-still` becomes a box of the stage's height, aspect 16:10, centred at `calc(var(--frame-x) * 100%)`; the canvas fills the stage. Replace the radial mask with a left-edge fade (`linear-gradient(90deg, transparent 0, #000 12%)` on the still box; on the canvas fade from `calc(var(--frame-x) * 100% - 45vh)`).
- `.hero-cover`: `position: relative; z-index: 10; background: theme(colors.ink)` with a top shadow `0 -30px 60px rgba(0,0,0,.5)` and a top border in `line`.
- Phone layout (`max-width: 899px`, and `max-height: 499px`): `.hero-copy` is `display: contents`; order `.hero-head` 1, `#workshop` 2, `.ws-card` 3, `.hero-body` 4, `.workshop-links` 5; the stage is edge to edge (`width: calc(100% + 2rem); margin-inline: -1rem`) at 4:3 (this replaces today's 640–899px 16:10 rule); `#hero` is not sticky; `.hero-scroll` hidden.
- `.btn-shine`: `@property --shine-a { syntax: '<angle>'; inherits: false; initial-value: 0deg }`; `::before` a 2px ring masked to the border (`mask-composite: exclude`) with `conic-gradient(from var(--shine-a), transparent 0 230deg, rgba(255,255,255,.15) 270deg, #fff 330deg, transparent 360deg)`; `::after` the same gradient blurred 10px at .55 opacity; both `animation: btn-shine-trace 3s linear infinite` (`to { --shine-a: 360deg }`); none under `prefers-reduced-motion`.
- `ui.js`: the `ws-hint-tap` text becomes `tap the room to explore`; on the phone layout the hint sits bottom-left on the room.
- Phone card sheet: `max-height: 44svh`, title 20px, description 14px, points 13px.
- `.hero-loading` and `.hero-scroll` are hidden unless `html.intro-pending` / landed (Task 4 drives them); `.hero-scroll` shows on desktop once landed.

- [ ] **Step 5: Run the new tests, then both suites**

Run: the two commands from Step 3, then both full suites from Global Constraints.
Expected: new tests PASS. Fix existing tests that encoded the old layout, keeping their intent: `test_see_my_work_scrolls_to_work` (now a ghost button), `test_phone_room_comes_first_and_fills_the_width` (the name now comes first; the room still fills the width), `test_phone_sections_do_not_move_when_the_scene_starts`, the `test_fallback.py` pin tests (pins are % of the still box, unchanged). Record each change as a ruling.

- [ ] **Step 6: Commit**

```bash
git add website/Pages/Index.razor website/wwwroot/css/app.css website/Styles/tailwind.css website/wwwroot/js/workshop/ui.js tests/
git commit -m "feat: full-height hero with the room behind the text; Get in touch with a tracing light"
```

### Task 2: Scene framing, held entrance and pausing

The live scene frames the room at `--frame-x`, centres a focused object right of its card, can hold its entrance until told, and can pause while covered. Re-render the still to match.

**Files:**
- Modify: `website/wwwroot/js/workshop/workshop.js` (`layout()`, camera goal, `mount` options, instance, debug hooks), `website/wwwroot/js/workshop/ui.js` (expose the card's right edge), `website/wwwroot/js/workshop/hotspots.js` (focus poses if needed)
- Modify: `website/wwwroot/js/site.js` (`mkWorkshop`: pass options, `settled()`, `startIntro()`, `setPaused()`, `mkHeroFrame()`)
- Modify: `website/wwwroot/images/workshop/still.webp`, `still@2x.webp`, `website/Data/HomeContent.cs` (PinX/PinY)
- Test: `tests/browser/test_workshop.py`

**Interfaces:**
- Consumes: `--frame-x` on `#hero` (Task 1).
- Produces:
  - `mount(root, data, { holdIntro = false } = {})`; the instance gains `startIntro(): void` (entrance clock starts; no-op when not held or reduced) and `setPaused(paused: boolean): void` (stops/resumes the render loop like `inView`).
  - `window.mkWorkshop.settled(): Promise<'live' | 'still'>`: resolves `'live'` on the first frame, `'still'` if the mount fails, the scene gives up, `_tooSlow()` is true, or there is no `#workshop`. Resolves at once if already known.
  - `window.mkWorkshop.startIntro()`, `window.mkWorkshop.setPaused(p)`: forward to the instance if any.
  - `window.mkHeroFrame(): void`: sets `--frame-x` on `#hero`: on desktop the centre of the space between `.hero-copy`'s right edge and the viewport's right edge, as a fraction of `#hero` width; `0.5` in the phone layout. Called on DOMContentLoaded, `enhancedload` and `resize`.
  - `ui.frameX(): number | null`: while a card is open on desktop, the centre of the space right of `.ws-card`, as a fraction of the stage width; otherwise `null`.
  - Debug hook `root.__workshop.project(key): { x, y }`: the focused object's camera target in stage pixels.

- [ ] **Step 1: Write the failing browser tests** in `tests/browser/test_workshop.py` (`/?debug=workshop`, 1440×900, wait for `data-state=live`)

- `test_no_hotspot_sits_under_the_hero_text`: every `__workshop.pins()` x (as % of stage) × stage width > `.hero-copy` right edge.
- `test_room_fills_most_of_the_hero_height`: the projected bounding box of all hotspot pins spans ≥ 55% of the stage height.
- `test_focused_object_is_centred_right_of_its_card`: click the stent's pin; after 1.5 s, `__workshop.project('stent').x` is within 6% of the stage width of `ui.frameX() × width`, and > `.ws-card` right edge.
- `test_held_entrance_waits_for_start`: mount with `holdIntro` (navigate to `/?debug=workshop&hold=1`; `site.js` passes `holdIntro` when `hold=1` is in the query, for tests); after 2 s the camera is still at the entrance pose (`__workshop.introT()` == 0); after `mkWorkshop.startIntro()` and 2.5 s, `introT()` > 2.2.
- `test_paused_scene_draws_no_frames`: `mkWorkshop.setPaused(true)`; `__workshop.frames()` is unchanged over 500 ms; `setPaused(false)` resumes.

- [ ] **Step 2: Run them to verify they fail**

Run: `uv run -q --with pytest --with pytest-playwright pytest tests/browser/test_workshop.py -q -k "under_the_hero or most_of_the or centred_right or held_entrance or paused_scene"`
Expected: FAIL.

- [ ] **Step 3: Implement**

- Framing: in `layout()` and each frame, read the wanted centre `fx` = `ui.frameX() ?? --frame-x` (desktop) and set the horizontal film offset so the camera target projects at `fx` of the stage width:

  ```js
  // the target projects at fraction fx of the width when the film is shifted by this much
  camera.filmOffset = (0.5 - fx) * 2 * Math.tan(THREE.MathUtils.degToRad(camera.fov / 2)) * camera.aspect * camera.getFilmWidth();
  ```

  Ease `fx` towards its goal with the camera's `damp(dt, 2.6)` so the room glides. The phone layout keeps today's 4:3 offset (`-1` when `W / H < 1.45`). Tune `HOME.r` (and `FRAME`) so the room fills the stage height (the test's 55%).
- Hold: when `holdIntro`, `intro` stays 0 (entrance pose, objects not popped) until `startIntro()`; the first frame still renders and sets `data-state=live`.
- Pause: `setPaused` feeds `updateLoop()` alongside `inView`.
- Desktop swap (CSS in `app.css`): when a card opens, `.hero-copy` slides out (`translateX(-120%)`, opacity 0, .7 s `cubic-bezier(.65,0,.25,1)`) and `.ws-card` slides in from the left into the same slot (.15 s later); closing reverses it.
- Debug hooks: `introT()`, `frames()`, `project(key)`.
- `snapshot(width, height)` renders with `fx = 0.5` (the still box is centred on `--frame-x` by CSS).

- [ ] **Step 4: Run the tests, re-render the still and update pins**

Run the Step 2 command (Expected: PASS). Then run the site on 5199 and `uv run --with playwright python tools/render-workshop-still.py http://127.0.0.1:5199`; paste the printed PinX/PinY into `HomeContent.Workshop`. Then both full suites (Expected: all PASS, including `test_pins_sit_on_their_objects`).

- [ ] **Step 5: Commit**

```bash
git add website/wwwroot/js website/wwwroot/images/workshop website/Data/HomeContent.cs tests/browser/test_workshop.py
git commit -m "feat: the scene frames the room right of the text and centres a clicked object right of its card"
```

### Task 3: The name build

A self-contained drawer for the reconstructed name, plus its vendored font and triangulator. No orchestration.

**Files:**
- Create: `website/wwwroot/js/intro/name-build.js`
- Create: `website/wwwroot/lib/delaunator/delaunator.min.js` (Delaunator 5.0.1 UMD build, which bundles robust-predicates; sets `window.Delaunator`), `website/wwwroot/fonts/inter-latin-800-normal.woff2` (from `@fontsource/inter@5.1.0/files/`)
- Modify: `website/wwwroot/css/app.css` (`@font-face { font-family: 'Inter Build'; font-weight: 800; src: url('/fonts/inter-latin-800-normal.woff2') format('woff2'); font-display: block }`)
- Modify: `website/Pages/Index.razor` (`<HeadContent>`: `<link rel="preload" as="font" type="font/woff2" crossorigin href="/fonts/inter-latin-800-normal.woff2">` and `<link rel="preload" as="script" href="/lib/delaunator/delaunator.min.js">`)
- Test: `tests/browser/test_name_build.py` (new)

**Interfaces:**
- Produces: `export function createNameBuild(canvas, { lines, fontFamily, fontSize, centreY, phone, seed, Delaunator }): NameBuild`, where `NameBuild` is `{ duration: 3.3, draw(t: number): void, textBox: { left, top, width, height } }` (CSS pixels within the canvas; `textBox` is the solid text's ink box, used to fly it to the `<h1>`). `canvas` is already sized to its CSS box × `devicePixelRatio`. `draw(t)` renders the frame at `t` seconds and is pure in `t` for a given seed (the tests rely on that).
- A test page hook: `/?debug=namebuild` makes `site.js` expose `window.__nameBuild = { create: (opts) => createNameBuild(...) }` after loading the module and Delaunator.

- [ ] **Step 1: Write the failing tests in `tests/browser/test_name_build.py`** (`/?debug=namebuild`, 1440×900; build on a fresh canvas with `seed: 1`)

- `test_facets_cover_the_letters_without_gaps`: `draw(2.6)`; among pixels where a reference canvas with the plain text is opaque, ≥ 97% are opaque in the build canvas.
- `test_outline_is_only_slightly_off_the_font`: `draw(2.6)`; no opaque build pixel is more than `ceil(max(0.9, 0.011 × fontSize)) + 1` px from the reference text, and ≥ 0.5% of reference edge pixels differ (it is not a perfect copy).
- `test_polish_ends_on_plain_text`: `draw(3.3)` matches the reference text pixel for pixel within alpha 8.
- `test_points_settle_by_the_scan`: `draw(1.45)` draws ≥ 600 cyan (`#67e8f9`-ish) points, all within 3 px of the letters.
- `test_phone_size_and_chip_count`: on 390×844 with `phone: true`, `fontSize` is 15% of the width and the build's chip count (`build.chips`, exposed for tests) is 5; 9 on desktop.

- [ ] **Step 2: Run them to verify they fail**

Run: `uv run -q --with pytest --with pytest-playwright pytest tests/browser/test_name_build.py -q`
Expected: FAIL (`createNameBuild` missing).

- [ ] **Step 3: Implement `createNameBuild`**, porting the prototype's `play()` frame code (v8 in `docs/superpowers/prototypes/2026-10-27-hero-intro.html`): grid-jittered vertices plus evenly spaced outline vertices; per-vertex heights; Lambert-lit facet colours; one mask canvas holding both lines, nudged by the strip displacement and chipped; stage times from Global Constraints. Use a seeded PRNG (mulberry32) instead of `Math.random`, `Delaunator.from(points).triangles` instead of d3. Precompute everything in `createNameBuild`; `draw(t)` only paints.

- [ ] **Step 4: Run the tests, then both suites**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add website/wwwroot/js/intro website/wwwroot/lib/delaunator website/wwwroot/fonts website/wwwroot/css/app.css website/Pages/Index.razor tests/browser/test_name_build.py
git commit -m "feat: the name build, a point cloud reconstructed into the name"
```

### Task 4: The intro: when it runs, the lock and the landing

**Files:**
- Modify: `website.api/Components/App.razor` (head script, next to the `reveal-ready` one)
- Create: `website/wwwroot/js/intro/intro.js`
- Modify: `website/wwwroot/js/site.js` (`window.mkIntro`), `website/wwwroot/css/app.css` (`intro-pending` / `intro-landed` states)
- Modify: `tests/browser/conftest.py` (skip the intro by default)
- Test: `tests/browser/test_intro.py` (new)

**Interfaces:**
- Consumes: `createNameBuild` (Task 3); `mkWorkshop.settled()`, `mkWorkshop.startIntro()`, mount option `holdIntro` (Task 2); `.hero-namefx`, `.hero-loading`, `.hero-head h1` (Task 1).
- Produces:
  - Head script: adds `intro-pending` to `<html>` and sets `sessionStorage['mk-intro-seen'] = '1'` when every condition in Global Constraints holds; on a reload (`performance.getEntriesByType('navigation')[0].type === 'reload'`) sets `history.scrollRestoration = 'manual'`, drops any hash with `history.replaceState`, scrolls to the top on `DOMContentLoaded` and `load`, then sets `scrollRestoration` back to `'auto'`.
  - `export async function runIntro({ hero, nameBuild, settled, startScene, startedAt }): Promise<void>` in `intro.js`: resolves once landed. `nameBuild` is a factory `(canvas, opts) => NameBuild`.
  - `window.mkIntro = { running: boolean, finish(): void }` in `site.js`: starts `runIntro` on DOMContentLoaded when `<html>` has `intro-pending`, passing `holdIntro: true` to the first `mkWorkshop.sync()`; `finish()` lands at once.
  - CSS: under `html.intro-pending`, `.hero-head h1` is `visibility: hidden`, the kicker, `.hero-body`, `.ws-hint`, and `.hero-scroll` are `opacity: 0` (the header stays visible and usable), the stage is `opacity: 0`, `.hero-loading` shows; `html.intro-landed` fades them in (kicker and body with a .9 s delay, the stage over 1.4 s growing from `scale(1.12)`), then both classes are removed.
  - conftest: an override of pytest-playwright's `context` fixture that runs `add_init_script("try{sessionStorage.setItem('mk-intro-seen','1')}catch(e){}")` unless the test is marked `@pytest.mark.intro`; the same init script on the contexts tests create themselves in `test_fallback.py` and `test_workshop.py`.

- [ ] **Step 1: Write the failing tests in `tests/browser/test_intro.py`** (all `@pytest.mark.intro`, 1440×900 unless stated)

- `test_intro_plays_and_lands_on_the_h1`: on `/`, `html.intro-pending` is set at `domcontentloaded`; within 12 s it is gone; the canvas `.hero-namefx` is hidden; the `<h1>` is visible at the same rect it has with the intro skipped (± 2 px).
- `test_scroll_is_locked_until_the_name_lands`: during the intro, mouse wheel, PageDown, Space and a touch swipe leave `scrollY == 0`; after landing, a wheel scrolls.
- `test_name_holds_until_the_scene_is_ready`: delay `/js/workshop/workshop.js` by 6 s with `page.route`; at 5 s the intro is still pending and the label `building the workshop` is visible; it lands after the scene goes live.
- `test_lands_after_ten_seconds_on_the_still`: block `/js/workshop/workshop.js` (route never fulfils); lands by 10.5 s with the still image visible.
- `test_intro_plays_once_per_tab`: after landing, `page.reload()`: no `intro-pending`; `scrollY == 0`.
- `test_reload_starts_at_the_top_even_with_a_hash`: land, click Get in touch (URL has `#contact`), `page.reload()`: `scrollY == 0`.
- `test_back_restores_the_scroll_position`: land, scroll to `#work`, click the first selected-work card, `page.go_back()`: `scrollY` within 50 px of where it was.
- `test_no_intro_for_a_section_link`: `/#contact` from a fresh tab: no `intro-pending`, lands on `#contact` (keeps `test_anchor_from_other_page_lands_on_section` true).
- `test_no_intro_from_enhanced_navigation`: fresh tab on `/projects`, click the logo: no `intro-pending` on `/`.
- `test_no_intro_with_reduced_motion`: context `reduced_motion="reduce"`: no `intro-pending`, page scrolls at once.
- `test_header_click_ends_the_intro`: during the build, click the header's Contact link: intro ends at once, page ends on `#contact`.
- `test_tab_key_ends_the_intro` (Review Focus): during the build, press Tab: intro ends; `scrollY` is unchanged by the focus.
- `test_leaving_mid_intro_releases_the_lock` (Review Focus): during the build, click the header's Projects link: on `/projects` a wheel scrolls; `mkIntro.running` is false.
- `test_resize_mid_intro_lands_on_the_h1` (Review Focus): at 1 s resize to 1100×800; the `<h1>` is visible at its new rect and the canvas hidden after landing.
- `test_missing_font_or_triangulator_shows_the_landed_hero` (Review Focus): abort `/fonts/inter-latin-800-normal.woff2` (then separately `/lib/delaunator/delaunator.min.js`): `intro-pending` is gone within 1 s of `load` and the page scrolls.
- `test_background_tab_waits_to_be_seen` (Review Focus): open `/` in a page, emulate `document.visibilityState = 'hidden'` before scripts run (init script overriding `visibilityState` and firing `visibilitychange` later); after 12 s hidden the intro is still pending; after becoming visible it plays and lands.

- [ ] **Step 2: Run them to verify they fail**

Run: `uv run -q --with pytest --with pytest-playwright pytest tests/browser/test_intro.py -q`
Expected: FAIL (no `intro-pending`).

- [ ] **Step 3: Implement the head script, `runIntro` and `mkIntro`**

`runIntro` order: wait for the tab to be visible; size `.hero-namefx` to `#hero`; load the font (`document.fonts.load("800 100px 'Inter Build'")`) and Delaunator (`<script>` tag, resolves on `load`); if either fails, land at once. Lock scrolling (`html { overflow: hidden }` plus non-passive `wheel`/`touchmove` and scroll-key `keydown` handlers that `preventDefault`). Animate `build.draw(t)` with `requestAnimationFrame` until `t >= build.duration`, then wait for `settled()` or the 10 s limit (from `startedAt`, the moment the tab first became visible). Land: fly the canvas with a 1.1 s `cubic-bezier(.7,0,.2,1)` transform so `build.textBox` lands on the `<h1>`'s current rect (measured at landing), swap in the real `<h1>`, add `intro-landed`, call `startScene()`, unlock, remove listeners. `finish()` (header link or anchor click captured on `document`, Tab key, or `enhancedload` to another page) skips straight to the landed state and runs the same cleanup.

- [ ] **Step 4: Run the intro tests, then both full suites**

Expected: PASS. Existing tests keep passing because conftest marks the intro as seen.

- [ ] **Step 5: Commit**

```bash
git add website.api/Components/App.razor website/wwwroot/js website/wwwroot/css/app.css tests/browser
git commit -m "feat: the name build is the homepage's loading screen; scrolling waits for it"
```

### Task 5: Covering the room

**Files:**
- Modify: `website/wwwroot/js/site.js` (a scroll handler beside `mkHeroFrame`)
- Test: `tests/browser/test_hero.py`

**Interfaces:**
- Consumes: `.hero-cover` (Task 1), `mkWorkshop.setPaused` (Task 2).
- Produces: on desktop, `--cover` on `#hero` = `clamp(0, scrollY / heroHeight, 1)`; the stage's `opacity: calc(1 - var(--cover) * .65)` and `transform: scale(calc(1 - var(--cover) * .06))`; `mkWorkshop.setPaused(cover >= 1)`. Nothing on the phone layout.

- [ ] **Step 1: Write the failing tests**

- `test_room_dims_as_the_page_covers_it`: at `scrollY` = half the hero height, the stage's computed opacity is between .6 and .75.
- `test_fully_covered_scene_stops_drawing`: scroll past the hero; `__workshop.frames()` unchanged over 500 ms; scroll back; frames resume.

- [ ] **Step 2: Run them to verify they fail**, **Step 3: implement**, **Step 4: run them and both full suites** (Expected: PASS)

- [ ] **Step 5: Commit**

```bash
git add website/wwwroot/js/site.js website/wwwroot/css/app.css tests/browser/test_hero.py
git commit -m "feat: the room dims and stops drawing as the page slides over it"
```

### Task 6: Docs and a last look

**Files:**
- Modify: `CLAUDE.md` (Homepage section: the name build, `intro-pending`, `mkIntro`, `--frame-x`, the conftest default and `@pytest.mark.intro`, the reload rule)

- [ ] **Step 1: Update `CLAUDE.md`**, **Step 2: run both full suites** (Expected: all PASS), **Step 3: screenshot `/` at 2560×1440, 1440×900, 390×844 and 844×390 during the build and after landing**, and compare with the prototype; list anything that differs in the ledger.

- [ ] **Step 4: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: the hero intro in CLAUDE.md"
```
