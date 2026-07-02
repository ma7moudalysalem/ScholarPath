using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Documents.Queries.GetUserOnboardingDocuments;

/// <summary>
/// Lists the verification documents a ScholarshipProvider or Consultant uploaded in support
/// of their onboarding request, newest first. Used by the admin onboarding-queue
/// reviewer; <c>AdminController</c> restricts the caller to Admin / SuperAdmin.
/// </summary>
public sealed record GetUserOnboardingDocumentsQuery(Guid UserId)
    : IRequest<IReadOnlyList<DocumentDto>>;

public sealed class GetUserOnboardingDocumentsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUserOnboardingDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    public async Task<IReadOnlyList<DocumentDto>> Handle(
        GetUserOnboardingDocumentsQuery request, CancellationToken ct)
    {
        var entities = await db.Documents.AsNoTracking()
            .Where(d => d.OwnerUserId == request.UserId
                        && d.Category == DocumentCategory.OnboardingDocument)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(DocumentMapping.ToDto).ToList();
    }
}
