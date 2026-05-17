# Deployment

How ScholarPath is built, deployed, and hosted on Azure.

> **Provisioning is a manual step.** The Bicep templates and GitHub Actions
> workflows in this repo describe the infrastructure and pipeline, but they
> do **not** create anything on their own. Standing up the Azure resources
> requires the team's Azure subscription and someone with `Contributor` +
> `User Access Administrator` (or `Owner`) rights on the target resource
> group. Do the one-time provisioning below before the deploy workflow can
> succeed.

---

## 1. Overview

| Component | Hosted on | Built by |
|-----------|-----------|----------|
| API (.NET 10) | Azure App Service (Linux) | `dotnet publish` |
| Client (React 19 + Vite) | Azure Static Web Apps | `npm run build` → `client/dist` |
| Database | Azure SQL (server + database) | — |
| Cache | Azure Cache for Redis | — |
| Secrets | Azure Key Vault | — |
| Telemetry | Application Insights + Log Analytics | — |

Pipelines:

- **`.github/workflows/ci.yml`** — runs on every push / PR to `main` and
  `integration`: restore, build the solution, run unit tests, and build the
  client. Integration tests (Testcontainers) run only on pushes.
- **`.github/workflows/deploy.yml`** — runs on push to `main` (or manual
  `workflow_dispatch`): publishes the API to App Service and the client to
  Static Web Apps.

---

## 2. Prerequisites

- An Azure subscription (the team's).
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) `>= 2.60`
  with the Bicep tooling (`az bicep install`).
- Permission to create resources and role assignments in the target
  resource group.
- Repository admin access on GitHub (to set secrets / variables).

---

## 3. One-time provisioning

### 3.1 Create the resource group

```bash
az login
az account set --subscription "<SUBSCRIPTION_ID>"

az group create \
  --name rg-scholarpath-prod \
  --location westeurope
```

### 3.2 Deploy the infrastructure

The Bicep template lives in [`infra/main.bicep`](../infra/main.bicep) with
non-secret defaults in [`infra/main.parameters.json`](../infra/main.parameters.json).
Two parameters are secret and must be passed on the command line — never
commit them:

- `sqlAdminPassword` — Azure SQL admin password (>= 12 chars, meets
  [Azure complexity rules](https://learn.microsoft.com/sql/relational-databases/security/password-policy)).
- `jwtSigningKey` — HMAC-SHA256 signing key for JWT access tokens
  (**>= 64 characters**; generate a fresh random value, see §5).

```bash
az deployment group create \
  --resource-group rg-scholarpath-prod \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json \
  --parameters sqlAdminPassword='<STRONG_SQL_PASSWORD>' \
               jwtSigningKey='<64+_CHAR_RANDOM_KEY>'
```

Validate first without deploying:

```bash
az deployment group what-if \
  --resource-group rg-scholarpath-prod \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json \
  --parameters sqlAdminPassword='<...>' jwtSigningKey='<...>'
```

When the deployment finishes, note the **outputs** (`apiAppName`,
`staticWebAppHostname`, `apiAppDefaultHostname`, `keyVaultName`, …) — you
need them for the GitHub configuration below:

```bash
az deployment group show \
  --resource-group rg-scholarpath-prod \
  --name main \
  --query properties.outputs
```

### 3.3 Set the CORS origin

`clientCorsOrigin` is empty on the first deploy because the Static Web App
URL is not known yet. After the first deployment, re-run the deployment with
the real client URL so the API allows the browser origin:

```bash
az deployment group create \
  --resource-group rg-scholarpath-prod \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json \
  --parameters sqlAdminPassword='<...>' jwtSigningKey='<...>' \
               clientCorsOrigin='https://<your-swa>.azurestaticapps.net'
```

---

## 4. What the Bicep template provisions

| Resource | Notes |
|----------|-------|
| App Service Plan (Linux) | `B1` by default — bump to `P1v3` for real load (`appServicePlanSku`). |
| App Service (API) | `DOTNETCORE\|10.0`, HTTPS-only, system-assigned managed identity, health check at `/health`. |
| Static Web App (client) | `Free` tier; deployed via the workflow's deployment token. |
| Azure SQL server + database | TLS 1.2 minimum; `Basic` DB SKU by default. Firewall rule allows Azure services. |
| Azure Cache for Redis | `Basic C0`; non-SSL port disabled. |
| Key Vault | RBAC authorization, soft-delete + purge protection on. |
| Log Analytics + Application Insights | Workspace-based App Insights, 30-day retention. |

The API App Service is wired to Key Vault via **managed identity + Key Vault
references**: its secrets (`ConnectionStrings__DefaultConnection`,
`Jwt__SigningKey`, `Redis__ConnectionString`) are stored as
`@Microsoft.KeyVault(SecretUri=...)` app settings and resolved at runtime —
no secret values live in App Service configuration. The template grants the
identity the **Key Vault Secrets User** role.

---

## 5. The JWT signing key

The API signs access tokens with a symmetric HMAC-SHA256 key read from the
`Jwt:SigningKey` configuration value (env var `Jwt__SigningKey`). In Azure
this comes from Key Vault.

Generate a strong key:

```bash
openssl rand -base64 64
```

The Bicep deployment writes the value you pass as `jwtSigningKey` into the
Key Vault secret **`Jwt--SigningKey`** (Key Vault uses `--` where config
uses `:`). To rotate it later, update the secret directly:

```bash
az keyvault secret set \
  --vault-name <KEY_VAULT_NAME> \
  --name "Jwt--SigningKey" \
  --value "$(openssl rand -base64 64)"
```

Then restart the API App Service so it picks up the new value. Rotating the
key invalidates all live access tokens (clients re-authenticate via their
refresh tokens).

> The Key Vault references the API also expects:
> `ConnectionStrings--DefaultConnection` and `Redis--ConnectionString` — both
> are created automatically by the Bicep deployment.

---

## 6. GitHub configuration

The deploy workflow authenticates to Azure with **OIDC** (federated
credentials) — there is no Azure client secret stored in GitHub.

### 6.1 Create an app registration with a federated credential

```bash
# Create the app registration + service principal
az ad app create --display-name "scholarpath-github-deploy"
APP_ID=$(az ad app list --display-name "scholarpath-github-deploy" --query "[0].appId" -o tsv)
az ad sp create --id "$APP_ID"

# Grant it Contributor on the resource group
SUB_ID=$(az account show --query id -o tsv)
az role assignment create \
  --assignee "$APP_ID" \
  --role Contributor \
  --scope "/subscriptions/$SUB_ID/resourceGroups/rg-scholarpath-prod"

# Add a federated credential trusting this repo's main branch
az ad app federated-credential create \
  --id "$APP_ID" \
  --parameters '{
    "name": "scholarpath-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:ma7moudalysalem/ScholarPath:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

> If you also deploy from `workflow_dispatch` on other branches or use GitHub
> Environments, add matching federated credentials (e.g.
> `repo:ma7moudalysalem/ScholarPath:environment:production`).

### 6.2 Repository **secrets**

`Settings → Secrets and variables → Actions → Secrets`:

| Secret | Value |
|--------|-------|
| `AZURE_CLIENT_ID` | `appId` of the app registration above. |
| `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv`. |
| `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv`. |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Static Web App deployment token (see §6.4). |
| `VITE_STRIPE_PUBLISHABLE_KEY` | Stripe publishable (`pk_...`) key for the client build. |

### 6.3 Repository **variables**

`Settings → Secrets and variables → Actions → Variables`:

| Variable | Value |
|----------|-------|
| `AZURE_API_APP_NAME` | API App Service name (`apiAppName` output, e.g. `app-scholarpath-prod-api`). |
| `VITE_API_BASE_URL` | Public API URL, e.g. `https://app-scholarpath-prod-api.azurewebsites.net`. |
| `VITE_GOOGLE_CLIENT_ID` | Google OAuth client id. |
| `VITE_MICROSOFT_CLIENT_ID` | Microsoft OAuth client id. |

### 6.4 Static Web App deployment token

```bash
az staticwebapp secrets list \
  --name <STATIC_WEB_APP_NAME> \
  --resource-group rg-scholarpath-prod \
  --query "properties.apiKey" -o tsv
```

Store the result as the `AZURE_STATIC_WEB_APPS_API_TOKEN` secret.

---

## 7. Deploying

Once the infrastructure exists and GitHub is configured:

- **Automatic** — every push to `main` triggers `deploy.yml`, which publishes
  the API and the client.
- **Manual** — `Actions → Deploy → Run workflow`, pick `staging` or
  `production`.

The workflow:

1. `deploy-api` — `dotnet publish` the API, `azure/login` via OIDC,
   `azure/webapps-deploy` to the App Service.
2. `deploy-client` — `npm ci && npm run build` with the production `VITE_*`
   values, then `Azure/static-web-apps-deploy` with the deployment token.

---

## 8. Post-deploy steps

### 8.1 Database migrations

EF Core migrations are applied by `DbSeeder.SeedAsync`, which calls
`Database.MigrateAsync()`. In `Program.cs` the seeder runs **only in the
`Development` environment** — so on a `Production` App Service migrations are
**not** applied automatically. Run them as an explicit step after the first
deploy and after any deploy that adds migrations:

```bash
# From a machine with the .NET SDK + EF Core tools and DB connectivity:
dotnet tool install --global dotnet-ef   # first time only

cd server
dotnet ef database update \
  --project src/ScholarPath.Infrastructure \
  --startup-project src/ScholarPath.API \
  --connection "Server=tcp:<sql-server>.database.windows.net,1433;Initial Catalog=sqldb-scholarpath;User ID=scholarpathadmin;Password=<...>;Encrypt=True;"
```

Make sure your client IP is allowed through the Azure SQL firewall (the
template only opens the `AllowAllAzureIps` rule for in-Azure traffic):

```bash
az sql server firewall-rule create \
  --resource-group rg-scholarpath-prod \
  --server <SQL_SERVER_NAME> \
  --name AllowMyIP \
  --start-ip-address <YOUR_IP> --end-ip-address <YOUR_IP>
```

Remove the rule again when you are done.

### 8.2 Seeding

`DbSeeder` also seeds roles and baseline data, but — like migrations — only
in `Development`. For a production environment, seed the reference data
deliberately (e.g. run the API once against the prod DB with
`ASPNETCORE_ENVIRONMENT=Development` from a trusted host, or add a dedicated
seed step). Do **not** leave the App Service running as `Development`.

### 8.3 Smoke test

```bash
curl -fsS https://<api-host>/health        # expect HTTP 200
```

Then open the Static Web App URL and confirm login works (the client must
point at the deployed API via `VITE_API_BASE_URL`, and the API must allow
the client origin via `clientCorsOrigin` / `Cors__AllowedOrigins__0`).

---

## 9. Configuration reference

App settings the API reads in Azure (most are set by the Bicep template):

| Setting | Source |
|---------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` (App Service app setting). |
| `ConnectionStrings__DefaultConnection` | Key Vault reference. |
| `Jwt__SigningKey` | Key Vault reference. |
| `Jwt__Issuer` / `Jwt__Audience` | App setting (`jwtIssuer` / `jwtAudience` params). |
| `Redis__Enabled` / `Redis__ConnectionString` | App setting / Key Vault reference. |
| `Hangfire__Enabled` | `true` — recurring jobs run in-process. |
| `Cors__AllowedOrigins__0` / `App__ClientUrl` | The Static Web App URL. |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App setting (from App Insights). |

Secrets **not** provisioned by the template (Stripe, OAuth client secrets,
SendGrid, etc.) should be added as Key Vault secrets and referenced the same
way, or set as App Service app settings, depending on your security posture.
