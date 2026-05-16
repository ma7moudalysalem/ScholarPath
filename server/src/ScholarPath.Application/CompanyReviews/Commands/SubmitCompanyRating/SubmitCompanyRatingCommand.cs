using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.CompanyReviews.Commands.SubmitCompanyRating;

[Auditable(AuditAction.Create, "CompanyReview",
    TargetIdProperty = nameof(ApplicationId),
    SummaryTemplate = "Student rated company {CompanyId} with {Rating} stars")]
public sealed record SubmitCompanyRatingCommand(
    Guid ApplicationId,
    Guid CompanyId,
    int Rating,
    string? Comment) : IRequest<Guid>;
