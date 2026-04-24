using FluentValidation;
using MediatR;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Community.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string NameEn,
    string NameAr,
    string Slug,
    string? DescriptionEn,
    string? DescriptionAr,
    int DisplayOrder) : IRequest<Guid>;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(v => v.NameEn).NotEmpty().MaximumLength(100);
        RuleFor(v => v.NameAr).NotEmpty().MaximumLength(100);
        RuleFor(v => v.Slug).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateCategoryCommandHandler(
    IApplicationDbContext db)
    : IRequestHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var category = new ForumCategory
        {
            NameEn = request.NameEn,
            NameAr = request.NameAr,
            Slug = request.Slug,
            DescriptionEn = request.DescriptionEn,
            DescriptionAr = request.DescriptionAr,
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };

        db.ForumCategories.Add(category);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return category.Id;
    }
}
