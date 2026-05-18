using MediatR;
using ScholarPath.Application.Ai.DTOs;

namespace ScholarPath.Application.Ai.Commands.RebuildKnowledgeBase;

/// <summary>
/// Admin command — rebuilds the RAG knowledge base from the current open
/// scholarships and the curated FAQ dataset. <see cref="Force"/> re-embeds
/// every document even when its content is unchanged.
/// </summary>
public sealed record RebuildKnowledgeBaseCommand(bool Force = false)
    : IRequest<KnowledgeBaseRebuildResultDto>;
