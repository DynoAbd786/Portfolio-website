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


def test_homelab_status_renders(page, site_url):
    page.goto(site_url + "/projects/home-lab", wait_until="load")
    page.wait_for_selector(".bridge-status", timeout=10000)
    page.wait_for_function("/ONLINE|OFFLINE/.test(document.querySelector('.bridge-status').innerText)", timeout=10000)
