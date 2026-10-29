"""The name build (js/intro/name-build.js): a point cloud reconstructed into the name. Each test draws
one moment of it on a canvas the size of the viewport (seed 1) and compares it with the plain text."""
import pytest

SETUP = """async ({ phone }) => {
  await window.__nameBuildReady;
  const W = innerWidth, H = innerHeight, mk = () => { const c = document.createElement('canvas'); c.width = W; c.height = H; c.style.width = W + 'px'; c.style.height = H + 'px'; return c; };
  const canvas = mk(), ref = mk();
  const fontSize = phone ? Math.round(W * .15) : Math.round(Math.min(170, W * .085));
  const build = window.__nameBuild.create(canvas, { lines: ['Muhammad', 'Kashif-Khan'], fontFamily: 'Inter Build', fontSize, centreY: H * .45, phone, seed: 1 });
  build.paintText(ref.getContext('2d'));
  window.__t = { W, H, canvas, ref, build, fontSize };
  return { fontSize, chips: build.chips, points: build.pointCount };
}"""

# alpha of the build and the reference, and helpers, at time t
MEASURE = """({ t, tol, pointsOnly }) => {
  const { W, H, canvas, ref, build } = window.__t;
  build.draw(t);
  const a = canvas.getContext('2d').getImageData(0, 0, W, H).data, b = ref.getContext('2d').getImageData(0, 0, W, H).data;
  const A = (x, y) => a[(y * W + x) * 4 + 3], B = (x, y) => b[(y * W + x) * 4 + 3];
  const near = (x, y, r) => { for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) { const X = x + dx, Y = y + dy; if (X >= 0 && Y >= 0 && X < W && Y < H && B(X, Y) > 127) return true; } return false; };
  let inside = 0, covered = 0, far = 0, edge = 0, edgeDiff = 0, maxDiff = 0, opaque = 0;
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const av = A(x, y), bv = B(x, y);
    maxDiff = Math.max(maxDiff, Math.abs(av - bv));
    if (av > 0) opaque++;
    if (bv > 127) {
      inside++; if (av > 0) covered++;
      if (x && y && x < W - 1 && y < H - 1 && (B(x + 1, y) < 128 || B(x - 1, y) < 128 || B(x, y + 1) < 128 || B(x, y - 1) < 128)) { edge++; if (av < 128) edgeDiff++; }
    } else if (av > 127 && !near(x, y, tol)) {
      // pointsOnly: just the point cloud's colour (#67e8f9), not the scan line passing by
      const i = (y * W + x) * 4;
      if (!pointsOnly || (a[i] > 80 && a[i] < 130 && a[i + 1] > 210 && a[i + 2] > 235)) far++;
    }
  }
  return { coverage: covered / inside, far, edgeDiff: edgeDiff / edge, maxDiff, opaque };
}"""


@pytest.fixture
def build(page, site_url):
    def make(width=1440, height=900, phone=False):
        page.set_viewport_size({"width": width, "height": height})
        page.goto(site_url + "/?debug=namebuild", wait_until="load")
        return page.evaluate(SETUP, {"phone": phone})
    return make


def measure(page, t, tol=0, points_only=False):
    return page.evaluate(MEASURE, {"t": t, "tol": tol, "pointsOnly": points_only})


def test_facets_cover_the_letters_without_gaps(page, build):
    build()
    assert measure(page, 2.7)["coverage"] >= 0.97


def test_outline_is_only_slightly_off_the_font(page, build):
    info = build()
    tol = int(max(0.9, 0.011 * info["fontSize"]) + 0.999) + 1
    m = measure(page, 2.7, tol)
    assert m["far"] == 0, m
    assert m["edgeDiff"] >= 0.005, m


def test_polish_ends_on_plain_text(page, build):
    build()
    assert measure(page, 3.3)["maxDiff"] <= 8


def test_points_settle_on_the_letters(page, build):
    info = build()
    assert info["points"] >= 600
    m = measure(page, 1.45, 3, points_only=True)
    assert m["far"] == 0 and m["opaque"] > 0, m


@pytest.mark.parametrize("phone,chips", [(False, 9), (True, 5)])
def test_chip_count(page, build, phone, chips):
    info = build(390 if phone else 1440, 844 if phone else 900, phone)
    assert info["chips"] == chips
