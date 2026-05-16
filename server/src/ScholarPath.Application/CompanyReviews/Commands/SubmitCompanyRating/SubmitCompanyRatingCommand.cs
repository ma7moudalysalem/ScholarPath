using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.CompanyReviews.Commands.SubmitCompanyRating;

// CompanyId is intentionally NOT a request field — it is resolved server-side
// from the application's scholarship owner (prevents rating an arbitrary company).
[Auditable(AuditAction.Create, "CompanyReview",
    TargetIdProperty = nameof(ApplicationId),
    SummaryTemplate = "Student rated the company {Rating} stars")]
public sealed record SubmitCompanyRatingCommand(
    Guid ApplicationId,
    int Rating,
    string? Comment) : IRequest<Guid>;
