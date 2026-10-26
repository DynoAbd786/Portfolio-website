// The workshop's HTML overlay: the hover tag, the project card, the hint bar and a keyboard
// tour of every object. Styles live in css/app.css (.ws-*).
const el = (tag, cls, text) => { const e = document.createElement(tag); if (cls) e.className = cls; if (text != null) e.textContent = text; return e; };
const kbd = (k, label) => { const s = el('span', 'ws-hint-k'); s.append(el('kbd', null, k), label); return s; };

export function createUI({ root, section, data, onOpen, onClose }) {
  const order = data.map((d) => d.key), byKey = Object.fromEntries(data.map((d) => [d.key, d]));
  const heroCopy = section.querySelector('.hero-copy');
  const wide = matchMedia('(min-width: 900px)');
  const state = { focusKey: null, explored: new Set() };
  const disposers = [];
  const on = (target, type, fn, opts) => { target.addEventListener(type, fn, opts); disposers.push(() => target.removeEventListener(type, fn, opts)); };

  // ---------------------------------------------------------------- hover tag
  const tag = el('div', 'ws-tag'); tag.setAttribute('aria-hidden', 'true');
  const tagIn = el('div', 'ws-tag-in'), tagName = el('b'), tagSub = el('span'), tagCta = el('em');
  tagIn.append(tagName, tagSub, tagCta); tag.append(tagIn); root.append(tag);
  let tagW = 0;

  // ---------------------------------------------------------------- hint bar
  const hint = el('div', 'ws-hint'); hint.setAttribute('aria-hidden', 'true');
  const found = el('b', null, '0');
  const foundWrap = el('span', 'ws-hint-found'); foundWrap.append('explored ', found, `/${order.length}`);
  hint.append(kbd('move', 'aim the lamp'), kbd('hover', 'inspect'), kbd('click', 'zoom in'), el('span', 'ws-hint-tap', 'tap an object'), foundWrap);
  root.append(hint);

  // ---------------------------------------------------------------- keyboard / screen-reader path into every object
  const tour = el('nav', 'ws-tour'); tour.setAttribute('aria-label', 'Explore the workshop');
  tour.append(el('span', null, 'explore:'));
  for (const k of order) { const b = el('button', null, byKey[k].name); b.type = 'button'; on(b, 'click', () => openCard(k)); tour.append(b); }
  root.append(tour);

  // ---------------------------------------------------------------- card
  const card = el('aside', 'ws-card'); card.setAttribute('role', 'dialog'); card.setAttribute('aria-labelledby', 'ws-card-title'); card.inert = true;
  const back = el('button', 'ws-card-back'); back.type = 'button'; back.append('← Back to the workshop ', el('kbd', null, 'esc'));
  const prev = el('button', null, '‹'); prev.type = 'button'; prev.setAttribute('aria-label', 'Previous object');
  const next = el('button', null, '›'); next.type = 'button'; next.setAttribute('aria-label', 'Next object');
  const step = el('span'); step.setAttribute('aria-live', 'polite');
  const stepper = el('div', 'ws-card-stepper'); stepper.append(prev, step, next);
  const row = el('div', 'ws-card-row'); row.append(back, stepper);
  const kicker = el('p', 'ws-card-kicker'), title = el('h2', 'ws-card-title'), desc = el('p', 'ws-card-desc');
  title.id = 'ws-card-title'; title.tabIndex = -1;
  const points = el('ul', 'ws-card-points'), chips = el('div', 'ws-card-chips');
  const ctaSlot = el('div');
  card.append(row, kicker, title, desc, points, chips, ctaSlot);
  section.append(card);
  let returnFocus = null;

  function openCard(k) {
    const d = byKey[k], was = state.focusKey;
    state.focusKey = k; state.explored.add(k); found.textContent = state.explored.size;
    kicker.textContent = '// ' + d.kicker; title.textContent = d.title; desc.textContent = d.desc;
    points.replaceChildren(...d.points.map((t) => el('li', null, t)));
    chips.replaceChildren(...d.chips.map((t) => el('span', null, t)));
    step.textContent = `${order.indexOf(k) + 1}/${order.length}`;
    let cta;
    if (d.action === 'assistant') { cta = el('button', 'btn-primary ws-card-cta', d.cta); cta.type = 'button'; cta.addEventListener('click', () => window.mkChat?.toggle()); }
    else { cta = el('a', 'btn-primary ws-card-cta', d.cta); cta.href = d.href; }
    ctaSlot.replaceChildren(cta);
    if (!was) { const ae = document.activeElement; returnFocus = ae && ae !== document.body && !card.contains(ae) ? ae : null; }
    card.inert = false; card.classList.add('open'); section.classList.add('ws-focused');
    if (heroCopy && wide.matches) heroCopy.inert = true;
    title.focus({ preventScroll: true });
    // narrow screens put the card below the room, so bring it into view
    if (!wide.matches) card.scrollIntoView({ block: 'nearest', behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth' });
    onOpen(k);
  }
  function closeCard(keepFocus) {
    if (!state.focusKey) return;
    state.focusKey = null; card.classList.remove('open'); card.inert = true; section.classList.remove('ws-focused');
    if (heroCopy) heroCopy.inert = false;
    if (!keepFocus && returnFocus && document.contains(returnFocus)) returnFocus.focus({ preventScroll: true });
    returnFocus = null;
    onClose();
  }
  const stepBy = (n) => openCard(order[(order.indexOf(state.focusKey) + order.length + n) % order.length]);
  on(back, 'click', () => closeCard());
  on(prev, 'click', () => stepBy(-1));
  on(next, 'click', () => stepBy(1));
  on(window, 'keydown', (e) => {
    if (!state.focusKey || e.defaultPrevented) return;
    // keys typed into the chat, the contact form or any other field are not for the workshop
    if (e.target.closest?.('input, textarea, select, [contenteditable]')) return;
    if (e.key === 'Escape') closeCard();
    else if (e.key === 'ArrowRight') stepBy(1);
    else if (e.key === 'ArrowLeft') stepBy(-1);
  });

  return {
    state, openCard, closeCard,
    get cardRect() { return state.focusKey && wide.matches ? card.getBoundingClientRect() : null; },
    // show the tag for a hotspot (null hides it)
    setTag(k, cta) {
      if (k) { tagName.textContent = byKey[k].name; tagSub.textContent = byKey[k].sub; tagCta.textContent = cta ?? ''; tagCta.hidden = !cta; tagW = 0; }
      tag.classList.toggle('on', !!k);
    },
    // demo: the intro's nudge before anyone has hovered; say: the robot's speech
    setTagStyle(demo, say) { tag.classList.toggle('demo', demo); tag.classList.toggle('say', say); },
    setTagSub(text) { if (tagSub.textContent !== text) { tagSub.textContent = text; tagW = 0; } },
    // place the tag at stage pixel (x, y), kept inside the stage width
    placeTag(x, y, width) {
      if (!tagW) tagW = tagIn.offsetWidth;
      const cx = Math.min(Math.max(x, tagW / 2 + 10), Math.max(tagW / 2 + 10, width - tagW / 2 - 10));
      tag.style.setProperty('--ax', Math.min(Math.max(x - cx, -tagW / 2 + 18), tagW / 2 - 18).toFixed(1) + 'px');
      tag.style.transform = `translate3d(${cx.toFixed(1)}px, ${y.toFixed(1)}px, 0)`;
    },
    dispose() {
      disposers.forEach((d) => d());
      if (heroCopy) heroCopy.inert = false;
      section.classList.remove('ws-focused');
      tag.remove(); hint.remove(); tour.remove(); card.remove();
    },
  };
}
