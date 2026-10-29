import os
import subprocess
import time
import urllib.request
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
PORT = 5199
URL = f"http://127.0.0.1:{PORT}"


def _healthy():
    try:
        with urllib.request.urlopen(URL + "/health", timeout=2) as r:
            return r.status == 200
    except OSError:
        return False


@pytest.fixture(scope="session")
def site_url():
    """Builds once and runs the real site on port 5199 for the whole session."""
    if _healthy():
        # Another server would answer the health check and the tests would hit stale files
        raise RuntimeError(f"something is already serving {URL}; stop it first")
    # No launch profile: its applicationUrl would override ASPNETCORE_URLS
    env = {**os.environ, "ASPNETCORE_URLS": URL, "ASPNETCORE_ENVIRONMENT": "Development", "DOTNET_NOLOGO": "1"}
    subprocess.run(["dotnet", "build", "website.api", "-v", "q", "-nologo"], cwd=ROOT, env=env, check=True)
    proc = subprocess.Popen(
        ["dotnet", "run", "--no-build", "--no-launch-profile", "--project", "website.api"],
        cwd=ROOT, env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
    )
    try:
        deadline = time.time() + 90
        while not _healthy():
            if proc.poll() is not None:
                raise RuntimeError("site exited during startup")
            if time.time() > deadline:
                raise TimeoutError("site did not become healthy within 90 s")
            time.sleep(0.5)
        yield URL
    finally:
        proc.terminate()
        try:
            proc.wait(10)
        except subprocess.TimeoutExpired:
            proc.kill()


@pytest.fixture(scope="session")
def browser_type_launch_args(browser_type_launch_args):
    return {**browser_type_launch_args, "channel": "chrome"}


@pytest.fixture
def page_errors(page):
    """Uncaught exceptions and console errors seen by the page."""
    errors = []
    page.on("pageerror", lambda e: errors.append(str(e)))
    page.on("console", lambda m: errors.append(m.text) if m.type == "error" else None)
    return errors


# The homepage intro (js/intro) plays on a tab's first load of /. Most tests are about what comes
# after it, so pages start as if the intro had been seen; tests of the intro itself are marked intro.
SKIP_INTRO = "try { sessionStorage.setItem('mk-intro-off', '1'); } catch (e) {}"


def pytest_configure(config):
    config.addinivalue_line("markers", "intro: runs with the homepage intro (conftest skips it otherwise)")


@pytest.fixture
def context(context, request):
    if not request.node.get_closest_marker("intro"):
        context.add_init_script(SKIP_INTRO)
    return context
