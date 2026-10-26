"""Small text meets WCAG AA contrast (4.5:1) against the page's darkest-to-lightest surfaces."""
import pytest

# Pages checked so far; each redesign PR adds its own
ROUTES = ["/"] + [
    "/projects/machine-learning", "/projects/medical-simulation", "/projects/3d-reconstruction",
    "/projects/pnanolocz", "/projects/distributed-systems", "/projects/home-lab",
    "/projects/synergy", "/projects/formula-student-ai", "/projects/data-mining",
    "/projects/numerical-computation", "/projects/mental-health-app", "/projects/terratracker",
    "/projects/business-plan", "/vascular-blood-flow", "/projects/ai-decision-trees",
    "/projects/ai-search-algorithms", "/projects/parallel-computing", "/projects/autonomous-robotics",
    "/projects/opengl-renderer", "/projects/xv6-operating-system", "/projects/quantum-dots",
    "/projects/c-programming", "/projects/graphics-fundamentals", "/projects/taskflow", "/projects/portfolio",
]

CHECK = """() => {
  const lum = (r, g, b) => { const f = (c) => { c /= 255; return c <= .03928 ? c / 12.92 : ((c + .055) / 1.055) ** 2.4; }; return .2126 * f(r) + .7152 * f(g) + .0722 * f(b); };
  const rgb = (c) => c.match(/[\\d.]+/g).map(Number);
  // the nearest opaque background; translucent panels count as ink-2, the lightest surface text sits on
  const background = (el) => {
    for (let e = el; e; e = e.parentElement) {
      const [r, g, b, a = 1] = rgb(getComputedStyle(e).backgroundColor);
      if (a >= .9) return lum(r, g, b);
    }
    return lum(0x11, 0x1a, 0x2e);
  };
  const bad = [];
  for (const el of document.querySelectorAll('main *, header *, footer *')) {
    if (![...el.childNodes].some((n) => n.nodeType === 3 && n.textContent.trim())) continue;
    const cs = getComputedStyle(el);
    if (cs.visibility === 'hidden' || cs.display === 'none' || el.closest('[aria-hidden=true], .workshop-pin, canvas')) continue;
    const m = cs.color.match(/[\\d.]+/g); if (!m) continue;
    const [r, g, b, a = 1] = m.map(Number); if (a < .5) continue;
    const size = parseFloat(cs.fontSize), large = size >= 24 || (size >= 18.66 && +cs.fontWeight >= 700);
    const fg = lum(r, g, b), bg = background(el);
    const ratio = (Math.max(fg, bg) + .05) / (Math.min(fg, bg) + .05);
    if (ratio < (large ? 3 : 4.5)) bad.push(`${el.tagName}.${String(el.className).slice(0, 40)} "${el.textContent.trim().slice(0, 30)}" ${ratio.toFixed(2)}`);
  }
  return bad;
}"""


@pytest.mark.parametrize("route", ROUTES)
def test_small_text_meets_aa_contrast(page, site_url, route):
    page.goto(site_url + route, wait_until="load")
    bad = page.evaluate(CHECK)
    assert bad == [], "\n".join(bad[:15])
