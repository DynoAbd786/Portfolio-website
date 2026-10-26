def test_home_loads_without_errors(page, page_errors, site_url):
    page.goto(site_url + "/", wait_until="load")
    page.wait_for_timeout(1000)
    assert page_errors == []
