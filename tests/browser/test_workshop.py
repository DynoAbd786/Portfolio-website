"""The homepage's 3D workshop (website/wwwroot/js/workshop)."""
import pytest

LIVE = "#workshop[data-state=live]"


@pytest.fixture(autouse=True)
def _workshop_log(page):
    """Keeps the page's [workshop] warnings, so a scene that fails to start says why."""
    page.workshop_log = []
    page.on("console", lambda m: page.workshop_log.append(m.text) if "[workshop]" in m.text else None)


def wait_live(page, settle=3500):
    try:
        page.wait_for_selector(LIVE, timeout=30000)
    except Exception:
        state = page.evaluate("[document.getElementById('workshop')?.dataset.state, mkWorkshop.active, mkWorkshop.mounts]")
        raise AssertionError(f"the scene never went live: [state, active, mounts] = {state}, log = {getattr(page, 'workshop_log', None)}")
    page.wait_for_timeout(settle)  # the intro flight and pop-in


def pin(page, key):
    """Page coordinates of a point that hovers/clicks the given object."""
    state = page.evaluate("document.getElementById('workshop').dataset.state")
    assert state == "live", f"the scene is not running (data-state={state})"
    found = page.evaluate(
        """key => { const root = document.getElementById('workshop');
                    const p = root.__workshop.pins().find(p => p.key === key);
                    const r = root.getBoundingClientRect();
                    return p && { x: r.left + p.x / 100 * r.width, y: r.top + p.y / 100 * r.height }; }""",
        key,
    )
    assert found, f"no pin for {key}"
    return found["x"], found["y"]


def open_card(page, key):
    x, y = pin(page, key)
    page.mouse.move(x, y)
    page.wait_for_timeout(300)
    page.mouse.click(x, y)
    page.wait_for_selector(".ws-card.open", timeout=3000)


def test_scene_goes_live_without_errors(page, page_errors, site_url):
    page.goto(site_url + "/", wait_until="load")
    wait_live(page, settle=1000)
    assert page_errors == []


def test_hover_shows_tag_and_click_opens_card(page, site_url):
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    wait_live(page)
    x, y = pin(page, "thesis")
    page.mouse.move(x - 4, y)
    page.mouse.move(x, y)
    page.wait_for_selector(".ws-tag.on .ws-tag-in", timeout=3000)
    assert "MEng project" in page.inner_text(".ws-tag")
    page.mouse.click(x, y)
    page.wait_for_selector(".ws-card.open", timeout=3000)
    assert page.is_visible(".ws-card-title")
    assert page.inner_text(".ws-card-title") == "3D from a single video"
    page.keyboard.press("Escape")
    page.wait_for_selector(".ws-card:not(.open)", state="attached", timeout=3000)


def test_robot_opens_chat(page, site_url):
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    wait_live(page)
    open_card(page, "robot")
    page.click(".ws-card-cta")
    page.wait_for_function("!document.getElementById('chat-panel').classList.contains('translate-x-full')", timeout=3000)


def test_workshop_survives_repeated_navigation(page, site_url):
    warnings = []
    page.on("console", lambda m: warnings.append(m.text))
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_selector(LIVE, timeout=30000)
    for _ in range(20):
        page.click("header nav a[href='/projects']")
        page.wait_for_url("**/projects")
        page.click("header nav a[href='/']")
        page.wait_for_url(site_url + "/")
        page.wait_for_selector(LIVE, timeout=30000)
    counts = page.evaluate("[mkWorkshop.mounts, mkWorkshop.disposes]")
    assert counts[0] - counts[1] == 1
    assert not [w for w in warnings if "Too many active WebGL contexts" in w]


def test_workshop_resizes_to_phone(page, site_url):
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_selector(LIVE, timeout=30000)
    page.set_viewport_size({"width": 390, "height": 844})
    page.wait_for_timeout(800)
    canvas = page.locator(".workshop-canvas").bounding_box()
    stage = page.locator("#workshop").bounding_box()
    for k in ("x", "y", "width", "height"):
        assert abs(canvas[k] - stage[k]) <= 2, (k, canvas, stage)
    h1 = page.locator("h1").bounding_box()
    assert h1["y"] + h1["height"] <= canvas["y"]  # phones: the name, then the room
    # the drawing buffer follows the element, so the frame is not stretched
    ratio = page.evaluate("(() => { const c = document.querySelector('.workshop-canvas'); return (c.width / c.height) / (c.clientWidth / c.clientHeight); })()")
    assert abs(ratio - 1) < 0.02


def test_workshop_ignores_keys_in_inputs(page, site_url):
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    wait_live(page)
    open_card(page, "thesis")
    page.focus("#message")
    page.keyboard.press("ArrowRight")
    page.keyboard.press("Escape")
    page.wait_for_timeout(300)
    assert page.is_visible(".ws-card.open")
    assert page.inner_text(".ws-card-title") == "3D from a single video"


def test_reduced_motion_opens_card_without_flight(browser, site_url):
    context = browser.new_context(reduced_motion="reduce", viewport={"width": 1440, "height": 900})
    page = context.new_page()
    try:
        page.goto(site_url + "/?debug=workshop", wait_until="load")
        wait_live(page, settle=800)
        x, y = pin(page, "thesis")
        page.mouse.move(x, y)
        page.mouse.click(x, y)
        page.wait_for_selector(".ws-card.open", timeout=300)
    finally:
        context.close()


def test_other_pages_do_not_load_three(page, site_url):
    urls = []
    page.on("request", lambda r: urls.append(r.url))
    for path in ("/projects", "/projects/home-lab"):
        page.goto(site_url + path, wait_until="networkidle")
    assert not [u for u in urls if "/lib/three/" in u or "/js/workshop/" in u]


def test_tour_buttons_reach_every_hotspot(page, site_url):
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_selector(LIVE, timeout=30000)
    buttons = page.locator(".ws-tour button")
    assert buttons.count() == 11
    page.focus("main a[href='#work']")
    for _ in range(40):
        page.keyboard.press("Tab")
        if page.evaluate("!!document.activeElement.closest('.ws-tour')"):
            break
    assert page.evaluate("!!document.activeElement.closest('.ws-tour')")
    page.keyboard.press("Enter")
    page.wait_for_selector(".ws-card.open", timeout=3000)


def test_hover_tag_does_not_widen_the_page(page, site_url):
    page.set_viewport_size({"width": 390, "height": 844})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    wait_live(page)
    for key in ("robot", "bookcase"):
        x, y = pin(page, key)
        page.mouse.move(x, y)
        page.wait_for_timeout(600)
        assert page.evaluate("document.documentElement.scrollWidth") <= 390, key


HOTSPOT_NAMES = {
    "synergy": "Synergy", "hireforge": "HireForge", "thesis": "MEng project", "stent": "Stent simulation",
    "afm": "pNanoLocz", "homelab": "Home lab", "azure": "Azure inventory", "whiteboard": "Internships",
    "certificate": "Education", "bookcase": "All projects", "robot": "Assistant",
}


def test_every_hotspot_is_pickable(page, site_url):
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    wait_live(page)
    keys = [p["key"] for p in page.evaluate("document.getElementById('workshop').__workshop.pins()")]
    assert sorted(keys) == sorted(HOTSPOT_NAMES)
    for key, name in HOTSPOT_NAMES.items():
        x, y = pin(page, key)
        page.mouse.move(x - 3, y)
        page.mouse.move(x, y)
        page.wait_for_function("n => document.querySelector('.ws-tag.on b')?.textContent === n", arg=name, timeout=3000)


def test_every_hotspot_opens_its_card_when_clicked(page, site_url):
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    wait_live(page)
    for key in HOTSPOT_NAMES:
        open_card(page, key)
        page.keyboard.press("Escape")
        page.wait_for_selector(".ws-card:not(.open)", state="attached", timeout=3000)
        page.wait_for_timeout(1500)  # camera back home before the next pin


def test_phone_card_scrolls_into_view(page, site_url):
    """On narrow screens the card sits below the room; opening it from the bottom of the screen must bring it into view."""
    page.set_viewport_size({"width": 390, "height": 844})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    wait_live(page)
    page.evaluate("(() => { const r = document.getElementById('workshop').getBoundingClientRect(); scrollTo(0, scrollY + r.bottom - innerHeight); })()")
    page.wait_for_timeout(300)
    open_card(page, "thesis")
    page.wait_for_timeout(1000)
    top = page.evaluate("document.querySelector('.ws-card').getBoundingClientRect().top")
    assert 0 <= top < page.viewport_size["height"] - 100, top


def _render_scale(page):
    return page.evaluate("(() => { const c = document.querySelector('.workshop-canvas'); return c.width / c.clientWidth; })()")


@pytest.mark.parametrize("tier, at_least", [(0, 2.5), (2, 1.75), (4, 1.0)])
def test_phone_renders_sharp_on_a_high_density_screen(browser, site_url, tier, at_least):
    """A phone's stage is small, so it can afford its screen's density; lower tiers shrink a pixel budget instead of dropping below 1x."""
    context = browser.new_context(viewport={"width": 412, "height": 915}, device_scale_factor=2.625, is_mobile=True, has_touch=True)
    page = context.new_page()
    try:
        page.goto(site_url + "/?debug=workshop", wait_until="load")
        page.wait_for_selector(LIVE, timeout=30000)
        page.evaluate(f"document.getElementById('workshop').__workshop.setTier({tier})")
        page.wait_for_timeout(300)
        assert _render_scale(page) >= at_least
    finally:
        context.close()


def test_desktop_render_scale_is_unchanged(browser, site_url):
    context = browser.new_context(viewport={"width": 1440, "height": 900}, device_scale_factor=2)
    page = context.new_page()
    try:
        page.goto(site_url + "/?debug=workshop", wait_until="load")
        page.wait_for_selector(LIVE, timeout=30000)
        page.evaluate("document.getElementById('workshop').__workshop.setTier(0)")
        page.wait_for_timeout(300)
        assert abs(_render_scale(page) - 1.5) < 0.02
    finally:
        context.close()


def test_phone_name_then_room_filling_the_width(page, site_url):
    page.set_viewport_size({"width": 390, "height": 844})
    page.goto(site_url + "/", wait_until="load")
    stage = page.locator("#workshop").bounding_box()
    head = page.locator(".hero-head").bounding_box()
    body = page.locator(".hero-body").bounding_box()
    assert head["y"] + head["height"] <= stage["y"] and stage["y"] + stage["height"] <= body["y"]
    assert stage["x"] == 0 and stage["width"] == 390
    assert abs(stage["width"] / stage["height"] - 4 / 3) < 0.02
    links = page.locator(".workshop-links").bounding_box()
    assert body["y"] < links["y"]


def test_phone_card_is_a_sheet_with_the_room_in_view(page, site_url):
    """On phones the card slides up over the page with the whole room above it, so tapping object
    after object needs no scrolling."""
    page.set_viewport_size({"width": 390, "height": 844})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    wait_live(page)
    page.evaluate("scrollTo(0, 400)")  # visitor has scrolled a little past the room
    page.wait_for_timeout(300)
    page.evaluate("document.getElementById('workshop').scrollIntoView({block: 'end'})")
    page.wait_for_timeout(300)
    open_card(page, "thesis")
    page.wait_for_timeout(1200)
    assert page.evaluate("getComputedStyle(document.querySelector('.ws-card')).position") == "fixed"
    stage = page.locator("#workshop").bounding_box()
    card = page.locator(".ws-card").bounding_box()
    assert stage["y"] >= 0 and stage["y"] + stage["height"] <= card["y"] + 1, (stage, card)
    assert card["y"] + card["height"] <= 844
    scrolled = page.evaluate("scrollY")
    page.click(".ws-card-back")
    page.wait_for_timeout(1500)  # camera back home
    open_card(page, "robot")
    page.wait_for_timeout(800)
    assert page.inner_text(".ws-card-title") == "Ask the assistant"
    assert page.evaluate("scrollY") == scrolled



def test_phone_tap_just_beside_a_small_object_opens_it(browser, site_url):
    """Objects are small on a phone; a tap that just misses one still opens it."""
    context = browser.new_context(viewport={"width": 390, "height": 844}, is_mobile=True, has_touch=True)
    page = context.new_page()
    try:
        page.goto(site_url + "/?debug=workshop", wait_until="load")
        wait_live(page)
        x, y = pin(page, "robot")
        miss = page.evaluate("""([x, y]) => {
            const ws = document.getElementById('workshop').__workshop;
            for (const [dx, dy] of [[0, -1], [1, 0], [-1, 0], [0, 1]]) {
                let d = 0;
                while (d < 80 && ws.keyAt(x + dx * d, y + dy * d) === 'robot') d += 2;
                const px = x + dx * (d + 8), py = y + dy * (d + 8);
                if (d < 80 && !ws.keyAt(px, py)) return [px, py];
            }
            return null;
        }""", [x, y])
        assert miss, "no empty spot beside the robot"
        page.touchscreen.tap(*miss)
        page.wait_for_selector(".ws-card.open", timeout=3000)
        assert page.inner_text(".ws-card-title") == "Ask the assistant"
    finally:
        context.close()


def test_phone_lightest_tier_draws_few_pixels(browser, site_url):
    """A phone that can't keep up must get light enough to stay animated rather than freeze on the still."""
    context = browser.new_context(viewport={"width": 412, "height": 915}, device_scale_factor=2.625, is_mobile=True, has_touch=True)
    page = context.new_page()
    try:
        page.goto(site_url + "/?debug=workshop", wait_until="load")
        page.wait_for_selector(LIVE, timeout=30000)
        page.evaluate("document.getElementById('workshop').__workshop.setTier(4)")
        page.wait_for_timeout(300)
        pixels = page.evaluate("(() => { const c = document.querySelector('.workshop-canvas'); return c.width * c.height; })()")
        assert pixels <= 180_000, pixels
    finally:
        context.close()


# ---------------------------------------------------------------- framing (the room right of the hero text)

def rect(page, selector):
    return page.eval_on_selector(selector, "e => { const r = e.getBoundingClientRect(); return { left: r.left, right: r.right, top: r.top, bottom: r.bottom, width: r.width, height: r.height }; }")


def room_rect(page):
    """The room's outline on screen, in page pixels."""
    return page.evaluate("""() => { const root = document.getElementById('workshop'), r = root.__workshop.roomRect(), b = root.getBoundingClientRect();
                                    return { left: b.left + r.left, right: b.left + r.right, top: b.top + r.top, bottom: b.top + r.bottom }; }""")


@pytest.mark.parametrize("size", [(1440, 900), (1280, 800), (2560, 1440), (1920, 1080)])
def test_no_hotspot_sits_under_the_hero_text(page, site_url, size):
    page.set_viewport_size({"width": size[0], "height": size[1]})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    wait_live(page)
    copy = rect(page, ".hero-copy")
    assert room_rect(page)["left"] >= copy["right"], (room_rect(page), copy)
    for p in page.evaluate("document.getElementById('workshop').__workshop.pins()"):
        x, _ = pin(page, p["key"])
        assert x > copy["right"], p


def test_room_fills_the_height_of_a_wide_screen(page, site_url):
    page.set_viewport_size({"width": 2560, "height": 1440})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    wait_live(page)
    room, stage = room_rect(page), rect(page, "#workshop")
    assert room["bottom"] - room["top"] >= 0.85 * stage["height"], (room, stage)
    assert room["top"] >= stage["top"] - 2 and room["bottom"] <= stage["bottom"] + 2


def test_focused_object_is_centred_right_of_its_card(page, site_url):
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    wait_live(page)
    open_card(page, "stent")
    page.wait_for_timeout(2000)
    stage, card = rect(page, "#workshop"), rect(page, ".ws-card")
    p = page.evaluate("document.getElementById('workshop').__workshop.project('stent')")
    want = (card["right"] - stage["left"] + stage["width"]) / 2
    assert abs(p["x"] - want) <= 0.06 * stage["width"], (p, want)
    assert stage["left"] + p["x"] > card["right"]


def test_held_entrance_waits_for_start(page, site_url):
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + "/?debug=workshop&hold=1", wait_until="load")
    page.wait_for_timeout(1500)
    # held: nothing is drawn (the first frame stalls the main thread) until firstFrame()
    assert page.evaluate("document.getElementById('workshop').dataset.state") != "live"
    page.evaluate("mkWorkshop.firstFrame()")
    page.wait_for_selector(LIVE, timeout=30000)
    page.wait_for_timeout(2000)
    assert page.evaluate("document.getElementById('workshop').__workshop.introT()") == 0
    page.evaluate("mkWorkshop.startIntro()")
    page.wait_for_timeout(2500)
    assert page.evaluate("document.getElementById('workshop').__workshop.introT()") > 2.2


def test_paused_scene_draws_no_frames(page, site_url):
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    wait_live(page, settle=500)
    page.evaluate("mkWorkshop.setPaused(true)")
    page.wait_for_timeout(200)
    before = page.evaluate("document.getElementById('workshop').__workshop.frames()")
    page.wait_for_timeout(500)
    assert page.evaluate("document.getElementById('workshop').__workshop.frames()") == before
    page.evaluate("mkWorkshop.setPaused(false)")
    page.wait_for_timeout(500)
    assert page.evaluate("document.getElementById('workshop').__workshop.frames()") > before


def test_settled_resolves_live(page, site_url):
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    assert page.evaluate("mkWorkshop.settled()") == "live"


def test_held_scene_stops_drawing_once_live(page, site_url):
    """While the homepage intro plays over it, the hidden scene draws its first frames and then waits."""
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + "/?debug=workshop&hold=1", wait_until="load")
    page.evaluate("mkWorkshop.firstFrame()")
    page.wait_for_selector(LIVE, timeout=30000)
    page.wait_for_timeout(300)
    frames = "document.getElementById('workshop').__workshop.frames()"
    before = page.evaluate(frames)
    page.wait_for_timeout(600)
    assert page.evaluate(frames) == before
    page.evaluate("mkWorkshop.startIntro()")
    page.wait_for_timeout(500)
    assert page.evaluate(frames) > before
