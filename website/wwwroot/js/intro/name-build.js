// The homepage's name build: the name is "reconstructed" the way the MEng project reconstructs a
// scene. A point cloud settles into the letters, a scan line wires it into a triangle mesh, the mesh
// fills with lit facets, then polishes into plain text. js/intro/intro.js plays it while the 3D
// workshop loads.
//
// createNameBuild precomputes everything; draw(t) only paints the moment t seconds in, so the same
// seed always draws the same frame.

const SCAN0 = 1.05, SCAN = .9, FILL0 = 1.75, FILL = .6, POLISH = 2.75, DURATION = 3.3;
const clamp = (v) => (v < 0 ? 0 : v > 1 ? 1 : v);
const ease = (k) => 1 - Math.pow(1 - k, 3);

// a small seeded generator (mulberry32)
function prng(seed) {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/**
 * @param canvas a canvas whose width/height are its CSS size times the pixel ratio
 * @param opts { lines: [string, string], fontFamily, fontSize (CSS px), centreY (CSS px), phone, seed,
 *               Delaunator, dpr (defaults to canvas.width / its CSS width) }
 */
export function createNameBuild(canvas, { lines, fontFamily, fontSize, centreY, phone = false, seed = Date.now(), Delaunator = window.Delaunator, dpr } = {}) {
  dpr = dpr || canvas.width / (parseFloat(canvas.style.width) || canvas.clientWidth || canvas.width);
  const W = canvas.width / dpr, H = canvas.height / dpr;
  const ctx = canvas.getContext('2d');
  const rnd = prng(seed);
  // the page's <h1>: tracking -.045em, line height .93, lines left-aligned (so the built name can fly onto it)
  const fs = fontSize, font = `800 ${fs}px "${fontFamily}"`, spacing = `${-.045 * fs}px`, lead = .93 * fs;

  // where the two lines sit: a left-aligned block centred on (W / 2, centreY)
  const m = document.createElement('canvas'); m.width = Math.ceil(W); m.height = Math.ceil(H);
  const mc = m.getContext('2d', { willReadFrequently: true });
  mc.font = font; mc.letterSpacing = spacing;
  const m1 = mc.measureText(lines[0]), m2 = mc.measureText(lines[1]);
  const asc = m1.actualBoundingBoxAscent, desc = m2.actualBoundingBoxDescent;
  const w1 = m1.width, w2 = m2.width, wMax = Math.max(w1, w2);
  const top = centreY - (lead + asc + desc) / 2, b1 = top + asc, b2 = b1 + lead, cx = W / 2, x0 = cx - wMax / 2;
  const paintText = (c) => {
    c.font = font; c.letterSpacing = spacing; c.textAlign = 'left'; c.textBaseline = 'alphabetic';
    c.fillText(lines[0], x0, b1); c.fillText(lines[1], x0, b2);
  };
  mc.fillStyle = '#fff'; paintText(mc);
  const A = mc.getImageData(0, 0, m.width, m.height).data;
  const inside = (x, y) => { x |= 0; y |= 0; return x >= 0 && y >= 0 && x < m.width && y < m.height && A[(y * m.width + x) * 4 + 3] > 127; };
  const bx0 = cx - wMax / 2 - 6, bx1 = cx + wMax / 2 + 6, by0 = top - 6, by1 = b2 + desc + 6;

  // vertices: one jittered point per grid cell (inside and just outside the letters, so the mesh
  // covers the whole word before it is cut to the letters), plus evenly spaced points on the outlines
  const cell = fs / (phone ? 8 : 10), ecell = fs / (phone ? 16 : 22);
  const pts = [], shown = [];
  for (let y = by0 - cell; y < by1 + cell; y += cell) for (let x = bx0 - cell; x < bx1 + cell; x += cell) {
    const px = x + (.15 + rnd() * .7) * cell, py = y + (.15 + rnd() * .7) * cell;
    pts.push([px, py]); shown.push(inside(px, py));
  }
  const taken = new Set(), edge = [];
  for (let y = by0 | 0; y < by1; y++) for (let x = bx0 | 0; x < bx1; x++) {
    if (!inside(x, y) || (inside(x + 1, y) && inside(x - 1, y) && inside(x, y + 1) && inside(x, y - 1))) continue;
    const key = ((x / ecell) | 0) + ',' + ((y / ecell) | 0);
    if (taken.has(key)) continue;
    taken.add(key); pts.push([x, y]); shown.push(true); edge.push([x, y]);
  }

  // each vertex has a height, so neighbouring facets share corners and light consistently;
  // one light from the top left
  const z = pts.map(() => rnd() * cell * 1.6);
  const L = [-.45, -.6, .66], Ln = Math.hypot(...L); L[0] /= Ln; L[1] /= Ln; L[2] /= Ln;
  const T = Delaunator.from(pts).triangles, tris = [];
  for (let i = 0; i < T.length; i += 3) {
    const a = T[i], b = T[i + 1], c = T[i + 2];
    const gx = (pts[a][0] + pts[b][0] + pts[c][0]) / 3, gy = (pts[a][1] + pts[b][1] + pts[c][1]) / 3;
    if (gx < bx0 - cell || gx > bx1 + cell || gy < by0 - cell || gy > by1 + cell) continue;
    const ux = pts[b][0] - pts[a][0], uy = pts[b][1] - pts[a][1], uz = z[b] - z[a];
    const vx = pts[c][0] - pts[a][0], vy = pts[c][1] - pts[a][1], vz = z[c] - z[a];
    let nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
    if (nz < 0) { nx = -nx; ny = -ny; nz = -nz; }
    const lit = Math.max(0, (nx * L[0] + ny * L[1] + nz * L[2]) / (Math.hypot(nx, ny, nz) || 1));
    const k = .22 + .78 * lit;
    // dark facets lean cyan, lit ones go to near white
    tris.push({ a, b, c, p: (gx - bx0) / (bx1 - bx0), fill: `rgb(${(40 + k * 208) | 0},${(100 + k * 152) | 0},${(135 + k * 120) | 0})` });
  }
  // every point starts in a loose cloud around the middle and settles over a second
  const from = pts.map(() => { const r = (.25 + rnd() * .75) * Math.max(W, H) * .55, a = rnd() * Math.PI * 2; return [cx + Math.cos(a) * r, centreY + Math.sin(a) * r * .6, rnd() * .45]; });
  const cur = pts.map((p) => [p[0], p[1]]);

  // The mesh is drawn on its own layer, then cut to the letters with one mask holding both lines
  // (cutting line by line would leave only their overlap). The mask's outline is slightly imperfect,
  // like a scanned surface: the edges drift by a pixel or two along smooth noise, plus a few chips.
  const mk = () => { const c = document.createElement('canvas'); c.width = canvas.width; c.height = canvas.height; return c; };
  const layer = mk(), lc = layer.getContext('2d');
  const txt = mk(), tx = txt.getContext('2d'); tx.setTransform(dpr, 0, 0, dpr, 0, 0); tx.fillStyle = '#fff'; paintText(tx);
  const amp = Math.max(.9, fs * .011) * dpr, sw = Math.max(1, Math.round(2 * dpr)), ph = rnd() * 100;
  const noise = (u, k) => Math.sin(u * .09 + ph * k) * .6 + Math.sin(u * .031 + ph * 2 * k) * .4 + Math.sin(u * .27 + ph * 3 * k) * .18;
  const mid = mk(), md = mid.getContext('2d');
  for (let x = 0; x < mid.width; x += sw) md.drawImage(txt, x, 0, sw, mid.height, x, amp * noise(x / dpr, 1), sw, mid.height);
  const cut = mk(), cc = cut.getContext('2d');
  for (let y = 0; y < cut.height; y += sw) cc.drawImage(mid, 0, y, cut.width, sw, amp * noise(y / dpr, 2), y, cut.width, sw);
  cc.globalCompositeOperation = 'destination-out'; cc.setTransform(dpr, 0, 0, dpr, 0, 0);
  const chips = phone ? 5 : 9;
  for (let i = 0; i < chips && edge.length; i++) {
    const e = edge[(rnd() * edge.length) | 0], r = fs * (.018 + rnd() * .02), a0 = rnd() * Math.PI * 2;
    cc.beginPath(); cc.moveTo(e[0] + Math.cos(a0) * r, e[1] + Math.sin(a0) * r);
    cc.lineTo(e[0] + Math.cos(a0 + 2.1) * r, e[1] + Math.sin(a0 + 2.1) * r); cc.lineTo(e[0] + Math.cos(a0 + 4.2) * r * .6, e[1] + Math.sin(a0 + 4.2) * r * .6); cc.fill();
  }
  cc.globalCompositeOperation = 'source-over';

  const path = (c, q) => { c.beginPath(); c.moveTo(cur[q.a][0], cur[q.a][1]); c.lineTo(cur[q.b][0], cur[q.b][1]); c.lineTo(cur[q.c][0], cur[q.c][1]); c.closePath(); };

  function draw(t) {
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0); ctx.clearRect(0, 0, W, H); ctx.globalAlpha = 1;
    for (let n = 0; n < pts.length; n++) {
      const k = ease(clamp((t - from[n][2]) / 1));
      cur[n][0] = from[n][0] + (pts[n][0] - from[n][0]) * k; cur[n][1] = from[n][1] + (pts[n][1] - from[n][1]) * k;
    }
    const polish = clamp((t - POLISH) / .35);
    if (t > SCAN0 && polish < 1) {
      lc.setTransform(dpr, 0, 0, dpr, 0, 0); lc.globalCompositeOperation = 'source-over'; lc.clearRect(0, 0, W, H);
      // facets fill left to right
      lc.lineWidth = .6;
      for (const q of tris) {
        const fa = clamp((t - (FILL0 + q.p * FILL)) / .28);
        if (!fa) continue;
        lc.globalAlpha = fa; lc.fillStyle = q.fill; lc.strokeStyle = q.fill;
        path(lc, q); lc.fill(); lc.stroke();
      }
      // the wireframe appears behind the scan line and fades as the facets fill
      lc.lineWidth = .8; lc.strokeStyle = '#22d3ee';
      for (const q of tris) {
        const ea = clamp((t - (SCAN0 + q.p * SCAN)) / .15) * (1 - clamp((t - (FILL0 + q.p * FILL + .15)) / .4) * .75);
        if (!ea) continue;
        lc.globalAlpha = ea * .9; path(lc, q); lc.stroke();
      }
      lc.globalAlpha = 1; lc.globalCompositeOperation = 'destination-in'; lc.setTransform(1, 0, 0, 1, 0, 0); lc.drawImage(cut, 0, 0);
      lc.globalCompositeOperation = 'source-over';
      ctx.setTransform(1, 0, 0, 1, 0, 0); ctx.globalAlpha = 1 - polish; ctx.drawImage(layer, 0, 0); ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    }
    // the point cloud, fading once the mesh is wired
    const pa = 1 - clamp((t - (SCAN0 + .3)) / .8);
    if (pa) { ctx.fillStyle = '#67e8f9'; ctx.globalAlpha = pa * .9; for (let n = 0; n < cur.length; n++) if (shown[n]) ctx.fillRect(cur[n][0] - 1, cur[n][1] - 1, 2, 2); }
    ctx.globalAlpha = 1;
    // the scan line
    if (t > SCAN0 - .05 && t < SCAN0 + SCAN + .1) {
      const sx = bx0 + clamp((t - SCAN0) / SCAN) * (bx1 - bx0), g = ctx.createLinearGradient(sx - 50, 0, sx + 2, 0);
      g.addColorStop(0, 'rgba(34,211,238,0)'); g.addColorStop(1, 'rgba(34,211,238,.35)');
      ctx.fillStyle = g; ctx.fillRect(sx - 50, by0 - 30, 52, by1 - by0 + 60);
      ctx.fillStyle = '#cffafe'; ctx.shadowColor = '#22d3ee'; ctx.shadowBlur = 14; ctx.fillRect(sx, by0 - 30, 2, by1 - by0 + 60); ctx.shadowBlur = 0;
    }
    if (polish) { ctx.globalAlpha = polish; ctx.fillStyle = '#f1f5f9'; paintText(ctx); ctx.globalAlpha = 1; }
  }

  return {
    duration: DURATION,
    draw,
    // the plain name, as the build lays it out
    paintText(c) { c.save(); c.setTransform(dpr, 0, 0, dpr, 0, 0); c.fillStyle = '#f1f5f9'; paintText(c); c.restore(); },
    // the solid text's ink box, and each line's left edge, width and baseline (CSS px within the
    // canvas), for flying it onto the page's <h1>
    textBox: { left: x0, top, width: wMax, height: b2 + desc - top },
    lineLeft: x0,
    lineWidths: [w1, w2],
    baselines: [b1, b2],
    chips,
    pointCount: shown.filter(Boolean).length,
  };
}
