"""The homepage intro (js/intro): the name build is the loading screen. It plays once per tab on a full
load of /, scrolling waits for it, and it lands on the <h1> when the build is done and the scene is ready."""
import time

import pytest

pytestmark = pytest.mark.intro  # conftest skips the intro for every other test

PENDING = "document.documentElement.classList.contains('intro-pending')"
LANDED = "!['intro-pending', 'intro-flying', 'intro-settling'].some(c => document.documentElement.classList.contains(c))"
SCENE = "**/js/workshop/workshop*.js"


def wait_landed(page, timeout=15000):
    page.wait_for_function(LANDED, timeout=timeout)
    page.wait_for_timeout(400)


def h1_rect(page):
    return page.eval_on_selector("#hero h1", "e => { const r = e.getBoundingClientRect(); return [r.left, r.top, r.width, r.height]; }")


@pytest.fixture
def desktop(page):
    page.set_viewport_size({"width": 1440, "height": 900})
    return page


@pytest.fixture
def quiet(desktop):
    """Without the 3D scene: the test browser draws it in software, holding the main thread for a second
    or more at a time, which throws out tests about the intro's own timing. The intro lands on the still."""
    desktop.route(SCENE, lambda route: route.abort())
    return desktop


# when <html> lost intro-pending and intro-flying, in ms since the page opened
LANDED_AT = """document.addEventListener('DOMContentLoaded', () => { const html = document.documentElement; new MutationObserver(() => {
    if (window.__landedAt == null && !html.classList.contains('intro-pending') && !html.classList.contains('intro-flying')) window.__landedAt = performance.now();
  }).observe(html, { attributes: true, attributeFilter: ['class'] }); })"""


def test_intro_plays_and_lands_on_the_h1(desktop, site_url):
    page = desktop
    page.goto(site_url + "/", wait_until="domcontentloaded")
    assert page.evaluate(PENDING)
    wait_landed(page)
    assert page.eval_on_selector(".hero-namefx", "e => getComputedStyle(e).display") == "none"
    assert page.eval_on_selector("#hero h1", "e => getComputedStyle(e).visibility") == "visible"
    landed = h1_rect(page)
    page.goto(site_url + "/?debug=workshop", wait_until="load")  # the same page without an intro
    assert not page.evaluate(PENDING)
    assert all(abs(a - b) <= 2 for a, b in zip(landed, h1_rect(page))), (landed, h1_rect(page))


def test_scroll_is_locked_until_the_name_lands(desktop, site_url):
    page = desktop
    page.goto(site_url + "/", wait_until="load")
    page.mouse.move(700, 450)
    page.mouse.wheel(0, 900)
    page.keyboard.press("PageDown")
    page.keyboard.press("Space")
    page.keyboard.press("End")
    page.wait_for_timeout(300)
    assert page.evaluate(PENDING) and page.evaluate("scrollY") == 0
    wait_landed(page)
    page.mouse.wheel(0, 900)
    page.wait_for_timeout(500)
    assert page.evaluate("scrollY") > 0


def test_name_holds_until_the_scene_is_ready(desktop, site_url):
    page = desktop
    held = []
    page.route(SCENE, lambda route: held.append(route))
    page.goto(site_url + "/", wait_until="domcontentloaded")
    page.wait_for_timeout(5000)
    assert page.evaluate(PENDING)
    assert page.is_visible(".hero-loading") and "building the workshop" in page.inner_text(".hero-loading")
    for route in held:
        route.continue_()
    wait_landed(page)
    page.wait_for_selector("#workshop[data-state=live]", timeout=30000)


def test_lands_after_ten_seconds_on_the_still(desktop, site_url):
    page = desktop
    page.route(SCENE, lambda route: None)  # never answered
    start = time.monotonic()
    page.goto(site_url + "/", wait_until="domcontentloaded")
    page.wait_for_function(LANDED, timeout=12000)
    assert 9.0 <= time.monotonic() - start <= 11.5
    assert page.is_visible(".workshop-still img")


def test_reload_replays_the_intro_from_the_top_even_with_a_hash(desktop, site_url):
    page = desktop
    page.goto(site_url + "/", wait_until="load")
    wait_landed(page)
    page.click(".hero-buttons a:has-text('Get in touch')")
    page.wait_for_timeout(1500)
    assert page.url.endswith("#contact") and page.evaluate("scrollY") > 0
    page.reload(wait_until="domcontentloaded")
    assert page.evaluate(PENDING)
    page.wait_for_timeout(800)
    assert page.evaluate("scrollY") == 0
    wait_landed(page)


def test_coming_back_in_the_same_tab_has_no_intro(desktop, site_url):
    page = desktop
    page.goto(site_url + "/", wait_until="load")
    wait_landed(page)
    page.goto(site_url + "/about", wait_until="load")
    page.goto(site_url + "/", wait_until="load")
    assert not page.evaluate(PENDING)


def test_reloading_another_page_keeps_its_place(desktop, site_url):
    # browsers keep a reloaded page where it was; only the homepage (whose intro replays) starts at the top
    page = desktop
    page.goto(site_url + "/projects/medical-simulation", wait_until="load")
    page.mouse.move(700, 450)
    page.mouse.wheel(0, 1200)
    page.wait_for_timeout(600)
    y = page.evaluate("scrollY")
    assert y > 0
    page.reload(wait_until="load")
    page.wait_for_timeout(800)
    assert abs(page.evaluate("scrollY") - y) <= 50


def test_scroll_waits_until_the_name_has_flown_to_its_place(quiet, site_url):
    page = quiet
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_function("document.documentElement.classList.contains('intro-flying')", timeout=15000, polling=20)
    page.mouse.move(700, 450)
    page.mouse.wheel(0, 900)
    page.keyboard.press("PageDown")
    page.wait_for_timeout(300)
    assert page.evaluate("document.documentElement.classList.contains('intro-flying')") and page.evaluate("scrollY") == 0
    wait_landed(page)
    page.mouse.wheel(0, 900)
    page.wait_for_timeout(500)
    assert page.evaluate("scrollY") > 0


def test_the_build_never_pauses(desktop, site_url):
    """Nothing (the 3D scene's first frame, above all) holds the main thread while the name builds."""
    page = desktop
    page.add_init_script("""window.__long = []; new PerformanceObserver(l => l.getEntries().forEach(e => __long.push([e.startTime, e.duration]))).observe({ type: 'longtask', buffered: true });""")
    page.goto(site_url + "/", wait_until="load")
    wait_landed(page)
    marks = page.evaluate("[performance.getEntriesByName('mk-build-start')[0]?.startTime, performance.getEntriesByName('mk-build-end')[0]?.startTime]")
    assert None not in marks, marks
    during = [t for t in page.evaluate("__long") if t[0] + t[1] > marks[0] and t[0] < marks[1] and t[1] > 150]
    assert during == [], (marks, during)


def test_get_in_touch_glides_to_the_contact_form(desktop, site_url):
    page = desktop
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    page.click(".hero-buttons a:has-text('Get in touch')")
    page.wait_for_timeout(120)
    mid = page.evaluate("scrollY")
    page.wait_for_timeout(1500)
    end = page.evaluate("scrollY")
    top = page.evaluate("document.getElementById('contact').getBoundingClientRect().top")
    assert 0 < mid < end, (mid, end)
    assert 0 <= top <= 120, top


def test_back_restores_the_scroll_position(desktop, site_url):
    page = desktop
    page.goto(site_url + "/", wait_until="load")
    wait_landed(page)
    page.evaluate("document.getElementById('work').scrollIntoView()")
    page.wait_for_timeout(500)
    y = page.evaluate("scrollY")
    page.click("#work a[href^='/projects/']")
    page.wait_for_url("**/projects/**")
    page.go_back()
    page.wait_for_url(site_url + "/")
    page.wait_for_timeout(1000)
    assert abs(page.evaluate("scrollY") - y) <= 50


def test_no_intro_for_a_section_link(desktop, site_url):
    page = desktop
    page.goto(site_url + "/#contact", wait_until="load")
    assert not page.evaluate(PENDING)
    page.wait_for_timeout(500)
    assert page.evaluate("document.getElementById('contact').getBoundingClientRect().top") < 450


def test_no_intro_from_enhanced_navigation(desktop, site_url):
    page = desktop
    page.goto(site_url + "/projects", wait_until="load")
    page.click("header a[href='/']")
    page.wait_for_url(site_url + "/")
    page.wait_for_timeout(300)
    assert not page.evaluate(PENDING)


def test_no_intro_with_reduced_motion(browser, site_url):
    context = browser.new_context(reduced_motion="reduce", viewport={"width": 1440, "height": 900})
    page = context.new_page()
    try:
        page.goto(site_url + "/", wait_until="load")
        assert not page.evaluate(PENDING)
        page.mouse.move(700, 450)
        page.mouse.wheel(0, 900)
        page.wait_for_timeout(400)
        assert page.evaluate("scrollY") > 0
    finally:
        context.close()


def test_header_click_ends_the_intro(desktop, site_url):
    page = desktop
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_timeout(600)
    assert page.evaluate(PENDING)
    page.click("header nav a[href='/#contact']")
    page.wait_for_function(LANDED, timeout=1000)
    page.wait_for_timeout(800)
    assert page.evaluate("document.getElementById('contact').getBoundingClientRect().top") < 450


def test_tab_key_ends_the_intro(quiet, site_url):
    page = quiet
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_timeout(600)
    assert page.evaluate(PENDING)
    page.keyboard.press("Tab")
    page.wait_for_function(LANDED, timeout=1000, polling=50)
    assert page.evaluate("scrollY") == 0


def test_leaving_mid_intro_releases_the_lock(desktop, site_url):
    page = desktop
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_timeout(600)
    page.click("header nav a[href='/about']")
    page.wait_for_url("**/about")
    page.wait_for_timeout(500)
    assert not page.evaluate("mkIntro.running")
    page.mouse.move(700, 450)
    page.mouse.wheel(0, 900)
    page.wait_for_timeout(400)
    assert page.evaluate("scrollY") > 0


def test_resize_mid_intro_lands_on_the_h1(desktop, site_url):
    page = desktop
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_timeout(1000)
    page.set_viewport_size({"width": 1100, "height": 800})
    wait_landed(page)
    assert page.eval_on_selector(".hero-namefx", "e => getComputedStyle(e).display") == "none"
    r = h1_rect(page)
    assert r[2] > 0 and 0 <= r[0] < 1100 and 0 <= r[1] < 800
    assert page.eval_on_selector("#hero h1", "e => getComputedStyle(e).visibility") == "visible"


@pytest.mark.parametrize("missing", ["**/fonts/inter-latin-800-normal.woff2", "**/lib/delaunator/delaunator.min.js"])
def test_missing_font_or_triangulator_shows_the_landed_hero(desktop, site_url, missing):
    page = desktop
    page.route(missing, lambda route: route.abort())
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_function(LANDED, timeout=1000)
    page.mouse.move(700, 450)
    page.mouse.wheel(0, 900)
    page.wait_for_timeout(400)
    assert page.evaluate("scrollY") > 0


def test_background_tab_waits_to_be_seen(desktop, site_url):
    page = desktop
    page.add_init_script("""(() => {
        let hidden = true;
        Object.defineProperty(document, 'hidden', { get: () => hidden, configurable: true });
        Object.defineProperty(document, 'visibilityState', { get: () => (hidden ? 'hidden' : 'visible'), configurable: true });
        window.__show = () => { hidden = false; document.dispatchEvent(new Event('visibilitychange')); };
    })()""")
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_timeout(10500)
    assert page.evaluate(PENDING)
    page.evaluate("__show()")
    wait_landed(page)


def test_phone_intro_lands_on_the_h1(page, site_url):
    page.set_viewport_size({"width": 390, "height": 844})
    page.goto(site_url + "/", wait_until="domcontentloaded")
    assert page.evaluate(PENDING)
    wait_landed(page)
    page.wait_for_function("!document.documentElement.classList.contains('intro-landed')", timeout=5000)  # the room has grown in
    assert page.eval_on_selector("#hero h1", "e => getComputedStyle(e).visibility") == "visible"
    head, stage = page.locator(".hero-head").bounding_box(), page.locator("#workshop").bounding_box()
    assert head["y"] + head["height"] <= stage["y"]


@pytest.mark.parametrize("size", [(844, 390), (390, 844), (1440, 900)])
def test_loading_label_clears_the_name(page, site_url, size):
    page.set_viewport_size({"width": size[0], "height": size[1]})
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_timeout(2600)  # the build is mostly filled
    assert page.evaluate(PENDING)
    painted = page.evaluate("""() => {
        const c = document.querySelector('.hero-namefx'), l = document.querySelector('.hero-loading').getBoundingClientRect(), r = c.getBoundingClientRect();
        const k = c.width / r.width, x = Math.max(0, (l.left - r.left) * k), y = Math.max(0, (l.top - r.top) * k);
        const d = c.getContext('2d').getImageData(x, y, Math.max(1, l.width * k), Math.max(1, l.height * k)).data;
        let n = 0; for (let i = 3; i < d.length; i += 4) if (d[i] > 40) n++;
        return n;
    }""")
    assert painted == 0


@pytest.mark.parametrize("stalled, by", [("**/fonts/inter-latin-800-normal.woff2", 10.8), ("**/js/intro/intro*.js", 14.8)])
def test_a_stalled_download_never_holds_the_page(quiet, site_url, stalled, by):
    """A request that never answers (not just one that fails) still lets the visitor in: the intro gives
    up 10 s after the page opened, and the head script's own watchdog covers intro.js itself."""
    page = quiet
    page.route(stalled, lambda route: None)
    page.add_init_script(LANDED_AT)
    page.goto(site_url + "/", wait_until="domcontentloaded")
    page.wait_for_function(LANDED, timeout=by * 1000 + 2000, polling=100)
    assert page.evaluate("window.__landedAt") <= by * 1000
    page.mouse.move(700, 450)
    page.mouse.wheel(0, 900)
    page.wait_for_timeout(400)
    assert page.evaluate("scrollY") > 0


LANDING = """() => {
    const c = document.querySelector('.hero-namefx'), l = JSON.parse(c.dataset.landing), r = c.getBoundingClientRect();
    const spans = [...document.querySelectorAll('#hero h1 span')], h1 = document.querySelector('#hero h1');
    const text = spans.map(s => { const g = document.createRange(); g.selectNodeContents(s.firstChild); return g.getBoundingClientRect().toJSON(); });
    const mark = document.createElement('span'); mark.style.cssText = 'display:inline-block;width:0;height:0;vertical-align:baseline';
    spans[1].append(mark); const baseline2 = mark.getBoundingClientRect().bottom; mark.remove();
    return { l, canvas: [r.left, r.top], text, baseline2 };
}"""


def assert_lands_on_the_text(page):
    page.wait_for_function("document.documentElement.classList.contains('intro-flying')", timeout=15000, polling=20)
    m = page.evaluate(LANDING)
    l, (t1, t2) = m["l"], m["text"]
    cl, ct = l["left"], l["top"]  # the canvas where it was when the flight began
    # each line's mapped left edge and width match the <h1>'s text, and line 2's baseline sits on it
    for i, t in enumerate((t1, t2)):
        assert abs(cl + l["tx"] + l["x0"] * l["s"] - t["left"]) <= 3, (i, m)
        assert abs(l["widths"][i] * l["s"] - t["width"]) <= 0.03 * t["width"], (i, m)
    assert abs(ct + l["ty"] + l["b2"] * l["s"] - m["baseline2"]) <= 3, m


def test_the_built_name_lands_on_the_h1_text(desktop, site_url):
    desktop.goto(site_url + "/", wait_until="domcontentloaded")
    assert_lands_on_the_text(desktop)


def test_the_built_name_lands_on_the_h1_text_on_the_still(quiet, site_url):
    quiet.goto(site_url + "/", wait_until="domcontentloaded")
    assert_lands_on_the_text(quiet)


def test_the_built_name_lands_on_the_h1_text_on_a_phone(page, site_url):
    page.set_viewport_size({"width": 390, "height": 844})
    page.goto(site_url + "/", wait_until="domcontentloaded")
    assert_lands_on_the_text(page)


def test_resized_mid_intro_the_name_lands_on_the_new_h1(desktop, site_url):
    desktop.goto(site_url + "/", wait_until="load")
    desktop.wait_for_timeout(1000)
    desktop.set_viewport_size({"width": 1100, "height": 800})
    assert_lands_on_the_text(desktop)


def test_enhanced_navigation_mid_intro_releases_the_lock(desktop, site_url):
    page = desktop
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_timeout(800)
    assert page.evaluate(PENDING)
    page.evaluate("Blazor.navigateTo('/about')")
    page.wait_for_url("**/about")
    page.wait_for_timeout(500)
    assert not page.evaluate("mkIntro.running") and page.evaluate(LANDED)
    page.mouse.move(700, 450)
    page.mouse.wheel(0, 900)
    page.wait_for_timeout(400)
    assert page.evaluate("scrollY") > 0


def test_scroll_waits_until_the_room_has_settled(desktop, site_url):
    """After the name has flown to its place, the room's own entrance (objects popping in, the camera
    coming to rest) plays out before the page scrolls."""
    page = desktop
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_function("document.documentElement.classList.contains('intro-flying')", timeout=15000, polling=20)
    page.wait_for_function("!document.documentElement.classList.contains('intro-flying')", timeout=5000, polling=20)
    assert page.evaluate("document.documentElement.classList.contains('intro-settling')")
    page.mouse.move(700, 450)
    page.mouse.wheel(0, 900)
    page.wait_for_timeout(200)
    assert page.evaluate("scrollY") == 0
    page.wait_for_function("!document.documentElement.classList.contains('intro-settling')", timeout=8000, polling=50)
    page.mouse.wheel(0, 900)
    page.wait_for_timeout(500)
    assert page.evaluate("scrollY") > 0


def test_phone_page_is_never_wider_than_the_screen_during_the_intro(page, site_url):
    # the room's entrance zooms it out from 112%; on a phone it is edge to edge, so the hero clips it
    page.set_viewport_size({"width": 390, "height": 844})
    page.goto(site_url + "/", wait_until="domcontentloaded")
    widest = 0
    for _ in range(120):
        widest = max(widest, page.evaluate("document.documentElement.scrollWidth"))
        if page.evaluate(LANDED + " && !document.documentElement.classList.contains('intro-landed')"):
            break
        page.wait_for_timeout(100)
    assert widest <= 390


def test_screen_readers_get_the_name_while_it_builds(desktop, site_url):
    page = desktop
    page.goto(site_url + "/", wait_until="domcontentloaded")
    assert page.evaluate(PENDING)
    heading = page.get_by_role("heading", level=1)
    assert heading.count() == 1 and "Kashif-Khan" in heading.inner_text()
    assert page.eval_on_selector("#hero h1", "e => getComputedStyle(e).opacity") == "0"  # still hidden on screen
