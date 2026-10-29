# Homepage hero: the name build and the room takeover

Date: 2026-10-27
Status: draft for approval (builder check done)
Builds on: `2026-10-26-portfolio-redesign-design.md` (the workshop hero, PR #5 and the stack above it)
Reference prototype: `docs/superpowers/prototypes/2026-10-27-hero-intro.html` (open it from the repo
in a browser; the room is the still image there, the real one is the live scene)

## Why

On a wide screen the workshop is small: the hero is capped at `max-w-7xl` and the room gets about
60% of that, so on a 2560px monitor it sits in a box in the middle of a lot of empty grid. Muhammad
wants the first screen to be large and eye-catching. The live scene also takes a moment to load,
and that wait currently shows nothing interesting.

The job of the hero is unchanged from the redesign spec: make a recruiter stop, and never put the
name, the CV or the way to get in touch behind a wait or a click.

## What a visitor sees

### 1. The name build (while the scene loads)

The name, "Muhammad / Kashif-Khan", is reconstructed in the middle of the screen, the way the MEng
project reconstructs a scene. About 3.3 seconds, in four stages:

1. **Point cloud**: cyan points fly in from a loose cloud and settle into the letter shapes.
2. **Scan**: a glowing vertical scan line sweeps left to right; behind it the points are wired into
   a fine triangle mesh (cyan wireframe).
3. **Facets**: the mesh fills, left to right, with lit low-poly facets: neighbouring triangles share
   vertex heights and are lit by one light from the top left, so it reads as faceted crystal.
4. **Polish**: the facets fade into solid text.

Quality rules (Muhammad rejected earlier versions on these):

- The mesh covers the whole word and is cut to the letters with one mask (both lines in one mask:
  cutting line by line leaves only their overlap). No gaps or holes, ever.
- The outline is *slightly* imperfect, like a scanned surface: edges drift by a pixel or two along
  smooth noise, plus a handful of tiny chips (9 on desktop, 5 on phones). Nothing more.
- No other imperfections: no vertices wobbling or settling, no facets filling late, no stray points
  off the letters, no missing wireframe edges. (Tried in prototype v7 and rejected.)
- Sized to the screen: about 8.5% of the width on desktop (capped at 170px), 15% on phones.

The prototype's `play()` is the reference implementation (d3-delaunay for the triangulation). The
real build must load nothing heavier than that, and must not delay the scene.

A small "building the workshop" label with a pulsing dot sits below the name until the scene is ready.
Before the build starts (while the font and triangulator load) the hero shows the dark grid and that
label only.

**Timing.** The name build is the loading screen for the scene (Muhammad's intent), so it is never
skipped and it covers the whole load:

- It starts as soon as it can. Self-host Inter 800 and the triangulator and preload both, so the
  build can start within a moment of the page opening rather than waiting on Google Fonts.
- It always runs to the end.
- The name lands when the build is done **and** the scene is ready. If the scene is slower than the
  build, the finished name holds in the middle with "building the workshop" under it.
- The scene giving up (no WebGL, failed load, too slow) counts as ready: the name lands on the still
  image.
- My call: if the scene is still loading 10 s after the page opened, the name lands anyway on the
  still image and the live scene replaces it when it arrives, so nobody is held indefinitely.

### 2. The landing (when the name build is done and the scene is ready)

The finished name flies to the top left and becomes the page's real `<h1>`. Under it fade in: the
kicker (`// software engineer · ai systems`), the blurb, three buttons and "Open to graduate
software & AI roles". At the same time the room grows in behind, and the hint bar (hover / click /
explored n/11) and a small "scroll" cue fade in.

- **Buttons**, in this order: **See my work** (ghost, `#work`), **Download CV** (ghost), **Get in
  touch** (primary, goes to `#contact`). Get in touch has a light that keeps tracing its outline (a
  conic gradient rotated with `@property`) and a soft glow behind it. It is the only button with this.
- **Desktop framing**: the room fills the full height under the header and the right two thirds
  of the screen, edge to edge, its left edge fading into the dark grid. The text column sits over
  the empty left third. Nothing clickable in the room may sit under the text (in the current framing
  the certificate and the Azure cloud would). This is a camera framing change in the live scene,
  not a CSS crop.
- If the scene isn't ready by the 10 s limit, the still image stands in and the live scene replaces
  it when ready, as the still/live swap works today.

### 3. Exploring

Desktop (900px and up, as today):

- Hover shows the object's tag, as today.
- Clicking an object swaps the left panel: the intro slides out to the left and the object's card
  slides into its place, while the camera moves so the object sits in the middle of the free space
  right of the card and zooms in. ×, Esc or clicking empty space reverses all of it.
- This is mostly what `ui.js` already does (card in place of `.hero-copy`, which goes `inert`); the
  change is the framing and where the camera puts the object.

Everything under 900px wide, and landscape phones (under 500px tall), uses the phone layout (my call;
it replaces today's room-first narrow layout between 640 and 900):

- Order: kicker and name, then the room edge to edge at 4:3, with "tap the room to explore · n/11"
  on it, then the blurb, the buttons (same order, wrapping to fit) and "open to roles". The name
  build lands into this position.
- On phones the buttons may sit just below the first screen; the name and the room come first
  (Muhammad approved this layout on his phone). On a 390x844 phone they fit.
- Today's swipeable row of room links stays (a test requires it), placed after "open to roles" so it
  doesn't push the buttons down (my call).
- Tapping an object scrolls the room up under the header, zooms in, and slides a short card up from
  the bottom (at most about 44% of the screen, smaller type, scrolls inside if long). The zoomed room
  stays visible above it. Today's phone sheet already does most of this.

### 4. Scrolling

- **No scrolling until the name has landed.** Wheel, touch and scroll keys do nothing while the
  intro runs, including while the name flies to its place and while the room's own entrance plays out
  (objects popping in, the camera coming to rest; 3.5 s at most after the name lands).
- See my work and Get in touch glide down to their section rather than jumping.
- The header stays visible and usable throughout. Clicking any of its links (or any in-page anchor)
  ends the intro at once: the name lands instantly, the lock lifts and the click goes where it points
  (my call; Muhammad asked that scrolling not skip the intro, but a deliberate click should win).
- **Desktop**: the hero stays put (sticky) and the rest of the page slides up over it with a shadow
  on its top edge; the room dims and pulls back slightly as it is covered. Stop rendering the scene
  once it is fully covered.
- **Phones**: the page scrolls normally.
- Everything below the hero (proof strip, selected work, what I'm building, experience, contact)
  is unchanged.

## When the intro plays

- On a full page load of `/` that is the first in a browser tab (sessionStorage), and on every reload
  of `/` (Muhammad expects a refresh to show it). Arriving at the homepage by enhanced navigation
  (clicking the logo from `/projects`) never plays it (my call: simpler, and no lock in the middle of an
  in-site click), nor does coming back to it later in the visit.
- **A reload of the homepage starts at the top**, even if the URL has a hash such as `#contact` from
  clicking Get in touch, so the intro plays from there. Other pages keep the browser's usual reload
  behaviour (it keeps your place, as on most sites). Only a real reload does this (navigation type
  `reload`); Back and Forward restore the scroll position as browsers normally do.
- A link to a section (`/#contact`, `/#work` from another page) lands on that section with no intro
  and no lock. Existing tests rely on this (`test_anchor_from_other_page_lands_on_section`,
  `test_phone_anchor_stays_on_section_while_the_scene_loads`).
- **Reduced motion**: no build, no lock; the landed state shows at once, as the scene already does
  for reduced motion.
- **No JavaScript**: the server-rendered hero must work as the landed state on its own: real `<h1>`,
  blurb and buttons, the still image with its pins. The intro only hides the copy after a script in
  the head has marked the page as able to run it (the same pattern as `reveal-ready`).
- **No WebGL or a device too slow for the scene**: the name build still plays (it is a 2D canvas)
  and lands on the still image, as the fallback works today.

## Goals

- The room is large: on a wide desktop it fills the screen height and most of its width.
- The wait for the scene becomes the name build instead of nothing.
- See my work, Download CV and Get in touch are on the first screen once the name lands on desktop
  and tablets; on phones just after the room.

## Non-goals

- No changes to the sections below the hero, the other pages, the content of the 11 hotspots or
  the cards' wording.
- No new scroll-driven storytelling beyond the slide-over and dim.
- No backend changes.

- The build must not stall: the 3D scene's heavy moments (loading its script and building the room,
  linking its shaders on the first frame) happen before the build starts or while the finished name
  is still, never during the build or the flight.

## Anti-goals (a failure even if it works)

- The name looks broken, gappy or warped at any stage, on desktop or phone.
- The name build floats over the page content (it happened in the prototype when it was a fixed
  layer; the lock and keeping the layer inside the hero both prevent it).
- A visitor is held more than 10 s, or can't scroll after the name lands.
- Hero text covers clickable objects in the room.
- The page jumps or the sections below move when the scene starts (an existing phone test covers this).

## Constraints

- Static SSR stays the default; the hero gains no Blazor render mode. All of this is
  `site.js` / `wwwroot/js/workshop/` plus `Index.razor` markup and Tailwind.
- three.js stays at 0.160.0 from `wwwroot/lib/three/`. Any new script (d3-delaunay or a small
  triangulator) is served from `wwwroot/lib/`, not a CDN, and only on the homepage.
- Colours from the Tailwind tokens (`ink`, `line`, `cy`, …); shared classes where they fit.
- Text keeps WCAG AA contrast (`test_contrast.py` covers the homepage).
- The scene's own entrance (the objects popping in, monitors booting, the lamp flicker, `INTRO_DUR`)
  should play as the room grows in after the landing, not unseen behind the name build.
- The still image and its pins come from the live scene: after the camera framing changes, re-run
  `tools/render-workshop-still.py` and update `PinX`/`PinY` in `HomeContent`.
- No em dashes in page text (a host test checks every page).

## Left to the builder

- How the name build is split from the scene code, and whether it lives in `site.js` or its own module.
- Exact camera poses for the new framing and for each object while its card is open (`hotspots.js`).
- How the hero's text and the scene's text column share space between 900px and very wide screens.
- Test design: browser tests for the lock, the landing, refresh-to-top, anchors skipping the intro,
  reduced motion, the hold while the scene loads and the 10 s limit, header clicks ending the intro, reload vs Back/Forward; updates to existing workshop and fallback tests where the
  framing or button changes break them.
