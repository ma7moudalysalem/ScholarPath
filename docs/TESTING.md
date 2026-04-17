# Testing

Target coverage: **≥70%** across unit + integration. Enforced by CI.

## Test pyramid

```
              ┌─────────────┐
              │  Playwright │   ~10 specs — critical user journeys
              │  (E2E)      │   register, login, apply, book, chat
              └─────────────┘
           ┌─────────────────────┐
           │   Integration       │   ~1 per controller + hub
           │  (Testcontainers)   │   real SQL + real DI
           └─────────────────────┘
       ┌───────────────────────────────┐
       │          Unit                  │  ~3 per handler
       │  (xUnit + FluentA + NSubstitute│  logic branches + edge cases
       └───────────────────────────────┘
```

## Backend

### Unit (`tests/ScholarPath.UnitTests`)
- Pure handler logic only.
- Mock `IApplicationDbContext`, `IStripeService`, etc. with NSubstitute.
- Bogus for fake data. Example:
  ```csharp
  var user = new Faker<ApplicationUser>()
      .RuleFor(u => u.Email, f => f.Internet.Email())
      .RuleFor(u => u.FirstName, f => f.Name.FirstName())
      .Generate();
  ```
- FluentAssertions for expressive checks (`result.Should().BeOfType<Created>()`).

### Integration (`tests/ScholarPath.IntegrationTests`)
- `WebApplicationFactory<Program>` hosts the full API in memory.
- `Testcontainers.MsSql` spins up a real SQL Server per test class.
- `Respawn` resets DB between tests without full teardown.
- Auth via `TestAuthenticationHandler` (pre-registered test user with known tokens).
- One spec per public endpoint — verify schema, status, and side-effects.

Example structure:
```csharp
public class AuthFlowTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Register_Login_Refresh_Logout_FullRoundTrip()
    {
        var http = fixture.CreateAuthenticatedClient();
        var registered = await http.PostAsJsonAsync("/api/auth/register", new { /* ... */ });
        registered.StatusCode.Should().Be(HttpStatusCode.OK);
        // ... assert user row created, tokens returned, etc.
    }
}
```

### Coverage
- `coverlet.collector` emits `coverage.cobertura.xml` per test run.
- CI threshold gate (roadmap): `coverlet msbuild` with `/p:Threshold=70`.

## Frontend

### Unit + component (Vitest + Testing Library)
- `happy-dom` as the DOM (faster than jsdom).
- Files colocated under `client/src/test/` or `__tests__/` next to components.
- `@testing-library/user-event` for interactions — never `fireEvent.click` on a button that has a label.

Example:
```ts
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Login } from "@/pages/auth/Login";

test("password field validates on blur", async () => {
  render(<Login />);
  const pwd = screen.getByLabelText(/password/i);
  await userEvent.type(pwd, "short");
  await userEvent.tab();
  expect(screen.getByText(/at least 8 characters/i)).toBeVisible();
});
```

### E2E (Playwright)
- Suite under `client/src/test/e2e/`.
- Config: Chromium + Firefox + mobile Pixel 7.
- `webServer` auto-starts `npm run dev` on CI.

Smoke specs we keep green on every PR:
- `home renders + language toggles direction`
- `login form renders with all fields`
- Roadmap:
  - `register → onboarding → student dashboard renders`
  - `consultant booking happy path with Stripe test card`
  - `admin approves a company onboarding request`

### Visual QA
See `docs/CHROME-DEVTOOLS-MCP.md` — connects an AI coding agent to a headless Chrome for screenshots + DOM/network inspection + design-token comparison against `docs/DESIGN.md`.

## Naming conventions

- `<Command>_<Scenario>_<Outcome>` for unit tests: `RegisterCommand_DuplicateEmail_ReturnsConflict`.
- `should <verb> <outcome>` for TS component tests: `"should disable submit while pending"`.

## What NOT to test

- Third-party library behaviour (MediatR, EF Core, axios).
- UI snapshots for components with heavy animation — use accessibility queries instead.
- Exact wording of error messages — match by role/aria or by substring.

## Running locally

```bash
# Backend
cd server
dotnet test                                 # all
dotnet test --filter Category=Unit          # fast lane
dotnet test --collect:"XPlat Code Coverage" # with coverage

# Frontend
cd client
npm run test                                # Vitest unit
npm run test:watch                          # interactive
npm run test:e2e                            # Playwright (boots dev server)
npm run test:e2e:install                    # first-time: install browsers
```

## CI behaviour

- On every PR: unit + integration + typecheck + lint + build.
- On `main` push: same + deploy-ready artifact upload.
- Failing tests block merge (branch protection).
- Coverage report published as a PR comment (roadmap — add `codecov-action` once paid plan is decided).
