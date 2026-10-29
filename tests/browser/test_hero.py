"""The hero's layout once the name has landed: a full-height room behind the text on desktop,
name, room, then text on phones, and the page sliding over the hero."""
import pytest

HOME = "/?debug=workshop"


def box(page, selector):
    return page.eval_on_selector(selector, "e => { const r = e.getBoundingClientRect(); return { left: r.left, right: r.right, top: r.top, bottom: r.bottom, width: r.width, height: r.height }; }")


def test_desktop_room_fills_the_hero_height(page, site_url):
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + HOME, wait_until="load")
    stage = box(page, "#workshop .workshop-canvas")
    assert stage["height"] >= 900 - 64 - 2
    assert stage["width"] == page.evaluate("innerWidth") - page.evaluate("innerWidth - document.documentElement.clientWidth")


def test_desktop_still_is_centred_on_the_frame(page, site_url):
    # the still picture is wider than the room in it; whether the room clears the text is checked
    # with the pins in test_workshop.py
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + HOME, wait_until="load")
    still, stage = box(page, "#workshop .workshop-still"), box(page, "#workshop")
    fx = float(page.evaluate("getComputedStyle(document.getElementById('hero')).getPropertyValue('--frame-x')"))
    assert still["height"] <= stage["height"] + 1
    assert abs((still["left"] + still["right"]) / 2 - fx * stage["width"]) <= 2


@pytest.mark.parametrize("width", [1100, 1920])
def test_desktop_header_spans_the_hero(page, site_url, width):
    # on the homepage the header is as wide as the hero: the logo over the name, the icons at the right
    page.set_viewport_size({"width": width, "height": 900})
    page.goto(site_url + HOME, wait_until="load")
    logo, h1 = box(page, "header nav > a"), box(page, "#hero h1")
    links = box(page, "header nav > div")
    assert abs(logo["left"] - h1["left"]) <= 2
    assert abs(page.evaluate("document.documentElement.clientWidth") - links["right"] - h1["left"]) <= 2


@pytest.mark.parametrize("path", ["/projects", "/projects/hireforge", "/about"])
def test_header_is_the_same_on_every_page(page, site_url, path):
    page.set_viewport_size({"width": 1920, "height": 900})
    page.goto(site_url + HOME, wait_until="load")
    home = box(page, "header nav > a")
    page.goto(site_url + path, wait_until="load")
    assert abs(box(page, "header nav > a")["left"] - home["left"]) <= 1


@pytest.mark.parametrize("size", [(390, 844), (844, 390)])
def test_phone_order_is_name_room_text(page, site_url, size):
    page.set_viewport_size({"width": size[0], "height": size[1]})
    page.goto(site_url + HOME, wait_until="load")
    # the row of room links is only shown under 640px, as before
    parts = [".hero-head", "#workshop", ".hero-body"] + ([".workshop-links"] if size[0] < 640 else [])
    tops = [box(page, s)["top"] for s in parts]
    assert tops == sorted(tops), tops
    stage = box(page, "#workshop")
    assert abs(stage["width"] - page.evaluate("document.documentElement.clientWidth")) <= 1
    assert abs(stage["width"] * 3 / 4 - stage["height"]) <= 1


def test_page_slides_over_the_hero(page, site_url):
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + HOME, wait_until="load")
    page.evaluate("scrollTo(0, 500)")
    page.wait_for_timeout(100)
    assert box(page, "#hero")["top"] == 64
    assert page.evaluate("!!document.elementFromPoint(innerWidth / 2, innerHeight / 2).closest('.hero-cover')")


def test_get_in_touch_has_the_tracing_light(page, site_url):
    page.goto(site_url + HOME, wait_until="load")
    assert page.eval_on_selector(".hero-buttons .btn-shine", "e => getComputedStyle(e, '::before').animationName") == "btn-shine-trace"


def test_room_dims_as_the_page_covers_it(page, site_url):
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + HOME, wait_until="load")
    page.evaluate("scrollTo(0, document.getElementById('hero').offsetHeight / 2)")
    page.wait_for_timeout(200)
    opacity = float(page.eval_on_selector("#workshop", "e => getComputedStyle(e).opacity"))
    assert 0.6 <= opacity <= 0.75, opacity


def test_fully_covered_scene_stops_drawing(page, site_url):
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + HOME, wait_until="load")
    page.wait_for_selector("#workshop[data-state=live]", timeout=30000)
    frames = "document.getElementById('workshop').__workshop.frames()"
    page.evaluate("scrollTo(0, document.getElementById('hero').offsetHeight + 200)")
    page.wait_for_timeout(300)
    before = page.evaluate(frames)
    page.wait_for_timeout(500)
    assert page.evaluate(frames) == before
    page.evaluate("scrollTo(0, 0)")
    page.wait_for_timeout(500)
    assert page.evaluate(frames) > before
