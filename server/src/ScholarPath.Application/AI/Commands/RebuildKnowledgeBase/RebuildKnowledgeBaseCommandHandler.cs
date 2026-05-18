using MediatR;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Ai.Commands.RebuildKnowledgeBase;

public sealed class RebuildKnowledgeBaseCommandHandler(IKnowledgeBaseIndexer indexer)
    : IRequestHandler<RebuildKnowledgeBaseCommand, KnowledgeBaseRebuildResultDto>
{
    public Task<KnowledgeBaseRebuildResultDto> Handle(
        RebuildKnowledgeBaseCommand request, CancellationToken cancellationToken)
        => indexer.RebuildAsync(request.Force, cancellationToken);
}
