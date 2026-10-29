"""The Ask AI panel: one header (outside the frame) with the panel's controls, the assistant inside
its frame in the site's look. Nothing here calls the model."""


def open_chat(page, site_url):
    page.goto(site_url + "/", wait_until="load")
    page.click("header button[onclick='mkChat.toggle()']")
    frame = page.frame_locator("#chat-slot iframe")
    frame.locator(".input-area input").wait_for(timeout=30000)
    return frame


def test_one_header_and_no_model_choice(page, site_url):
    frame = open_chat(page, site_url)
    assert frame.locator(".chat-header").count() == 0
    assert frame.locator("select").count() == 0
    header = page.inner_text("#chat-panel .chat-panel-header")
    assert "Ask AI" in header and "DeepSeek" in header


def test_greeting_is_plain(page, site_url):
    frame = open_chat(page, site_url)
    greeting = frame.locator(".message-wrapper.ai .message-content").first.inner_text()
    assert "Muhammad" in greeting
    assert not any(ord(c) > 0x2000 for c in greeting)  # no emoji


def test_new_chat_from_the_header_starts_over(page, site_url):
    frame = open_chat(page, site_url)
    frame.locator(".input-area input").fill("hello")  # typed but not sent
    page.evaluate("localStorage.setItem('probe', '1')")
    page.click("#chat-panel button[aria-label='New chat']")
    frame.locator(".message-wrapper.ai").first.wait_for()
    assert frame.locator(".message-wrapper").count() == 1


def test_history_button_opens_past_chats(page, site_url):
    frame = open_chat(page, site_url)
    page.click("#chat-panel button[aria-label='Past chats']")
    frame.locator(".history-sidebar").wait_for(timeout=3000)
