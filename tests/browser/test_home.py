def _in_viewport(page, selector):
    return page.evaluate(
        """s => { const r = document.querySelector(s).getBoundingClientRect();
                  return r.top < innerHeight && r.bottom > 0; }""",
        selector,
    )


def test_anchor_from_other_page_lands_on_section(page, site_url):
    page.goto(site_url + "/projects", wait_until="load")
    page.click("header nav a[href='/#contact']")
    page.wait_for_url("**/#contact")
    page.wait_for_timeout(1500)
    assert _in_viewport(page, "#contact")


def test_see_my_work_scrolls_to_work(page, site_url):
    page.goto(site_url + "/", wait_until="load")
    page.click("main a[href='#work']")
    # a smooth scroll: wait for it to arrive rather than a fixed time (slow under a loaded machine)
    page.wait_for_function("() => { const r = document.querySelector('#work').getBoundingClientRect(); return r.top < innerHeight && r.bottom > 0; }", timeout=4000)
    assert _in_viewport(page, "#work")


def test_chat_styles_do_not_leak_into_contact_form(page, site_url):
    page.goto(site_url + "/", wait_until="load")
    box = page.locator("#agree").bounding_box()
    assert box["width"] < 30
    assert page.eval_on_selector("#name", "el => getComputedStyle(el).borderTopWidth") == "1px"


def _top(page, selector):
    return page.evaluate("s => document.querySelector(s).getBoundingClientRect().top + scrollY", selector)


def test_phone_sections_do_not_move_when_the_scene_starts(page, site_url):
    page.set_viewport_size({"width": 390, "height": 844})
    page.goto(site_url + "/", wait_until="domcontentloaded")
    before = _top(page, "#work")
    page.wait_for_selector("#workshop[data-state=live]", timeout=30000)
    page.wait_for_timeout(500)
    assert _top(page, "#work") == before


def test_phone_anchor_stays_on_section_while_the_scene_loads(page, site_url):
    page.set_viewport_size({"width": 390, "height": 844})
    for anchor in ("#work", "#contact"):
        page.goto(site_url + "/" + anchor, wait_until="load")
        page.wait_for_timeout(4000)  # the scene loads (or waits, off screen) above the section
        assert abs(page.evaluate("s => document.querySelector(s).getBoundingClientRect().top", anchor)) < 120, anchor


def test_phone_room_links_are_one_swipeable_row(page, site_url):
    page.set_viewport_size({"width": 412, "height": 915})
    page.goto(site_url + "/", wait_until="load")
    box = page.locator(".workshop-links").bounding_box()
    assert box["height"] < 60, box
    assert page.evaluate("(() => { const l = document.querySelector('.workshop-links'); return l.scrollWidth > l.clientWidth && getComputedStyle(l).overflowX === 'auto'; })()")


def test_phone_sections_are_not_padded_like_desktop(page, site_url):
    page.set_viewport_size({"width": 412, "height": 915})
    page.goto(site_url + "/", wait_until="load")
    for section in ("#work", "#building", "#experience", "#contact"):
        assert page.evaluate("s => parseFloat(getComputedStyle(document.querySelector(s)).paddingTop)", section) <= 56, section


def test_runtime_loads_only_when_the_chat_opens(page, site_url):
    """The .NET WebAssembly runtime (about 3 MB) is fetched only once someone opens the assistant."""
    wasm = []
    page.on("request", lambda r: wasm.append(r.url) if r.url.endswith(".wasm") else None)
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_timeout(2500)
    assert wasm == []
    page.click("header button[onclick='mkChat.toggle()']")
    frame = page.frame_locator("#chat-slot iframe")
    frame.locator(".input-area input").wait_for(timeout=30000)
    assert wasm, "opening the chat should load the runtime"


def test_contact_form_works_before_any_runtime(page, site_url):
    page.goto(site_url + "/", wait_until="load")
    assert page.is_enabled("#contact button[type=submit]")
    assert "Send Message" in page.inner_text("#contact button[type=submit]")


def test_chat_shows_one_model_and_no_choice(page, site_url):
    page.goto(site_url + "/", wait_until="load")
    page.click("header button[onclick='mkChat.toggle()']")
    frame = page.frame_locator("#chat-slot iframe")
    frame.locator(".input-area input").wait_for(timeout=30000)
    assert frame.locator(".model-name").inner_text() == "DeepSeek Flash"
    assert frame.locator("select").count() == 0
    assert "Powered by DeepSeek" in page.inner_text("#chat-panel")
