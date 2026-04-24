# adf/arm/

Full ARM templates for the Data Factory instance + its auth plumbing. Pipelines
and datasets themselves are deployed via the git-sync integration in the ADF
portal (reading from `analytics/adf/pipeline/*.json`); this ARM template sets
up the factory and its linkedServices so the git sync has something to land on.

## Deploy

```bash
az deployment group create \
    --resource-group scholarpath-rg-dev \
    --template-file adf-arm-template.json \
    --parameters   adf-arm-parameters.dev.json
```

After the factory exists:

1. Portal → ADF → Manage → Git configuration → Configure → Azure DevOps/GitHub
   → repo `ma7moudalysalem/ScholarPath`, branch `main`, root folder `analytics/adf`.
2. Portal → Triggers → enable `tr_cdc_every_15min`.

## Secrets (must exist in Key Vault before deploy)

| Secret name            | Purpose                                       |
|------------------------|-----------------------------------------------|
| `adf-sql-sp-secret`    | Service-principal password used by ADF → SQL |
| `adls-account-key`     | Shared key for the Bronze Storage account    |
| `adf-powerbi-sp-secret`| Power BI embed SP (PB-015 FR-220)            |

Grant ADF's system-assigned managed identity `get` + `list` on the Key Vault
secrets before the first pipeline run.
