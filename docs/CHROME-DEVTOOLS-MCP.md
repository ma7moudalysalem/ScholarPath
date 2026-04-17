# Chrome DevTools MCP

The [chrome-devtools-mcp](https://github.com/ChromeDevTools/chrome-devtools-mcp) server lets any MCP-capable AI coding agent drive a real Chrome instance: take screenshots, inspect the DOM, read network/console, run Lighthouse traces, and click/fill forms. We use it for **visual QA** — reviewing UI work against `docs/DESIGN.md` tokens.

## Install (one-time)

Pick whichever coding assistant your editor supports; the command is the same under the hood:

```bash
# Copilot, Cursor, Windsurf, Codex CLI, and any other MCP-capable client
# See each tool's MCP docs for the exact config snippet.
npx chrome-devtools-mcp@latest
```

### Requirements
- Node 20.19+ (we ship 22 LTS — fine).
- Current stable Chrome.

## Basic usage

1. Start the dev stack:
   ```bash
   docker compose up -d sqlserver redis mailhog
   cd server && dotnet run --project src/ScholarPath.API &
   cd client && npm run dev
   ```
2. Connect your AI agent to the MCP server.
3. Ask the agent to review a page:
   > Open http://localhost:5173, take a screenshot in both light and dark mode, and tell me if the homepage matches the spacing rhythm in docs/DESIGN.md.

The agent can now:
- Navigate, click, fill
- Take screenshots (full page or viewport)
- Read console + network
- Run performance traces
- Inspect element computed styles

## Attach to an existing Chrome

Useful when you want the agent to see your actual auth state:

1. Start Chrome with remote debugging:
   ```bash
   # Windows
   "C:\Program Files\Google\Chrome\Application\chrome.exe" --remote-debugging-port=9222 --user-data-dir="C:\chrome-mcp-profile"

   # macOS
   /Applications/Google\ Chrome.app/Contents/MacOS/Google\ Chrome --remote-debugging-port=9222 --user-data-dir=/tmp/chrome-mcp-profile
   ```
2. Point the MCP server at the running Chrome via `--browser-url=http://127.0.0.1:9222`.

## Slim mode

For short sessions, add `--slim --headless` to skip heavyweight Lighthouse instrumentation:

```bash
npx chrome-devtools-mcp@latest --slim --headless
```

## Workflows we commit to

### Design QA on a PR
> Take a screenshot of each page under `/student/*`, both in EN and AR RTL, both light and dark. Compare against the design tokens (pill CTAs 980px radius, shadow-md on cards, letter-spacing -0.02em on ≥28px headlines). Output a bullet list of deviations.

### Accessibility sniff test
> Open the Login page. Tab through every interactive element. Verify focus ring is `box-shadow: 0 0 0 3px rgb(37 99 235 / 0.35)`. Flag anything without a visible focus state.

### Network regression
> Perform a fresh sign-up and log every request. Flag any 4xx response or any request that doesn't include the `Authorization` header after login.

### RTL visual check
> Toggle the language switcher to Arabic. Screenshot every page. Check: `dir="rtl"`, labels flipped to the correct side, icons mirrored where they should be.

## Do not

- Let the MCP agent touch a production URL (keep it at `localhost`).
- Paste real Stripe keys into the attached Chrome session.
- Commit screenshots to the repo; save them to `.screenshots/` which is gitignored.

## Troubleshooting

| Symptom                                 | Fix                                                                   |
|-----------------------------------------|-----------------------------------------------------------------------|
| `Chrome not found`                      | Install current stable Chrome. Add its path to `PATH`.                |
| `ECONNREFUSED :9222`                    | Chrome wasn't started with `--remote-debugging-port`.                 |
| Empty screenshots                       | Viewport too small — set `viewport: 1440x900` in the MCP invocation.  |
| "Cannot navigate: about:blank"          | Page hasn't loaded; use `waitForSelector` before the screenshot call. |
