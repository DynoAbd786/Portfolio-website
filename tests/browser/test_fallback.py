"""When the 3D workshop can't run, the homepage shows its still image with link pins."""
import pytest

from conftest import SKIP_INTRO


def _errors(page):
    errors = []
    page.on("pageerror", lambda e: errors.append(str(e)))
    page.on("console", lambda m: errors.append(m.text) if m.type == "error" else None)
    return errors


@pytest.fixture
def no_webgl_page(browser_type, site_url):
    browser = browser_type.launch(channel="chrome", args=["--disable-gpu", "--disable-webgl", "--disable-webgl2"])
    page = browser.new_page(viewport={"width": 1440, "height": 900})
    page.add_init_script(SKIP_INTRO)
    yield page
    browser.close()


def test_no_webgl_shows_still_with_links(no_webgl_page, site_url):
    page = no_webgl_page
    errors = _errors(page)
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_selector("#workshop[data-state=failed]", timeout=15000)
    assert page.is_visible(".workshop-still img")
    pins = page.locator(".workshop-pin")
    assert pins.count() == 11
    for i in range(11):
        assert pins.nth(i).is_visible()
    assert page.get_attribute(".workshop-pin[href='/projects/medical-simulation']", "href") == "/projects/medical-simulation"
    assert page.locator(".workshop-pin", has_text="Stent simulation").get_attribute("href") == "/projects/medical-simulation"
    assert errors == []


def test_no_javascript_shows_still(browser, site_url):
    context = browser.new_context(java_script_enabled=False, viewport={"width": 1440, "height": 900})
    page = context.new_page()
    try:
        page.goto(site_url + "/", wait_until="load")
        assert page.is_visible(".workshop-still img")
        assert page.locator(".workshop-pin:visible").count() == 11
        assert page.is_visible("h1")
    finally:
        context.close()


def test_slow_device_falls_back_to_still(page, site_url):
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    page.wait_for_selector("#workshop[data-state=live]", timeout=30000)
    page.evaluate("document.getElementById('workshop').__workshop.forceFps(10)")
    page.wait_for_selector("#workshop[data-state=still]", timeout=12000)
    assert page.evaluate("mkWorkshop.active") is False
    assert page.is_visible(".workshop-still img")
    assert page.locator(".workshop-pin:visible").count() == 11


def test_still_image_loads(page, site_url):
    response = page.request.get(site_url + "/images/workshop/still.webp")
    assert response.status == 200
    page.goto(site_url + "/", wait_until="load")
    assert page.evaluate("document.querySelector('.workshop-still img').naturalWidth") == 1600


def test_pins_sit_on_their_objects(page, site_url):
    """The still's pins (HomeContent) match where the live scene puts each object."""
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    page.wait_for_selector("#workshop[data-state=live]", timeout=30000)
    page.wait_for_timeout(3500)
    # live pins are % of the stage, the still's are % of the picture centred in it: compare on the page
    pairs = page.evaluate("""() => {
        const root = document.getElementById('workshop'), stage = root.getBoundingClientRect();
        const still = root.querySelector('.workshop-still').getBoundingClientRect();
        const data = Object.fromEntries(JSON.parse(document.getElementById('workshop-data').textContent).map(d => [d.key, d]));
        return root.__workshop.pins().map(p => ({ key: p.key,
            live: [stage.left + p.x / 100 * stage.width, stage.top + p.y / 100 * stage.height],
            still: [still.left + data[p.key].pinX / 100 * still.width, still.top + data[p.key].pinY / 100 * still.height],
            tol: .04 * still.width }));
    }""")
    for p in pairs:
        assert abs(p["live"][0] - p["still"][0]) < p["tol"] and abs(p["live"][1] - p["still"][1]) < p["tol"], p


def test_phone_still_lists_links_instead_of_pins(browser, site_url):
    context = browser.new_context(java_script_enabled=False, viewport={"width": 390, "height": 844})
    page = context.new_page()
    try:
        page.goto(site_url + "/", wait_until="load")
        assert page.locator(".workshop-pin:visible").count() == 0
        links = page.locator(".workshop-links a:visible, .workshop-links button:visible")
        assert links.count() == 11
        assert page.locator(".workshop-links a", has_text="Stent simulation").get_attribute("href") == "/projects/medical-simulation"
    finally:
        context.close()


def test_links_list_shown_on_phones_and_hidden_on_desktop(page, site_url):
    page.set_viewport_size({"width": 390, "height": 844})
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_selector("#workshop[data-state=live]", timeout=30000)
    assert page.locator(".workshop-links a:visible, .workshop-links button:visible").count() == 11
    page.set_viewport_size({"width": 1440, "height": 900})
    assert page.locator(".workshop-links:visible").count() == 0


def test_slow_check_starts_afresh_after_a_pause(page, site_url):
    """A pause (scrolled away, tab hidden) must not let the slow-device check judge on a handful of frames."""
    page.goto(site_url + "/", wait_until="load")
    result = page.evaluate("""async () => {
        const { createQuality, MAX_TIER } = await import('/js/workshop/quality.js');
        const q = createQuality(() => {}, 60);
        q.set(MAX_TIER);
        let t = 0;
        for (; t < 1; t += 1 / 60) q.tooSlow(1 / 60, t);   // a second of smooth frames
        t = 100; q.reset(t);                                // paused, then resumed
        let slow = false;
        for (let i = 0; i < 12; i++, t += .1) slow = q.tooSlow(.1, t) || slow;  // 1.2 s of 10 fps
        return slow;
    }""")
    assert result is False
