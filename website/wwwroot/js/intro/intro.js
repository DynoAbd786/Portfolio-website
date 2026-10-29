// The homepage intro: the name build (name-build.js) is the loading screen for the 3D workshop.
// site.js starts it when App.razor's head script has marked the page .intro-pending.
//
// While it runs, scrolling waits: wheel, touch and scroll keys do nothing. The name lands on the
// page's <h1> once the build has finished and the scene is ready (or has given up). 10 s after the
// page opened (or first became visible) nothing waits any longer: a build that hasn't started is
// skipped, one that is playing finishes and lands without waiting for the scene. A click on any link
// or button, Tab, or leaving the page ends it at once.

const SCROLL_KEYS = new Set(['ArrowDown', 'ArrowUp', 'PageDown', 'PageUp', ' ', 'End', 'Home']);
const LIMIT_MS = 10000;
const SETTLE_MS = 3500;
const FLIGHT_MS = 1100;

/**
 * @param hero        #hero
 * @param deps        () => Promise: Delaunator and the font (rejects if either fails)
 * @param loadBuild   () => Promise<{ createNameBuild }>
 * @param settled     Promise: the scene is live or won't run
 * @param startScene  () => void: the scene's held entrance starts
 * @param firstFrame  () => void: the held scene may draw (its first frame holds the main thread, so it
 *                    waits until the build has finished drawing)
 * @param sceneLoaded Promise: the scene's script has loaded and built the room (which also holds the
 *                    main thread), so the build starts after it, or after 1.5 s at most
 * @param sceneEntered () => Promise: the room's own entrance has played out; scrolling waits for it
 *                    (3.5 s at most after the name lands)
 * @returns { done: Promise (resolves once landed), finish(): lands at once }
 */
export function runIntro({ hero, deps, loadBuild, settled, startScene, firstFrame = () => {}, sceneLoaded = Promise.resolve(), sceneEntered = () => Promise.resolve() }) {
  const html = document.documentElement;
  const canvas = hero.querySelector('.hero-namefx'), loading = hero.querySelector('.hero-loading'), h1 = hero.querySelector('.hero-head h1');
  let ended = false, raf = 0, resolveDone, flying = null;
  const done = new Promise((resolve) => { resolveDone = resolve; });
  const timers = [], offs = [];
  const on = (target, type, fn, opts) => { target.addEventListener(type, fn, opts); offs.push(() => target.removeEventListener(type, fn, opts)); };

  // ---------------------------------------------------------------- the scroll lock
  const stop = (e) => e.preventDefault();
  on(window, 'wheel', stop, { passive: false });
  on(window, 'touchmove', stop, { passive: false });
  on(window, 'keydown', (e) => {
    if (e.key === 'Tab') { finish(); return; } // a keyboard user moving on: don't let focus scroll under the build
    if (SCROLL_KEYS.has(e.key) && !e.target.closest?.('input, textarea, select, [contenteditable]')) e.preventDefault();
  });
  on(window, 'scroll', () => { if (scrollY) scrollTo(0, 0); }); // a dragged scrollbar
  // a deliberate click (the header's links, Ask AI, the CV) wins: land first, then let it go where it points
  on(document, 'click', (e) => { if (e.target.closest?.('a, button')) finish(); }, true);

  function stopBuild() { timers.forEach(clearTimeout); cancelAnimationFrame(raf); }
  function release() { stopBuild(); offs.forEach((off) => off()); offs.length = 0; }

  // ---------------------------------------------------------------- landing
  // where the <h1>'s second line is on the page: its text's left edge and width, and its baseline
  function h1Line() {
    const span = h1.querySelectorAll('span')[1] || h1;
    const range = document.createRange(); range.selectNodeContents(span.firstChild || span);
    const r = range.getBoundingClientRect();
    const mark = document.createElement('span'); mark.style.cssText = 'display:inline-block;width:0;height:0;vertical-align:baseline';
    span.append(mark); const baseline = mark.getBoundingClientRect().bottom; mark.remove();
    return { left: r.left, width: r.width, baseline };
  }

  // Scrolling stays locked until the name is in its place and the room has settled (.intro-settling in
  // between). finish() snaps straight to the end.
  function land(fly) {
    if (ended) { flying?.(); return; }
    ended = true;
    stopBuild();
    startScene();
    if (!fly || !h1) {
      release();
      html.classList.remove('intro-pending', 'intro-flying');
      canvas.style.transform = '';
      canvas.width = canvas.height = 0;
      resolveDone();
      return;
    }
    // fly the built name onto the <h1>'s text, measured now (so a resize mid-intro is fine): the same
    // width and left edge, and the second line on its baseline
    const c = canvas.getBoundingClientRect(), line = h1Line();
    const s = line.width / fly.lineWidths[1];
    const tx = line.left - c.left - fly.lineLeft * s, ty = line.baseline - c.top - fly.baselines[1] * s;
    canvas.dataset.landing = JSON.stringify({ s, tx, ty, x0: fly.lineLeft, widths: fly.lineWidths, b2: fly.baselines[1], left: c.left, top: c.top });
    html.classList.add('intro-flying', 'intro-landed');
    html.classList.remove('intro-pending');
    canvas.style.transition = `transform ${FLIGHT_MS}ms cubic-bezier(.7, 0, .2, 1)`;
    canvas.style.transform = `translate(${tx}px, ${ty}px) scale(${s})`;
    let arrived, settledT;
    const unlock = () => {
      flying = null; clearTimeout(arrived); clearTimeout(settledT);
      release();
      html.classList.remove('intro-flying', 'intro-settling');
      canvas.style.transition = canvas.style.transform = '';
      canvas.width = canvas.height = 0; // free its bitmap
      resolveDone();
      setTimeout(() => html.classList.remove('intro-landed'), 2500);
    };
    flying = unlock;
    const entered = Promise.race([sceneEntered(), new Promise((resolve) => { settledT = setTimeout(resolve, SETTLE_MS); })]);
    arrived = setTimeout(async () => {
      if (!flying) return;
      // the name is in its place: hide the canvas, but keep scrolling locked while the room settles
      html.classList.add('intro-settling'); html.classList.remove('intro-flying');
      canvas.style.transition = canvas.style.transform = '';
      canvas.width = canvas.height = 0;
      await entered;
      flying?.();
    }, FLIGHT_MS + 20);
  }

  function finish() { land(null); }

  // ---------------------------------------------------------------- the build
  (async () => {
    // the 10 s count from the page opening, or from the tab first being shown if it opened hidden
    let from = 0;
    if (document.hidden) {
      await new Promise((resolve) => on(document, 'visibilitychange', () => { if (!document.hidden) resolve(); }));
      from = performance.now();
    }
    if (ended) return;
    let building = false;
    const limit = new Promise((resolve) => timers.push(setTimeout(() => {
      if (!building) finish(); // still downloading: skip the build
      resolve();
    }, Math.max(0, from + LIMIT_MS - performance.now()))));
    let build;
    try {
      const [, { createNameBuild }] = await Promise.all([deps(), loadBuild(),
        Promise.race([sceneLoaded, new Promise((resolve) => setTimeout(resolve, 1500))])]);
      await new Promise((resolve) => requestAnimationFrame(resolve)); // let that work's frame finish
      if (ended) return;
      building = true;
      const stacked = matchMedia('(max-width: 899px), (max-height: 499px)').matches;
      const w = hero.clientWidth, h = Math.min(hero.clientHeight, innerHeight - hero.getBoundingClientRect().top);
      const dpr = Math.min(devicePixelRatio || 1, 2);
      canvas.width = Math.round(w * dpr); canvas.height = Math.round(h * dpr);
      canvas.style.width = w + 'px'; canvas.style.height = h + 'px';
      build = createNameBuild(canvas, {
        lines: ['Muhammad', 'Kashif-Khan'], fontFamily: 'Inter Build', centreY: h * .45, phone: stacked, dpr,
        // a short landscape screen limits the name by its height
        fontSize: Math.round(stacked ? Math.min(w * .15, h * .2) : Math.min(170, w * .085)),
      });
      // the label sits under the name, never on it
      if (loading) { loading.style.top = Math.round(Math.max(h * .74, build.textBox.top + build.textBox.height + 28)) + 'px'; loading.style.bottom = 'auto'; }
    } catch (e) {
      console.warn('[intro]', e && e.message);
      finish();
      return;
    }
    const t0 = performance.now();
    performance.mark('mk-build-start');
    await new Promise((resolve) => {
      const step = (now) => {
        if (ended) return resolve();
        const t = (now - t0) / 1000;
        try { build.draw(Math.min(t, build.duration)); } catch (e) { console.warn('[intro]', e && e.message); finish(); return resolve(); }
        if (t >= build.duration) resolve(); else raf = requestAnimationFrame(step);
      };
      raf = requestAnimationFrame(step);
    });
    performance.mark('mk-build-end');
    if (ended) return;
    firstFrame();
    await Promise.race([settled, limit]);
    // let the scene's first frames (which link its shaders and hold the main thread) finish while the
    // name is still, so they don't stall the flight
    for (let i = 0; i < 3 && !ended; i++) await new Promise((resolve) => { raf = requestAnimationFrame(resolve); });
    land(build);
  })();

  return { done, finish };
}
