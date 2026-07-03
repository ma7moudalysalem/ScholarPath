# Power BI Row-Level Security (RLS) Setup

**PB-015 T-012 / T-013** — Configure RLS roles in Power BI Desktop and verify via impersonation.

---

> ### ⚠️ Critical: RLS role names must match the JWT role verbatim
>
> `PowerBiService.GetEmbedTokenAsync` sends **`roles = new[] { activeRole }`** — the
> caller's active JWT role string, unmodified. So the RLS role defined in the `.pbix`
> **must be named exactly** `Consultant`, `Student`, or `Company` (whatever the raw
> role string is). There is **no `…Scope` suffix** anywhere in the code — an earlier
> revision of this doc invented `ConsultantScope`/`StudentScope`/`CompanyScope`, which
> the backend never sends.
>
> **This is a correctness gate, not a cosmetic one.** If the published workspace was
> built to the old doc (roles named `…Scope`), the embed token references a role that
> does not exist → Power BI **either rejects `GenerateToken` or applies no row filter**,
> which is a **cross-tenant data leak**. Before the defense, open the published `.pbix`
> (Modeling → Manage Roles) and confirm the three role names are `Consultant`,
> `Student`, `Company`. Rename them if they still carry the `…Scope` suffix.
>
> _Note on the role rename:_ on `main` the third role is `Company`. If/when the
> `Company → ScholarshipProvider` rename lands, the RLS role must be renamed to match,
> since the code passes the role string through untouched.

---

## Overview

ScholarPath uses Power BI **Row-Level Security** to ensure every user only sees data
that belongs to them or their scope. The backend passes the caller's active role
straight through as the RLS role name:

| JWT Role        | RLS Role in Power BI | Data Scope                                                  |
|-----------------|----------------------|-------------------------------------------------------------|
| `Admin`         | *(no filter)*        | All data — no row-level filter applied                      |
| `SuperAdmin`    | *(no filter)*        | All data                                                    |
| `Consultant`    | `Consultant`         | Rows where `ConsultantEmail = USERNAME()` or `ALL` in agg views |
| `Student`       | `Student`            | Rows where `StudentEmail = USERNAME()` or `ALL` in agg views  |
| `Company`       | `Company`            | Rows where `CompanyEmail = USERNAME()` or `ALL` in agg views  |

`USERNAME()` in Power BI RLS resolves to the `EffectiveIdentity.username` field
sent in the embed token request. The backend (`PowerBiService.cs`) sets this to
the user's email address, and the role to the caller's active role verbatim.

---

## Step-by-step: Configure in Power BI Desktop

1. **Open the `.pbix` file** for the report you want to secure.

2. **Modeling tab → Manage Roles → Create roles:**

   **Role: `Consultant`**
   ```dax
   -- DimUser table
   [Email] = USERNAME()
   
   -- FactBooking table (consultant sees their own bookings)
   [ConsultantEmail] = USERNAME()
   ```

   **Role: `Student`**
   ```dax
   -- DimUser table
   [Email] = USERNAME()
   
   -- FactApplication table
   [StudentEmail] = USERNAME()
   
   -- FactBooking table (student sees their own bookings)
   [StudentEmail] = USERNAME()
   ```

   **Role: `Company`**
   ```dax
   -- DimUser table
   [Email] = USERNAME()
   
   -- Scholarship table (company sees their own scholarships)
   [CompanyEmail] = USERNAME()
   ```

   Admin / SuperAdmin: **no role defined** — the embed token is requested without
   an `EffectiveIdentity`, so Power BI applies no row filter.

3. **Test in Desktop:** Modeling tab → View As → select a role + enter a test
   email address → verify the visuals show only that user's data.

4. **Publish** the updated `.pbix` to the Power BI workspace.

---

## How the Backend Requests Embed Tokens with RLS

`PowerBiService.cs` (`GetEmbedTokenAsync`) sets the effective identity:

```csharp
// GenerateToken request body (identities sent only when a DatasetId is configured):
new
{
    username = userEmail,          // e.g. "student@example.com"
    roles    = new[] { activeRole }, // the caller's active role, verbatim
    datasets = new[] { datasetId },
}
```

`activeRole` is the caller's current role string (`Consultant` / `Student` /
`Company`). For Admin / SuperAdmin the backend does not request analytics through
an effective identity that filters rows, so no RLS role is applied. There is no
role-name remapping — whatever role name the token carries **is** the RLS role
name Power BI looks up.

---

## T-013 Verification — Impersonation Tests

Run `analytics/powerbi/test-rls-impersonation.py` against the staging API to
verify that four different JWT roles receive correctly-scoped embed tokens.

```bash
cd analytics/powerbi
pip install requests

E2E_BASE_URL=https://your-staging-api.azurewebsites.net \
E2E_STUDENT_TOKEN=<jwt>      \
E2E_CONSULTANT_TOKEN=<jwt>   \
E2E_COMPANY_TOKEN=<jwt>      \
E2E_ADMIN_TOKEN=<jwt>        \
python test-rls-impersonation.py
```

Expected output (the effective-identity role is opaque inside the embed token, so
the script asserts only HTTP 200 + `isConfigured` + a non-null token per role):
```
[PASS] Admin token: HTTP 200, isConfigured=True
[PASS] Consultant token: HTTP 200, isConfigured=True
[PASS] Student token: HTTP 200, isConfigured=True
[PASS] Company token: HTTP 200, isConfigured=True
```

> The `expect_roles` labels inside `test-rls-impersonation.py` are cosmetic metadata
> only — the script cannot read the role out of the opaque embed token, so it does
> **not** verify the role name. Confirming the RLS role names match the JWT roles is
> the manual `.pbix` check described in the banner at the top of this doc.

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| "Report not found" (404) | `ReportIds` in `appsettings.json` not set | Fill in the Power BI report GUIDs after publishing |
| "Token generation failed" | Service principal not a Workspace Member | Add SP to workspace with Contributor role |
| Empty data after approval | RLS role name mismatch | Role names in `.pbix` must match the JWT role **exactly**: `Consultant`, `Student`, `Company` (no `…Scope` suffix — see the banner at the top) |
| Admin sees filtered data | `EffectiveIdentity` sent for Admin | Check `PowerBiService.cs` — Admin should pass `roles: []` |
