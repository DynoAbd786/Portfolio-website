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
    assert "MEng thesis" in page.inner_text(".ws-tag")
    page.mouse.click(x, y)
    page.wait_for_selector(".ws-card.open", timeout=3000)
    assert page.is_visible(".ws-card-title")
    assert page.inner_text(".ws-card-title") == "Hybrid 3D/4D reconstruction"
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
    assert canvas["y"] + canvas["height"] <= h1["y"]  # phones: the room comes first
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
    assert page.inner_text(".ws-card-title") == "Hybrid 3D/4D reconstruction"


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
    "synergy": "Synergy", "hireforge": "HireForge", "thesis": "MEng thesis", "stent": "Stent simulation",
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


@pytest.mark.parametrize("tier, at_least", [(0, 2.5), (2, 1.9), (4, 1.3)])
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


def test_phone_room_comes_first_and_fills_the_width(page, site_url):
    page.set_viewport_size({"width": 390, "height": 844})
    page.goto(site_url + "/", wait_until="load")
    stage = page.locator("#workshop").bounding_box()
    copy = page.locator(".hero-copy").bounding_box()
    assert stage["y"] + stage["height"] <= copy["y"]
    assert stage["x"] == 0 and stage["width"] == 390
    assert abs(stage["width"] / stage["height"] - 4 / 3) < 0.02
    links = page.locator(".workshop-links").bounding_box()
    assert stage["y"] < links["y"] < copy["y"]


def test_phone_card_sits_between_room_and_text(page, site_url):
    page.set_viewport_size({"width": 390, "height": 844})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    wait_live(page)
    open_card(page, "thesis")
    card = page.locator(".ws-card").bounding_box()
    stage = page.locator("#workshop").bounding_box()
    copy = page.locator(".hero-copy").bounding_box()
    assert stage["y"] + stage["height"] <= card["y"] < copy["y"]


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
