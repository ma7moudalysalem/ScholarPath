namespace ScholarPath.Application.Ai.DTOs;

/// <summary>Knowledge-base index status for the admin RAG panel.</summary>
public sealed record KnowledgeBaseStatusDto(
    int TotalDocuments,
    int ScholarshipDocuments,
    int FaqDocuments,
    int EmbeddedDocuments,
    int PendingDocuments,
    string ActiveEmbeddingModel,
    DateTimeOffset? LastIndexedAt,
    // Resource Hub articles/guides that were indexed (PB-009). Defaults to 0
    // so existing serialisers/tests built against the previous shape keep working.
    int ResourceDocuments = 0);

/// <summary>Outcome of a knowledge-base rebuild (re-index).</summary>
public sealed record KnowledgeBaseRebuildResultDto(
    int Upserted,
    int Reembedded,
    int Removed,
    int Skipped,
    KnowledgeBaseStatusDto Status);

/// <summary>Outcome of importing a curated dataset of scholarships.</summary>
public sealed record DatasetImportResultDto(
    string DatasetName,
    int TotalInDataset,
    int Created,
    int Updated,
    int Skipped);

/// <summary>Combined result of importing a dataset and rebuilding the knowledge base.</summary>
public sealed record DatasetImportWithRebuildDto(
    DatasetImportResultDto Import,
    KnowledgeBaseRebuildResultDto KnowledgeBase);

/// <summary>
/// An exported fine-tuning dataset in the OpenAI chat JSONL format — one
/// training example per line. Fed to an Azure OpenAI fine-tuning job.
/// </summary>
public sealed record FineTuningDatasetDto(
    string FileName,
    int ExampleCount,
    string Jsonl,
    DateTimeOffset GeneratedAt);

/// <summary>
/// A knowledge-base document the RAG retriever used to ground a chat answer —
/// returned to the UI as a citation under the assistant's reply.
/// </summary>
public sealed record ChatSourceDto(
    string Title,
    string SourceType,
    Guid? ScholarshipId,
    double Score);
