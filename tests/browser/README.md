# Browser tests

Playwright tests that run the real site (built and started on port 5199 by `conftest.py`)
in Chrome. They need the .NET 10 SDK on `PATH`, `uv` and an installed Chrome.

From the repository root:

```bash
uv run -q --with pytest --with pytest-playwright pytest tests/browser -q
```
