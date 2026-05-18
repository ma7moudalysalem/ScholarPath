# ScholarPath AI — RAG, datasets & fine-tuning

This document describes the AI architecture behind the ScholarPath assistant:
Retrieval-Augmented Generation (RAG), the curated datasets, and the Azure
OpenAI fine-tuning pipeline.

## 1. Providers

The AI provider is selected with one config key — `Ai:Provider`:

| `Ai:Provider` | Chat              | Embeddings            | Needs a key |
|---------------|-------------------|-----------------------|-------------|
| `Stub` / `Local` (default) | Local RAG router (extractive) | `local-hash-v1` (offline) | No |
| `OpenAi`      | OpenAI chat completions | `local-hash-v1` (offline) | `Ai:OpenAi:ApiKey` |
| `AzureOpenAi` | Azure OpenAI chat | Azure `text-embedding-3-small` | `Ai:AzureOpenAi:*` |

Every provider grounds the chatbot with RAG. The cloud providers degrade to the
local path automatically on a network failure, so the chat never hard-fails.

## 2. The RAG pipeline

```
scholarships + FAQ ──▶ KnowledgeDocument rows ──▶ embeddings (vector)
                                                        │
user question ──▶ embed ──▶ cosine-similarity search ──▶ top-K context
                                                        │
                          context + question ──▶ LLM ──▶ grounded answer + citations
```

- **Knowledge base** — `KnowledgeDocument` rows, one per open scholarship and
  per FAQ entry. The embedding vector is stored as a packed `float[]` column.
- **Indexer** — `KnowledgeBaseIndexer` upserts documents and (re)embeds only the
  ones that are new, changed, or were embedded with a different model.
- **Retriever** — `KnowledgeRetriever` embeds the query and ranks documents by
  cosine similarity in-process (the corpus is small — no vector DB needed).
- **Grounding** — the top-K documents are injected into the chat prompt as
  context; the answer cites them. `Ai:RagTopK` and `Ai:RagMinScore` tune this.

Without an Azure key the offline `local-hash-v1` embedder (feature hashing) is
used, so RAG is fully demonstrable with no cloud dependency.

## 3. Datasets

Two curated datasets are bundled (embedded into the Infrastructure assembly,
authored under `server/datasets/`):

- **`external-scholarships.json`** — 24 real international scholarship programs.
  Imported as Open listings (`ImportExternalScholarshipsCommand`); they join the
  catalogue and the knowledge base.
- **`scholarpath-faq.json`** — curated help knowledge base, indexed as FAQ
  documents so the chatbot can answer "how do I…" questions.

The knowledge base is built automatically on startup (after the demo seed) and
can be rebuilt any time from **Admin → AI knowledge base**.

## 4. Fine-tuning runbook (Azure OpenAI)

Fine-tuning teaches a base model ScholarPath's domain and house voice. The
training data is generated from the platform's own data.

### Step 1 — export the dataset

As an Admin, open **Admin → AI knowledge base → Export .jsonl** (or call
`GET /api/admin/ai/fine-tuning/dataset`). Save it as `scholarpath-finetune.jsonl`.
Each line is a chat training example built from the FAQ and the catalogue.

### Step 2 — run the fine-tuning job

Requires PowerShell 7+ and an Azure OpenAI resource in a region that supports
fine-tuning of the base model.

```pwsh
./scripts/ai/run-finetune.ps1 `
    -Endpoint   "https://<your-resource>.openai.azure.com" `
    -ApiKey     $env:AZURE_OPENAI_KEY `
    -DatasetPath ./scholarpath-finetune.jsonl `
    -BaseModel  "gpt-4o-mini"
```

The script uploads the dataset, creates the fine-tuning job, and polls until it
finishes (typically 30–90 minutes). It prints the resulting `fine_tuned_model`.

### Step 3 — deploy and wire it up

1. In Azure AI Foundry, create a **deployment** for the fine-tuned model.
2. Set `Ai:AzureOpenAi:FineTunedDeploymentName` to that deployment name.
3. Restart the API — the chatbot now uses the fine-tuned model for chat.

When `FineTunedDeploymentName` is empty, chat uses the base `DeploymentName`.

## 5. Configuration reference (`Ai` section)

```jsonc
"Ai": {
  "Provider": "AzureOpenAi",            // Stub | Local | OpenAi | AzureOpenAi
  "RagTopK": 4,                          // documents retrieved per chat turn
  "RagMinScore": 0.15,                   // min cosine similarity to use as context
  "AzureOpenAi": {
    "Endpoint": "https://<resource>.openai.azure.com",
    "ApiKey": "<key>",
    "DeploymentName": "gpt-4o-mini",          // chat deployment
    "EmbeddingDeploymentName": "text-embedding-3-small",
    "EmbeddingDimensions": 1536,
    "ApiVersion": "2024-10-21",
    "FineTunedDeploymentName": ""              // set after fine-tuning
  }
}
```

Switching the embedding model invalidates stored vectors automatically — the
indexer re-embeds documents whose `EmbeddingModel` no longer matches, so a
**Rebuild** from the admin panel after changing providers is all that is needed.
