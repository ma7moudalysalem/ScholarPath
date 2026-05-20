using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Scholarships.Commands;

public record UpdateScholarshipCommand : IRequest<bool>
{
    public Guid Id { get; init; }
    public string TitleEn { get; init; } = default!;
    public string TitleAr { get; init; } = default!;
    public string DescriptionEn { get; init; } = default!;
    public string DescriptionAr { get; init; } = default!;
    public DateTimeOffset Deadline { get; init; }
    public Guid CategoryId { get; init; }
    /// <summary>Optional updated list of eligible academic fields of study.</summary>
    public string[]? FieldsOfStudy { get; init; }
}

public class UpdateScholarshipCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    : IRequestHandler<UpdateScholarshipCommand, bool>
{
    public async Task<bool> Handle(UpdateScholarshipCommand request, CancellationToken ct)
    {
        var entity = await db.Scholarships
            .Include(x => x.Applications)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (entity == null) throw new NotFoundException(nameof(Scholarship), request.Id);

        if (entity.OwnerCompanyId != user.UserId)
            throw new ForbiddenAccessException();

        if (entity.Applications.Any() && entity.CategoryId != request.CategoryId)
            throw new ConflictException("Cannot change scholarship category while applications are in progress.");

        entity.TitleEn = request.TitleEn;
        entity.TitleAr = request.TitleAr;
        entity.DescriptionEn = request.DescriptionEn;
        entity.DescriptionAr = request.DescriptionAr;
        entity.Deadline = request.Deadline;
        entity.CategoryId = request.CategoryId;
        entity.FieldsOfStudyJson = request.FieldsOfStudy is { Length: > 0 }
            ? System.Text.Json.JsonSerializer.Serialize(request.FieldsOfStudy)
            : null;

        await db.SaveChangesAsync(ct);
        return true;
    }
}
