# Runbook — Azure OpenAI / Knowledge-Base setup

**Applies to:** the AI subsystem (`Ai:Provider`) and the RAG knowledge base.
**Symptom this fixes:** `POST /api/admin/ai/knowledge-base/rebuild` returns **503**,
and AI chat returns canned/stub answers instead of real model output.

---

## Why it happens

`appsettings.json` ships with `Ai:Provider = "Stub"` and **placeholder** Azure/OpenAI
credentials (secrets must never be committed). The knowledge-base rebuild needs a
**real embedding provider** to vectorize documents; with no provider configured,
`KnowledgeBaseIndexer.RebuildAsync` cannot embed and the endpoint fails closed with
**503 Service Unavailable** (by design — a clear signal, not a crash).

You have two supported options.

---

## Option A — Configure Azure OpenAI (real RAG + chat)

Requires an Azure subscription with an **Azure OpenAI** resource and two deployments:
a chat model (e.g. `gpt-4o-mini`) and an embedding model (e.g. `text-embedding-3-small`).

### 1. Settings to provide

| Key (`appsettings`) | App Service env var | Example |
|---|---|---|
| `Ai:Provider` | `Ai__Provider` | `AzureOpenAi` |
| `Ai:AzureOpenAi:Endpoint` | `Ai__AzureOpenAi__Endpoint` | `https://<resource>.openai.azure.com/` |
| `Ai:AzureOpenAi:ApiKey` | `Ai__AzureOpenAi__ApiKey` | *(from Key Vault — see below)* |
| `Ai:AzureOpenAi:DeploymentName` | `Ai__AzureOpenAi__DeploymentName` | `gpt-4o-mini` |
| `Ai:AzureOpenAi:EmbeddingDeploymentName` | `Ai__AzureOpenAi__EmbeddingDeploymentName` | `text-embedding-3-small` |
| `Ai:AzureOpenAi:EmbeddingDimensions` | `Ai__AzureOpenAi__EmbeddingDimensions` | `1536` |
| `Ai:AzureOpenAi:ApiVersion` | `Ai__AzureOpenAi__ApiVersion` | `2024-10-21` |

> **Never** put the API key in `appsettings.json`. Locally use `dotnet user-secrets`;
> in production store it in **Azure Key Vault** and reference it from App Service, the
> same way `Jwt:KeyVaultUri` / `FieldEncryption:KeyVaultUri` are wired.

### 2. Local dev (user-secrets)

```bash
cd server/src/ScholarPath.API
dotnet user-secrets set "Ai:Provider" "AzureOpenAi"
dotnet user-secrets set "Ai:AzureOpenAi:Endpoint" "https://<resource>.openai.azure.com/"
dotnet user-secrets set "Ai:AzureOpenAi:ApiKey" "<key>"
```

### 3. Re-index after configuring

The corpus only matches vectors from the **currently active embedding model**, so
re-index whenever the embedding deployment changes:

```
POST /api/admin/ai/knowledge-base/rebuild   (admin JWT)   ?force=true
```

Expect **200** with a `KnowledgeBaseRebuildResultDto` (counts of indexed docs).

---

## Option B — Local provider for the demo (no Azure cost)

For a graduation demo where you don't want Azure spend, use the deterministic local
provider. Recommendations are already local; this makes chat/embeddings local too.

```bash
dotnet user-secrets set "Ai:Provider" "Local"   # or Ai__Provider=Local on App Service
```

`LocalAiService` returns deterministic, offline answers and `LocalEmbeddingService`
produces local vectors, so rebuild succeeds and chat works with **no external calls
and no key**. Document in the demo script that AI is running in local/deterministic mode.

---

## Verification

| Check | Expected |
|---|---|
| `GET /health` | `Healthy` |
| `POST /api/admin/ai/knowledge-base/rebuild?force=true` | `200` + indexed counts (not 503) |
| Ask the chatbot a scholarship question | Grounded answer + sources + disclaimer |
| `GET /api/admin/ai/...` cost/usage | per-call `AiInteraction` rows logged |

If rebuild still 503s after Option A: check the App Service log for an auth/endpoint
error, confirm the **deployment names** match the Azure portal exactly, and confirm the
key is being read (Key Vault access policy / managed identity).
