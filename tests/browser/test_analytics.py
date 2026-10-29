"""Analytics events from the browser (mkTrack in site.js, sent to Umami's tracker). The real Umami
script only loads on the live site, so these tests stand in a recording `window.umami`."""
import json

RECORD = """window.__events = []; window.umami = { track: (name, data) => window.__events.push([name, data || null]) };
  // let tracked links be clicked without leaving the page (PDFs and other sites open new tabs)
  addEventListener('click', (e) => { const a = e.target.closest && e.target.closest('a[target=_blank], a[href^=mailto], a[href*=".pdf"], a[href^="http"]'); if (a) e.preventDefault(); });"""


def events(page, name=None):
    got = page.evaluate("window.__events")
    return [d for n, d in got if n == name] if name else got


def wait_event(page, name, timeout=5000):
    page.wait_for_function(f"window.__events.some(e => e[0] === {json.dumps(name)})", timeout=timeout)
    return events(page, name)


def test_cv_downloads_say_where_they_came_from(page, site_url):
    page.add_init_script(RECORD)
    page.goto(site_url + "/projects", wait_until="load")
    # the header link is only an icon (Font Awesome, from a CDN): dispatch the click so the test doesn't
    # depend on the icon font arriving
    page.locator("header a[aria-label='Download CV']").first.dispatch_event("click")
    page.click("footer a[href*='Muhammad-Kashif-Khan-CV.pdf']")
    assert [e["from"] for e in events(page, "cv-download")] == ["header", "footer"]


def test_report_downloads_name_the_file(page, site_url):
    page.add_init_script(RECORD)
    page.goto(site_url + "/projects/business-plan", wait_until="load")
    page.click("a[href='/docs/project-synapse-business-plan.pdf']")
    assert events(page, "pdf-download") == [{"file": "project-synapse-business-plan.pdf", "page": "/projects/business-plan"}]


def test_links_to_other_sites_and_email_are_counted(page, site_url):
    page.add_init_script(RECORD)
    page.goto(site_url + "/about", wait_until="load")
    page.click("footer a[href*='linkedin.com']")
    page.click("footer a[href^='mailto:']")
    assert events(page, "outbound")[0]["to"] == "linkedin.com"
    assert events(page, "email-click") == [{"page": "/about"}]


def test_opening_a_project_says_from_where(page, site_url):
    page.add_init_script(RECORD)
    page.goto(site_url + "/projects", wait_until="load")
    page.click("main a[href='/projects/hireforge']")
    page.wait_for_url("**/projects/hireforge")
    assert events(page, "project-open")[0] == {"project": "hireforge", "from": "/projects"}


def test_opening_ask_ai_and_asking_are_counted_with_the_question(page, site_url):
    page.add_init_script(RECORD)
    page.goto(site_url + "/projects", wait_until="load")
    page.click("header button[onclick='mkChat.toggle()']")
    assert events(page, "ask-ai-open") == [{"page": "/projects"}]
    frame = page.frame_locator("#chat-slot iframe")
    frame.locator(".input-area input").wait_for(timeout=30000)
    frame.locator(".input-area input").fill("What is HireForge?")
    frame.locator(".send-btn").click()
    assert wait_event(page, "ask-ai-question", 10000) == [{"question": "What is HireForge?", "page": "/projects"}]


def test_homepage_sections_reached_are_counted_once(page, site_url):
    page.add_init_script(RECORD)
    page.goto(site_url + "/", wait_until="load")
    for section in ("#work", "#building", "#experience", "#contact"):
        page.locator(section).scroll_into_view_if_needed()
        page.wait_for_timeout(300)
    page.locator("#work").scroll_into_view_if_needed()
    page.wait_for_timeout(300)
    assert [e["section"] for e in events(page, "home-section")] == ["work", "building", "experience", "contact"]


def test_a_scene_that_cannot_run_is_counted(page, site_url):
    page.add_init_script(RECORD)
    page.route("**/js/workshop/workshop*.js", lambda route: route.abort())
    page.goto(site_url + "/", wait_until="load")
    assert wait_event(page, "scene-fallback", 15000) == [{"reason": "failed"}]


def test_objects_opened_in_the_room_are_counted(page, site_url):
    page.add_init_script(RECORD)
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(site_url + "/?debug=workshop", wait_until="load")
    page.wait_for_selector("#workshop[data-state=live]", timeout=30000)
    page.wait_for_timeout(3000)  # the entrance has played
    page.evaluate("document.querySelector('.workshop-tour button, .ws-tour button')?.click()")
    page.wait_for_selector(".ws-card.open", timeout=5000)
    assert events(page, "room-object")[0]["object"]
