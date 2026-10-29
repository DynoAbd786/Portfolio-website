"""Moving between pages (Blazor's enhanced navigation) crossfades the content with a view transition;
the header stays put. Nothing animates with reduced motion, and a jump within the page doesn't fade."""
import pytest

# count the view transitions the page starts
COUNT = """(() => { const start = document.startViewTransition?.bind(document); if (!start) return;
  window.__transitions = 0; document.startViewTransition = (cb) => { window.__transitions++; return start(cb); }; })()"""


def test_header_link_fades_to_the_next_page(page, site_url):
    page.add_init_script(COUNT)
    page.goto(site_url + "/projects", wait_until="load")
    page.click("header nav a[href='/about']")
    page.wait_for_url("**/about")
    page.wait_for_function("document.title.startsWith('About')")
    assert page.evaluate("window.__transitions") == 1
    page.wait_for_timeout(600)  # the fade has finished and the new page is usable
    assert page.locator("main h1").is_visible()


def test_project_card_fades_to_its_page(page, site_url):
    page.add_init_script(COUNT)
    page.goto(site_url + "/projects", wait_until="load")
    page.click("a[href='/projects/hireforge']")
    page.wait_for_url("**/projects/hireforge")
    assert page.evaluate("window.__transitions") == 1


def test_no_fade_with_reduced_motion(browser, site_url):
    context = browser.new_context(reduced_motion="reduce")
    page = context.new_page()
    page.add_init_script(COUNT)
    page.goto(site_url + "/projects", wait_until="load")
    page.click("header nav a[href='/about']")
    page.wait_for_url("**/about")
    page.wait_for_function("document.title.startsWith('About')")
    assert page.evaluate("window.__transitions") == 0
    context.close()


def test_a_jump_within_the_page_does_not_fade(page, site_url):
    page.add_init_script(COUNT)
    page.goto(site_url + "/", wait_until="load")
    page.click("header nav a[href='/#contact']")
    page.wait_for_timeout(1200)
    assert page.evaluate("window.__transitions") == 0


def test_sending_the_contact_form_does_not_fade(page, site_url):
    page.add_init_script(COUNT)
    page.goto(site_url + "/#contact", wait_until="load")
    page.fill("#name", "Test"); page.fill("#email", "test@example.com")
    page.select_option("#subject", index=1); page.fill("#message", "Hello")
    page.check("#agree")
    page.click("#contact button[type=submit]")
    page.wait_for_function("document.querySelector('#contact').innerText.includes('message')", timeout=10000)
    page.wait_for_timeout(1000)
    assert page.evaluate("window.__transitions") == 0
