using MediatR;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Ai.Queries.GetKnowledgeBaseStatus;

public sealed class GetKnowledgeBaseStatusQueryHandler(IKnowledgeBaseIndexer indexer)
    : IRequestHandler<GetKnowledgeBaseStatusQuery, KnowledgeBaseStatusDto>
{
    public Task<KnowledgeBaseStatusDto> Handle(
        GetKnowledgeBaseStatusQuery request, CancellationToken cancellationToken)
        => indexer.GetStatusAsync(cancellationToken);
}
