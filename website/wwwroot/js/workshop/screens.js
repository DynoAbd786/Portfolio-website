// The two live monitors on the desk, drawn into canvases: Synergy's architecture graph and a
// HireForge terminal session. Both are drawn at SS x the logical size for crisp close-ups.
import { clamp, smooth, MONO } from './kit.js';

export const SW = 528, SH = 320, SS = 1.5;

// CRT power-on: a bright line opens into the picture
export function crt(ctx, k) {
  if (k >= 1) return;
  if (k <= 0) { ctx.fillStyle = '#000'; ctx.fillRect(0, 0, SW, SH); return; }
  const lineK = clamp(k / .3, 0, 1), e = smooth(clamp((k - .25) / .75, 0, 1)), hh = SH / 2 * e;
  ctx.fillStyle = '#000'; ctx.fillRect(0, 0, SW, SH / 2 - hh); ctx.fillRect(0, SH / 2 + hh, SW, SH / 2 - hh + 1);
  ctx.fillStyle = `rgba(200,245,255,${(1 - k) * .55})`; ctx.fillRect(0, SH / 2 - hh, SW, hh * 2);
  const w = SW * smooth(lineK); ctx.fillStyle = `rgba(235,252,255,${1 - e * .9})`; ctx.fillRect(SW / 2 - w / 2, SH / 2 - hh - 1.5, w, 3);
  if (hh > 2) ctx.fillRect(SW / 2 - w / 2, SH / 2 + hh - 1.5, w, 3);
}

// Synergy: a force-laid-out graph of its own packages (models → persistence → analyzer →
// visualizer, mcp and backend; the frontend talks to the backend), fed by the eight languages
// it parses, with an MCP query cycling through it. The cursor (in screen pixels) pushes nodes
// away and highlights the nearest one.
export function createSynergyScreen(R) {
  const TOP = 28, BOT = 34;
  const LANG = ['.py', '.cs', '.java', '.go', '.cpp', '.rs', '.js', '.ts'];
  // [label, kind, x, y]; kind: 0 core, 2 agent-facing, 3 user-facing
  const MOD = [['analyzer', 0, 196, 162], ['persistence', 0, 250, 256], ['models', 0, 352, 268], ['visualizer', 3, 300, 82],
    ['mcp', 2, 380, 150], ['backend', 3, 300, 190], ['frontend', 3, 410, 226]];
  const nodes = [];
  LANG.forEach((l, i) => { const t = i / 7; nodes.push({ l, k: 1, ax: 48 + Math.sin(t * Math.PI) * 30, ay: 56 + t * 222 }); });
  MOD.forEach(([l, k, ax, ay]) => nodes.push({ l, k, ax, ay }));
  nodes.forEach((n) => { n.x = n.ax + (R() - .5) * 20; n.y = n.ay + (R() - .5) * 20; n.vx = 0; n.vy = 0; });
  const A = 8, PER = 9, MODELS = 10, VIS = 11, MCP = 12, BACK = 13, FRONT = 14;
  const E = [...LANG.map((_, i) => [i, A]), [A, PER], [A, MODELS], [PER, MODELS], [VIS, A], [MCP, A], [BACK, A], [FRONT, BACK]];
  const ELAB = ['parsed by', 'stores', 'uses', 'uses', 'reads', 'queries', 'queries', 'calls'];
  const edgeLabel = (ei) => (ei < LANG.length ? ELAB[0] : ELAB[ei - LANG.length + 1]);
  const nb = nodes.map(() => new Set()); E.forEach(([a, b]) => { nb[a].add(b); nb[b].add(a); });
  const COL = ['#22d3ee', '#a78bfa', '#34d399', '#9fb3d1'];
  // results are true of the graph on screen
  const QS = [{ q: 'get_context_bundle(depth=2)', n: A, r: '7 packages' }, { q: 'impact("synergy_models")', n: MODELS, r: '6 dependents' },
    { q: 'path(mcp, models)', n: MCP, r: '2 hops' }, { q: 'compare_refs("main", "HEAD")', n: PER, r: 'structural diff' }];
  const QT = 4.4;
  let cursor = null, hover = -1, auto = -1;

  function step(T) {
    const qi = Math.floor(T / QT) % QS.length, qt = T % QT; auto = qt > 1.15 ? QS[qi].n : -1;
    for (let it = 0; it < 2; it++) {
      for (let i = 0; i < nodes.length; i++) {
        const a = nodes[i];
        let fx = (a.ax + Math.sin(T * .7 + i) * 5 - a.x) * .025, fy = (a.ay + Math.cos(T * .6 + i * 1.3) * 4 - a.y) * .025;
        for (let j = 0; j < nodes.length; j++) if (j !== i) {
          const b = nodes[j]; const dx = a.x - b.x, dy = a.y - b.y, d2 = dx * dx + dy * dy + 30; const f = 1500 / (d2 * Math.sqrt(d2));
          fx += dx * f; fy += dy * f;
        }
        if (cursor) { const dx = a.x - cursor.x, dy = a.y - cursor.y, d2 = dx * dx + dy * dy + 60; if (d2 < 9000) { const f = 1000 / d2; fx += dx / Math.sqrt(d2) * f; fy += dy / Math.sqrt(d2) * f; } }
        a.vx = (a.vx + fx) * .8; a.vy = (a.vy + fy) * .8;
      }
      for (const [i, j] of E) {
        const a = nodes[i], b = nodes[j]; const dx = b.x - a.x, dy = b.y - a.y, d = Math.hypot(dx, dy) || 1; const f = (d - 64) * .003;
        a.vx += dx / d * f; a.vy += dy / d * f; b.vx -= dx / d * f; b.vy -= dy / d * f;
      }
      for (const a of nodes) { a.x = clamp(a.x + a.vx, 16, SW - 52); a.y = clamp(a.y + a.vy, TOP + 16, SH - BOT - 14); }
    }
    hover = -1;
    if (cursor) { let best = 34 * 34; nodes.forEach((n, i) => { const d = (n.x - cursor.x) ** 2 + (n.y - cursor.y) ** 2; if (d < best) { best = d; hover = i; } }); }
  }

  function draw(ctx, T) {
    const sel = hover >= 0 ? hover : auto;
    ctx.fillStyle = '#070b16'; ctx.fillRect(0, 0, SW, SH);
    const vg = ctx.createRadialGradient(300, 170, 20, 300, 170, 320); vg.addColorStop(0, 'rgba(34,211,238,.07)'); vg.addColorStop(1, 'rgba(34,211,238,0)');
    ctx.fillStyle = vg; ctx.fillRect(0, 0, SW, SH);
    ctx.fillStyle = 'rgba(130,160,210,.14)';
    for (let x = 12; x < SW; x += 22) for (let y = TOP + 10; y < SH - BOT; y += 22) ctx.fillRect(x, y, 1.6, 1.6);
    ctx.lineCap = 'round';
    E.forEach(([i, j], ei) => {
      const a = nodes[i], b = nodes[j]; const hl = sel >= 0 && (i === sel || j === sel);
      ctx.strokeStyle = hl ? 'rgba(34,211,238,.95)' : 'rgba(150,172,220,.32)'; ctx.lineWidth = hl ? 2.4 : 1.3;
      ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(b.x, b.y); ctx.stroke();
      const f = (T * .55 + ei * .137) % 1; const px = a.x + (b.x - a.x) * f, py = a.y + (b.y - a.y) * f;
      ctx.fillStyle = hl ? '#ffffff' : COL[nodes[j].k]; ctx.beginPath(); ctx.arc(px, py, hl ? 2.8 : 2.1, 0, 7); ctx.fill();
      if (hl && a.k !== 1) {
        const mx = (a.x + b.x) / 2, my = (a.y + b.y) / 2, t = edgeLabel(ei);
        ctx.font = `600 9.5px ${MONO}`; const w = ctx.measureText(t).width + 8;
        ctx.fillStyle = 'rgba(6,10,20,.92)'; ctx.fillRect(mx - w / 2, my - 7, w, 13); ctx.fillStyle = '#7dd3fc'; ctx.fillText(t, mx - w / 2 + 4, my + 3);
      }
    });
    nodes.forEach((n, i) => {
      const hl = i === sel, near = sel >= 0 && nb[sel].has(i), dim = sel >= 0 && !hl && !near;
      const c = COL[n.k]; const r = n.k === 1 ? 4.6 : 6.4;
      const al = dim ? .4 : 1;
      ctx.globalAlpha = al * .22; ctx.fillStyle = c; ctx.beginPath(); ctx.arc(n.x, n.y, r * (hl ? 3.2 : 2.3), 0, 7); ctx.fill();
      ctx.globalAlpha = al;
      ctx.fillStyle = '#0a0f1c'; ctx.beginPath(); ctx.arc(n.x, n.y, r + 1.5, 0, 7); ctx.fill();
      ctx.strokeStyle = c; ctx.lineWidth = hl ? 2.6 : 2; ctx.beginPath(); ctx.arc(n.x, n.y, r, 0, 7); ctx.stroke();
      ctx.fillStyle = c; ctx.beginPath(); ctx.arc(n.x, n.y, r * .5, 0, 7); ctx.fill();
      ctx.font = `${hl ? 700 : 500} ${hl ? 12.5 : 11}px ${MONO}`; ctx.fillStyle = hl ? '#ffffff' : '#b3c0d6'; ctx.fillText(n.l, n.x + r + 5, n.y + 4);
      ctx.globalAlpha = 1;
    });
    if (sel >= 0) {
      const n = nodes[sel]; const txt = n.k === 1 ? `${n.l} · tree-sitter grammar` : `synergy_${n.l} · ${nb[sel].size} links`;
      ctx.font = `600 11px ${MONO}`; const w = ctx.measureText(txt).width + 16; const bx = clamp(n.x - w / 2, 6, SW - w - 6), by = n.y - 38 < TOP + 4 ? n.y + 16 : n.y - 38;
      ctx.fillStyle = 'rgba(8,12,22,.95)'; ctx.strokeStyle = 'rgba(34,211,238,.75)'; ctx.lineWidth = 1;
      ctx.beginPath(); ctx.roundRect(bx, by, w, 22, 6); ctx.fill(); ctx.stroke(); ctx.fillStyle = '#e8f9ff'; ctx.fillText(txt, bx + 8, by + 15);
    }
    if (cursor) { ctx.strokeStyle = 'rgba(34,211,238,.55)'; ctx.lineWidth = 1; ctx.beginPath(); ctx.arc(cursor.x, cursor.y, 14, 0, 7); ctx.stroke(); }
    // window chrome: title bar and the MCP query bar
    ctx.fillStyle = '#0d1324'; ctx.fillRect(0, 0, SW, TOP);
    ['#f87171', '#fbbf24', '#34d399'].forEach((c, i) => { ctx.fillStyle = c; ctx.beginPath(); ctx.arc(14 + i * 13, 14, 4, 0, 7); ctx.fill(); });
    ctx.font = `700 12px ${MONO}`; ctx.fillStyle = '#e4ebf7'; ctx.fillText('synergy', 60, 18.5);
    ctx.font = `500 11px ${MONO}`; ctx.fillStyle = '#6c7891'; ctx.fillText('· architecture graph', 122, 18.5);
    ctx.fillStyle = '#0a1020'; ctx.fillRect(0, SH - BOT, SW, BOT); ctx.fillStyle = 'rgba(34,211,238,.35)'; ctx.fillRect(0, SH - BOT, SW, 1);
    const qi = Math.floor(T / QT) % QS.length, qt = T % QT, Q = QS[qi]; const n = Math.min(Q.q.length, Math.floor(qt * 24));
    ctx.font = `700 13px ${MONO}`; ctx.fillStyle = '#34d399'; ctx.fillText('mcp ›', 12, SH - 12);
    ctx.font = `500 13px ${MONO}`; ctx.fillStyle = '#e6edf7'; ctx.fillText(Q.q.slice(0, n), 62, SH - 12);
    if (qt > 1.15) { ctx.textAlign = 'right'; ctx.fillStyle = '#22d3ee'; ctx.font = `700 13px ${MONO}`; ctx.fillText('→ ' + Q.r, SW - 12, SH - 12); ctx.textAlign = 'left'; }
    else if (Math.floor(T * 2.4) % 2 === 0) { ctx.fillStyle = '#22d3ee'; ctx.fillRect(62 + ctx.measureText(Q.q.slice(0, n)).width + 2, SH - 23, 7, 14); }
  }

  return { step, draw, set cursor(v) { cursor = v; }, get cursor() { return cursor; } };
}

// HireForge: an assessment session, end to end. The idle loop is short; zoomed in, the longer
// session plays and scrolls as it goes. Illustrative: no real candidate, no real scores.
export function createTerminal() {
  const base = [
    { c: 1, t: 'hireforge session create --scenario checkout-api' },
    { t: '  ▸ generator agent: building the codebase', col: '#93a0b8' },
    { t: '  ✓ codebase ready · 1 seeded defect · tests run', col: '#34d399' },
    { c: 1, t: 'hireforge candidate start --demo' },
    { t: '  ▸ candidate working · diff + action log recorded', col: '#93a0b8' },
    { t: '  ✓ session finished · snapshot saved', col: '#34d399', q: 1 },
    { c: 1, t: 'hireforge review' },
    { t: '  ▸ reviewer agent · independent of the generator', col: '#93a0b8' },
    { t: '  ✓ seeded defect found and fixed', col: '#34d399', q: 1 },
    { t: '  ✓ scorecard ready  ↗', col: '#22d3ee', b: 1 },
  ];
  const more = [
    { c: 1, t: 'hireforge scorecard --summary' },
    { t: '  diagnosis       found the root cause', col: '#e6edf7', q: 1 },
    { t: '  fix             minimal, covered by a test', col: '#e6edf7', q: 1 },
    { t: '  communication   clear commit message', col: '#e6edf7', q: 1 },
    { t: '  ⚑ reviewer note: check the retry path', col: '#fbbf24' },
    { c: 1, t: 'hireforge share --team hiring' },
    { t: '  ✓ scorecard shared with the hiring team', col: '#34d399', b: 1 },
  ];
  return createSession(base, more);
}

function createSession(base, more) {
  const CPS = 34;
  const time = (list) => { let t = .4; for (const l of list) { l.s = t; if (l.c) t += l.t.length / CPS + .45; else t += l.q ? .32 : .5; } return t; };
  const doneA = time(base), totalA = doneA + 3.4;
  const full = base.map((l) => ({ ...l })).concat(more.map((l) => ({ ...l }))); const doneB = time(full);
  const scoreAt = full[base.length - 1].s;
  function draw(ctx, T, fin, ext) {
    const lines = ext ? full : base, done = ext ? doneB : doneA;
    const tt = fin ? 999 : (ext ? Math.min(T, doneB + 60) : T % totalA);
    ctx.fillStyle = '#080b13'; ctx.fillRect(0, 0, SW, SH);
    ctx.font = `500 13px ${MONO}`;
    // visible lines and the scroll needed to keep the newest one on screen
    let n = 0; while (n < lines.length && lines[n].s <= tt) n++;
    const LH = 22, Y0 = 52, YMAX = SH - 34;
    const lastY = Y0 + Math.max(0, n - 1) * LH + (n && !lines[n - 1].c ? LH : 0);
    const scroll = Math.max(0, lastY - YMAX);
    ctx.save(); ctx.beginPath(); ctx.rect(0, 30, SW, SH - 50); ctx.clip(); ctx.translate(0, -scroll);
    let y = Y0, cx = 16, cy = Y0;
    for (let i = 0; i < n; i++) {
      const l = lines[i];
      if (l.c) {
        const k = Math.min(l.t.length, Math.floor((tt - l.s) * CPS));
        ctx.fillStyle = '#22d3ee'; ctx.fillText('❯', 14, y); ctx.fillStyle = '#eef1f7'; const s = l.t.slice(0, k); ctx.fillText(s, 30, y);
        cx = 30 + ctx.measureText(s).width + 2; cy = y;
      } else { ctx.fillStyle = l.col; ctx.font = `${l.b ? 700 : 500} 13px ${MONO}`; ctx.fillText(l.t, 14, y); ctx.font = `500 13px ${MONO}`; cx = 30; cy = y + LH; }
      y += LH;
    }
    if (Math.floor(T * 2.2) % 2 === 0 || fin) { ctx.fillStyle = '#22d3ee'; ctx.fillRect(cx, cy - 11, 7, 14); }
    ctx.restore();
    ctx.fillStyle = '#111727'; ctx.fillRect(0, 0, SW, 28);
    ['#f87171', '#fbbf24', '#34d399'].forEach((c, i) => { ctx.fillStyle = c; ctx.beginPath(); ctx.arc(14 + i * 13, 14, 4, 0, 7); ctx.fill(); });
    ctx.font = `700 12px ${MONO}`; ctx.fillStyle = '#e4ebf7'; ctx.fillText('hireforge', 60, 18.5); ctx.font = `500 11px ${MONO}`; ctx.fillStyle = '#6c7891'; ctx.fillText('— session://demo', 134, 18.5);
    ctx.textAlign = 'right'; ctx.fillStyle = '#a78bfa'; ctx.fillText('zsh', SW - 12, 18.5); ctx.textAlign = 'left';
    // scorecard ring: fills as the review completes
    const sk = fin ? 1 : clamp((tt - (ext ? scoreAt + .5 : doneA) + .3) / .8, 0, 1);
    if (sk > 0) {
      const cxr = SW - 58, cyr = 82;
      ctx.fillStyle = 'rgba(8,11,19,.85)'; ctx.beginPath(); ctx.arc(cxr, cyr, 46, 0, 7); ctx.fill();
      ctx.lineWidth = 7; ctx.strokeStyle = 'rgba(167,139,250,.18)'; ctx.beginPath(); ctx.arc(cxr, cyr, 34, 0, 7); ctx.stroke();
      ctx.strokeStyle = '#22d3ee'; ctx.lineCap = 'round'; ctx.beginPath(); ctx.arc(cxr, cyr, 34, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * smooth(sk)); ctx.stroke(); ctx.lineCap = 'butt';
      ctx.textAlign = 'center'; ctx.fillStyle = '#ffffff'; ctx.font = `700 22px ${MONO}`; ctx.globalAlpha = smooth(sk); ctx.fillText('✓', cxr, cyr + 8); ctx.globalAlpha = 1;
      ctx.font = `500 9.5px ${MONO}`; ctx.fillStyle = '#93a0b8'; ctx.fillText('scorecard', cxr, cyr + 52); ctx.textAlign = 'left';
    }
    const p = clamp(tt / done, 0, 1);
    ctx.fillStyle = '#121725'; ctx.fillRect(14, SH - 14, SW - 28, 3); ctx.fillStyle = '#a78bfa'; ctx.fillRect(14, SH - 14, (SW - 28) * p, 3);
  }
  return { draw };
}
