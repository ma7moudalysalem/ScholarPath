# Power BI Row-Level Security (RLS) Setup

**PB-015 T-012 / T-013** — Configure RLS roles in Power BI Desktop and verify via impersonation.

---

## Overview

ScholarPath uses Power BI **Row-Level Security** to ensure every user only sees data
that belongs to them or their scope:

| JWT Role        | RLS Role in Power BI | Data Scope                                                  |
|-----------------|----------------------|-------------------------------------------------------------|
| `Admin`         | *(no filter)*        | All data — no row-level filter applied                      |
| `SuperAdmin`    | *(no filter)*        | All data                                                    |
| `Consultant`    | `ConsultantScope`    | Rows where `ConsultantId = USERNAME()` or `ALL` in agg views |
| `Student`       | `StudentScope`       | Rows where `StudentId = USERNAME()` or `ALL` in agg views  |
| `Company`       | `CompanyScope`       | Rows where `CompanyId = USERNAME()` or `ALL` in agg views  |

`USERNAME()` in Power BI RLS resolves to the `EffectiveIdentity.username` field
sent in the embed token request. The backend (`PowerBiService.cs`) sets this to
the user's email address and role.

---

## Step-by-step: Configure in Power BI Desktop

1. **Open the `.pbix` file** for the report you want to secure.

2. **Modeling tab → Manage Roles → Create roles:**

   **Role: `ConsultantScope`**
   ```dax
   -- DimUser table
   [Email] = USERNAME()
   
   -- FactBooking table (consultant sees their own bookings)
   [ConsultantEmail] = USERNAME()
   ```

   **Role: `StudentScope`**
   ```dax
   -- DimUser table
   [Email] = USERNAME()
   
   -- FactApplication table
   [StudentEmail] = USERNAME()
   
   -- FactBooking table (student sees their own bookings)
   [StudentEmail] = USERNAME()
   ```

   **Role: `CompanyScope`**
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
new EffectiveIdentity(
    username: userEmail,          // e.g. "student@example.com"
    datasets: [datasetId],
    roles: activeRole switch {
        "Consultant" => ["ConsultantScope"],
        "Student"    => ["StudentScope"],
        "Company"    => ["CompanyScope"],
        _            => []           // Admin/SuperAdmin: no role filter
    }
)
```

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

Expected output:
```
[PASS] Admin token: isConfigured=True, effectiveIdentity roles=[]
[PASS] Consultant token: isConfigured=True, effectiveIdentity roles=['ConsultantScope']
[PASS] Student token: isConfigured=True, effectiveIdentity roles=['StudentScope']
[PASS] Company token: isConfigured=True, effectiveIdentity roles=['CompanyScope']
```

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| "Report not found" (404) | `ReportIds` in `appsettings.json` not set | Fill in the Power BI report GUIDs after publishing |
| "Token generation failed" | Service principal not a Workspace Member | Add SP to workspace with Contributor role |
| Empty data after approval | RLS role name mismatch | Role names in `.pbix` must match exactly: `ConsultantScope`, `StudentScope`, `CompanyScope` |
| Admin sees filtered data | `EffectiveIdentity` sent for Admin | Check `PowerBiService.cs` — Admin should pass `roles: []` |
