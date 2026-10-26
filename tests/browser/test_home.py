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
    page.wait_for_timeout(1000)
    assert _in_viewport(page, "#work")


def test_chat_styles_do_not_leak_into_contact_form(page, site_url):
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_selector(".input-area input", state="attached", timeout=20000)  # chat styles loaded
    box = page.locator("#agree").bounding_box()
    assert box["width"] < 30
    assert page.eval_on_selector("#name", "el => getComputedStyle(el).borderTopWidth") == "1px"
