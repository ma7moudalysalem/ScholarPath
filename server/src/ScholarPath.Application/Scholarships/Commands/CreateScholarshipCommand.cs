using MediatR;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using FluentValidation;
using ScholarPath.Domain.Interfaces;
namespace ScholarPath.Application.Scholarships.Commands;

public record CreateScholarshipCommand : IRequest<Guid>
{
    public string TitleEn { get; init; } = null!;
    public string TitleAr { get; init; } = null!;
    public string DescriptionEn { get; init; } = null!;
    public string DescriptionAr { get; init; } = null!;
    public Guid CategoryId { get; init; }
    public DateTimeOffset Deadline { get; init; }
    public FundingType FundingType { get; init; }
    public AcademicLevel TargetLevel { get; init; }
}

public class CreateScholarshipCommandValidator : AbstractValidator<CreateScholarshipCommand>
{
    public CreateScholarshipCommandValidator()
    {
        RuleFor(v => v.TitleEn).MaximumLength(300).NotEmpty();
        RuleFor(v => v.TitleAr).MaximumLength(300).NotEmpty();
        RuleFor(v => v.DescriptionEn).NotEmpty();
        RuleFor(v => v.CategoryId).NotEmpty();

        // Spec: deadline must be at least 7 days out. Evaluated per-request
        // (a lambda) — NOT captured once at validator construction.
        RuleFor(v => v.Deadline)
            .Must(deadline => deadline > DateTimeOffset.UtcNow.AddDays(7))
            .WithMessage("Deadline must be at least 7 days from now.");
    }
}

public class CreateScholarshipCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    : IRequestHandler<CreateScholarshipCommand, Guid>
{
    public async Task<Guid> Handle(CreateScholarshipCommand request, CancellationToken ct)
    {
        // Only an authenticated Company account can publish an in-app listing.
        if (!user.IsInRole("Company"))
            throw new ForbiddenAccessException("Only a Company account can create scholarship listings.");

        var ownerCompanyId = user.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var entity = new Scholarship
        {
            Id = Guid.NewGuid(),
            TitleEn = request.TitleEn,
            TitleAr = request.TitleAr,
            DescriptionEn = request.DescriptionEn,
            DescriptionAr = request.DescriptionAr,
            CategoryId = request.CategoryId,
            Deadline = request.Deadline,
            FundingType = request.FundingType,
            TargetLevel = request.TargetLevel,
            Status = ScholarshipStatus.Open,
            OwnerCompanyId = ownerCompanyId,
        };

        db.Scholarships.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
