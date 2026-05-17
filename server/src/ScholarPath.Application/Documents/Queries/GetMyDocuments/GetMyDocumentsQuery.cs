using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Documents.Queries.GetMyDocuments;

/// <summary>Lists the authenticated caller's own vault documents (FR-216), newest first.</summary>
public sealed record GetMyDocumentsQuery(DocumentCategory? Category = null)
    : IRequest<IReadOnlyList<DocumentDto>>;

public sealed class GetMyDocumentsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    public async Task<IReadOnlyList<DocumentDto>> Handle(
        GetMyDocumentsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var query = db.Documents.AsNoTracking()
            .Where(d => d.OwnerUserId == userId);

        if (request.Category is { } category)
            query = query.Where(d => d.Category == category);

        var entities = await query
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(DocumentMapping.ToDto).ToList();
    }
}
