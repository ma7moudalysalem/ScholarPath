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
One parameter is secret and must be passed on the command line — never
commit it:

- `sqlAdminPassword` — Azure SQL admin password (>= 12 chars, meets
  [Azure complexity rules](https://learn.microsoft.com/sql/relational-databases/security/password-policy)).

The RS256 JWT signing key is **not** a deployment parameter — it is an RSA
private key the operator uploads to Key Vault by hand after provisioning
(see §5).

```bash
az deployment group create \
  --resource-group rg-scholarpath-prod \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json \
  --parameters sqlAdminPassword='<STRONG_SQL_PASSWORD>'
```

Validate first without deploying:

```bash
az deployment group what-if \
  --resource-group rg-scholarpath-prod \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json \
  --parameters sqlAdminPassword='<...>'
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
  --parameters sqlAdminPassword='<...>' \
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

The API App Service is wired to Key Vault via **managed identity**. Two
secrets (`ConnectionStrings__DefaultConnection`, `Redis__ConnectionString`)
are stored as `@Microsoft.KeyVault(SecretUri=...)` app settings and resolved
by the App Service runtime at startup — no secret values live in App Service
configuration. The RS256 JWT signing key is handled differently: the API
reads it from Key Vault **itself** at runtime (see §5), so the template only
sets `Jwt__KeyVaultUri` (the vault URI) and `Jwt__KeyName` as plain app
settings. Either way the template grants the identity the **Key Vault
Secrets User** role, which covers both the Key Vault references and the
API's own secret read.

---

## 5. The JWT signing key

The API signs access tokens with **RS256** — an asymmetric RSA key pair. It
signs with the RSA **private key** and validates with the public key. In
Azure the private key is stored in Key Vault as a **secret** (not a Bicep
parameter, and not an App Service app setting): the API reads it at runtime
itself, using `DefaultAzureCredential` + a Key Vault `SecretClient`. The
secret name comes from `Jwt:KeyName` (env var `Jwt__KeyName`, default
`scholarpath-jwt-signing`) and the vault from `Jwt:KeyVaultUri`
(`Jwt__KeyVaultUri`) — both are set by the Bicep template.

Because the App Service's managed identity already has the **Key Vault
Secrets User** role, no further wiring is needed — the API authenticates to
Key Vault with that identity.

Generate an RSA-2048 key pair:

```bash
openssl genrsa -out jwt-private.pem 2048
# (optional) derive the public key, e.g. for verification tooling:
openssl rsa -in jwt-private.pem -pubout -out jwt-public.pem
```

Upload the **private key PEM** as the Key Vault secret
`scholarpath-jwt-signing`:

```bash
az keyvault secret set \
  --vault-name <KEY_VAULT_NAME> \
  --name scholarpath-jwt-signing \
  --file jwt-private.pem
```

This is a one-time post-provision step (the Bicep deployment does **not**
create this secret). To **rotate** the key later, set a new secret value the
same way — generate a fresh key pair and run `az keyvault secret set` again:

```bash
openssl genrsa -out jwt-private.pem 2048
az keyvault secret set \
  --vault-name <KEY_VAULT_NAME> \
  --name scholarpath-jwt-signing \
  --file jwt-private.pem
```

Then restart the API App Service so it picks up the new key. Rotating the
key invalidates all live access tokens (clients re-authenticate via their
refresh tokens).

> **The private key must never be committed to the repository or placed in
> App Service configuration.** It lives only in Key Vault. Delete the local
> `jwt-private.pem` once it is uploaded.

> The Key Vault references the API also expects:
> `ConnectionStrings--DefaultConnection` and `Redis--ConnectionString` — both
> are created automatically by the Bicep deployment.

---

## 6. Encrypting sensitive fields at rest

The database has **two** at-rest encryption layers:

1. **Azure SQL Transparent Data Encryption (TDE)** encrypts the whole database
   — data files, log files and backups — and is **on by default** for Azure
   SQL Database. It needs no configuration and protects against someone
   obtaining the physical storage or a backup file.
2. **Application-level field encryption** is a second, stronger layer for the
   most sensitive columns. The API encrypts specific personal-data columns
   with **AES-256-GCM** *before* they reach SQL Server, so they stay
   ciphertext even to someone with a direct `SELECT` on the table — TDE alone
   does not protect against that, because TDE decrypts transparently for any
   authenticated query. Currently encrypted: the user profile **biography**
   and an application's **personal notes**.

Field encryption is controlled by the `FieldEncryption` config section:

| Setting | Meaning |
|---------|---------|
| `FieldEncryption__KeyVaultUri` | Azure Key Vault URI. When set (production) the AES key is read from the vault; when empty (development) the local `FieldEncryption__DevKey` is used. |
| `FieldEncryption__KeyName` | Key Vault **secret** name holding the Base64 AES key (default `field-encryption-key`). |
| `FieldEncryption__DevKey` | Development-only Base64 256-bit key. Never set in production — leave it empty and use Key Vault. |

The AES key is a **256-bit key, Base64-encoded, stored as a Key Vault secret**
— exactly like the JWT key in §5, and resolved at runtime by the API itself
via `DefaultAzureCredential` (the App Service managed identity already has the
**Key Vault Secrets User** role). Set `FieldEncryption__KeyVaultUri` and
`FieldEncryption__KeyName` as plain app settings on the API App Service.

Generate a key and upload it as the Key Vault secret `field-encryption-key`:

```bash
# 32 random bytes (256 bits), Base64-encoded
openssl rand -base64 32 > field-encryption-key.txt
az keyvault secret set \
  --vault-name <KEY_VAULT_NAME> \
  --name field-encryption-key \
  --file field-encryption-key.txt
```

This is a one-time post-provision step (the Bicep deployment does **not**
create this secret). Delete the local `field-encryption-key.txt` once it is
uploaded.

> **The field-encryption key must be stable — never rotate it casually.**
> Unlike the JWT key, rotating this key does **not** silently re-encrypt
> existing rows: a value encrypted under the old key can only be decrypted
> with the old key. The ciphertext envelope carries a version segment
> (`enc:v1:`) precisely so a future key rotation can be done as a deliberate,
> versioned migration. Decryption transparently passes through any value that
> is **not** an `enc:v1:` envelope, so enabling the feature over a database of
> pre-existing plaintext rows is safe — old rows keep reading and are
> encrypted on their next write.

---

## 7. Antivirus scanning of uploads

Every file upload (the document vault, profile photos) is virus-scanned
**before** the bytes are persisted (SRS security NFR). Scanning goes through
**ClamAV**: the API streams the upload to a `clamd` daemon via the `nClam`
client and only stores the file if the verdict is *clean*.

The behaviour is controlled by the `FileScanning` config section:

| Setting | Meaning |
|---------|---------|
| `FileScanning__Enabled` | Master switch. `false` by default — the app then uses a no-op scanner and stores files unscanned. |
| `FileScanning__ClamAvHost` | Hostname of the `clamd` daemon. |
| `FileScanning__ClamAvPort` | `clamd` port (ClamAV's INSTREAM default is `3310`). |

**Production must set `FileScanning__Enabled=true`** and run a reachable
`clamd` daemon — the ClamAV container / sidecar. The repo's
[`docker-compose.yml`](../docker-compose.yml) includes a `clamav` service
(image `clamav/clamav:1.4`, port `3310`, with a healthcheck) and the `api`
service already points `FileScanning__ClamAvHost` at it; mirror that in the
production environment (an Azure Container Instances sidecar, a `clamav`
container on the same network, or a managed scanning endpoint).

> **Fail-closed.** When `FileScanning__Enabled=true` and the daemon is
> unreachable or returns an error, the upload is **rejected** — the API never
> stores a file it could not scan. So a misconfigured or down `clamd` blocks
> uploads rather than letting unverified files through. Confirm the daemon is
> reachable (`clamd` is listening on `ClamAvHost:ClamAvPort` and has finished
> loading its signature database) before enabling scanning in production.

The `clamav/clamav` image downloads its full virus-definition database on
first start, which takes a couple of minutes; the container is not healthy —
and uploads will be rejected — until that finishes. Give it a generous
`start_period` (the compose healthcheck uses 120s) and persist
`/var/lib/clamav` to a volume so definitions survive restarts.

---

## 8. GitHub configuration

The deploy workflow authenticates to Azure with **OIDC** (federated
credentials) — there is no Azure client secret stored in GitHub.

### 8.1 Create an app registration with a federated credential

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

### 8.2 Repository **secrets**

`Settings → Secrets and variables → Actions → Secrets`:

| Secret | Value |
|--------|-------|
| `AZURE_CLIENT_ID` | `appId` of the app registration above. |
| `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv`. |
| `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv`. |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Static Web App deployment token (see §8.4). |
| `VITE_STRIPE_PUBLISHABLE_KEY` | Stripe publishable (`pk_...`) key for the client build. |

### 8.3 Repository **variables**

`Settings → Secrets and variables → Actions → Variables`:

| Variable | Value |
|----------|-------|
| `AZURE_API_APP_NAME` | API App Service name (`apiAppName` output, e.g. `app-scholarpath-prod-api`). |
| `VITE_API_BASE_URL` | Public API URL, e.g. `https://app-scholarpath-prod-api.azurewebsites.net`. |
| `VITE_GOOGLE_CLIENT_ID` | Google OAuth client id. |
| `VITE_MICROSOFT_CLIENT_ID` | Microsoft OAuth client id. |

### 8.4 Static Web App deployment token

```bash
az staticwebapp secrets list \
  --name <STATIC_WEB_APP_NAME> \
  --resource-group rg-scholarpath-prod \
  --query "properties.apiKey" -o tsv
```

Store the result as the `AZURE_STATIC_WEB_APPS_API_TOKEN` secret.

---

## 9. Deploying

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

## 10. Post-deploy steps

### 10.1 Database migrations

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

### 10.2 Seeding

`DbSeeder` also seeds roles and baseline data, but — like migrations — only
in `Development`. For a production environment, seed the reference data
deliberately (e.g. run the API once against the prod DB with
`ASPNETCORE_ENVIRONMENT=Development` from a trusted host, or add a dedicated
seed step). Do **not** leave the App Service running as `Development`.

### 10.3 Smoke test

```bash
curl -fsS https://<api-host>/health        # expect HTTP 200
```

Then open the Static Web App URL and confirm login works (the client must
point at the deployed API via `VITE_API_BASE_URL`, and the API must allow
the client origin via `clientCorsOrigin` / `Cors__AllowedOrigins__0`).

---

## 11. Configuration reference

App settings the API reads in Azure (most are set by the Bicep template):

| Setting | Source |
|---------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` (App Service app setting). |
| `ConnectionStrings__DefaultConnection` | Key Vault reference. |
| `Jwt__KeyVaultUri` | App setting — the Key Vault URI. The API reads the RS256 signing key from this vault at runtime via its managed identity. |
| `Jwt__KeyName` | App setting (`jwtKeyName` param, default `scholarpath-jwt-signing`) — the Key Vault secret name of the RSA private key. |
| `Jwt__Issuer` / `Jwt__Audience` | App setting (`jwtIssuer` / `jwtAudience` params). |
| `FieldEncryption__KeyVaultUri` | App setting — the Key Vault URI. Set in production so the API reads the AES field-encryption key from Key Vault (§6). |
| `FieldEncryption__KeyName` | App setting (default `field-encryption-key`) — the Key Vault secret name of the Base64 AES-256 key. |
| `Redis__Enabled` / `Redis__ConnectionString` | App setting / Key Vault reference. |
| `Hangfire__Enabled` | `true` — recurring jobs run in-process. |
| `Cors__AllowedOrigins__0` / `App__ClientUrl` | The Static Web App URL. |
| `FileScanning__Enabled` | `true` in production — turns on ClamAV scanning of uploads (§7). |
| `FileScanning__ClamAvHost` / `FileScanning__ClamAvPort` | App setting — host/port of the reachable `clamd` daemon. |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App setting (from App Insights). |

Secrets **not** provisioned by the template (Stripe, OAuth client secrets,
SendGrid, etc.) should be added as Key Vault secrets and referenced the same
way, or set as App Service app settings, depending on your security posture.
