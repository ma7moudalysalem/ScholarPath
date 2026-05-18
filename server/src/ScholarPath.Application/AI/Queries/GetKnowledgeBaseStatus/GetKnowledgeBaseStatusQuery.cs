using MediatR;
using ScholarPath.Application.Ai.DTOs;

namespace ScholarPath.Application.Ai.Queries.GetKnowledgeBaseStatus;

/// <summary>Admin query — current RAG knowledge-base index status.</summary>
public sealed record GetKnowledgeBaseStatusQuery : IRequest<KnowledgeBaseStatusDto>;
