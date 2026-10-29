import pytest


def _opacity(page, selector):
    return page.eval_on_selector(selector, "el => getComputedStyle(el).opacity")


@pytest.fixture
def no_js_page(browser):
    context = browser.new_context(java_script_enabled=False)
    yield context.new_page()
    context.close()


def test_sections_visible_without_javascript(no_js_page, site_url):
    no_js_page.goto(site_url + "/", wait_until="load")
    assert _opacity(no_js_page, "#work article") == "1"
    assert _opacity(no_js_page, "#stack .reveal") == "1"


def test_sections_reveal_when_scrolled_to(page, site_url):
    page.goto(site_url + "/", wait_until="load")
    page.locator("#experience li").last.scroll_into_view_if_needed()
    page.wait_for_timeout(1200)
    assert _opacity(page, "#experience li:last-child") == "1"
