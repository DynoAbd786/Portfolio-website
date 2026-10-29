"""Interactive pieces of project pages still work after moving onto the shared template."""


def test_medical_gallery_next_works(page, site_url):
    page.goto(site_url + "/projects/medical-simulation", wait_until="load")
    page.wait_for_selector(".gallery-next:not([disabled])", timeout=30000)
    first = page.get_attribute("main img", "src")
    page.click(".gallery-next")
    page.wait_for_function("s => document.querySelector('main img').getAttribute('src') !== s", arg=first, timeout=5000)


def test_gallery_caption_renders_its_markup(page, site_url):
    page.goto(site_url + "/projects/medical-simulation", wait_until="load")
    page.wait_for_selector(".gallery-next:not([disabled])", timeout=30000)
    page.click(".gallery-next")
    page.wait_for_timeout(300)
    assert "<span" not in page.inner_text("main")


# Pages that still scroll sideways on a phone until their batch moves them onto the template
NOT_YET_MIGRATED_OVERFLOW = set()


def test_project_pages_fit_a_phone(page, site_url):
    page.set_viewport_size({"width": 390, "height": 844})
    routes = [p["projectUrl"] for p in page.request.get(site_url + "/api/projects").json()]
    assert len(routes) == 31
    wide = {}
    for route in routes:
        page.goto(site_url + route, wait_until="load")
        width = page.evaluate("document.documentElement.scrollWidth")
        if width > 390:
            wide[route] = width
    assert set(wide) <= NOT_YET_MIGRATED_OVERFLOW, wide
