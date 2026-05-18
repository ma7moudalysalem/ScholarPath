using MediatR;
using ScholarPath.Application.Ai.DTOs;

namespace ScholarPath.Application.Ai.Queries.ExportFineTuningDataset;

/// <summary>
/// Admin query — builds a fine-tuning dataset (chat JSONL) from the platform's
/// own data: the FAQ knowledge base and the scholarship catalogue. The output
/// is uploaded to an Azure OpenAI fine-tuning job (see the fine-tuning runbook).
/// </summary>
public sealed record ExportFineTuningDatasetQuery : IRequest<FineTuningDatasetDto>;
