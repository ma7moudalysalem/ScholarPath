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

        // Blocker B5: التأكد أن المستخدم الحالي هو صاحب المنحة
        if (entity.OwnerCompanyId != user.UserId)
            throw new ForbiddenAccessException();

        // منع التعديلات الجوهرية (Schema) إذا كان هناك تقديمات نشطة
        if (entity.Applications.Any() && entity.CategoryId != request.CategoryId)
            throw new ConflictException("Cannot change scholarship category while applications are in progress.");

        entity.TitleEn = request.TitleEn;
        entity.TitleAr = request.TitleAr;
        entity.DescriptionEn = request.DescriptionEn;
        entity.DescriptionAr = request.DescriptionAr;
        entity.Deadline = request.Deadline;
        entity.CategoryId = request.CategoryId;

        // ملاحظة: تم إزالة منطق الـ ArchivedAt من هنا لأنه Update وليس Archive

        await db.SaveChangesAsync(ct);
        return true;
    }
}
