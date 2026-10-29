"""Renders the homepage workshop's still image and prints its link pins.

Run the site first, then:
    uv run --with playwright python tools/render-workshop-still.py http://127.0.0.1:5199

Writes website/wwwroot/images/workshop/still.webp (1600x1000) and still@2x.webp (3200x2000),
and prints PinX/PinY values to paste into website/Data/HomeContent.cs and the --still-room
value for css/app.css.
"""
import base64
import sys
from pathlib import Path

from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "website" / "wwwroot" / "images" / "workshop"


def main(base):
    OUT.mkdir(parents=True, exist_ok=True)
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome")
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        page.goto(base.rstrip("/") + "/?debug=workshop&still=1", wait_until="load")
        page.wait_for_selector("#workshop[data-state=live]", timeout=60000)
        page.evaluate("document.fonts.ready")
        page.wait_for_timeout(1500)
        for name, (w, h) in {"still.webp": (1600, 1000), "still@2x.webp": (3200, 2000)}.items():
            url = page.evaluate(f"document.getElementById('workshop').__workshop.snapshot({w}, {h})")
            assert url.startswith("data:image/webp"), "the browser could not encode WebP"
            (OUT / name).write_bytes(base64.b64decode(url.split(",", 1)[1]))
            print(f"wrote {OUT / name}")
        pins = page.evaluate("document.getElementById('workshop').__workshop.stillPins()")
        still_room = page.evaluate("document.getElementById('workshop').__workshop.stillRoom()")
        browser.close()
    print(f"\n/* css/app.css, #hero */ --still-room: {still_room};")
    print("\n// PinX, PinY for HomeContent.Workshop")
    for pin in pins:
        print(f'{pin["key"]}: {pin["x"]}, {pin["y"]}')


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "http://127.0.0.1:5199")
