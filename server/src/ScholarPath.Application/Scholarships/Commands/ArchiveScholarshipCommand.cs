using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScholarPath.Application.Scholarships.Commands;

// 1. الـ Command يحتاج فقط الـ Id الخاص بالمنحة
public record ArchiveScholarshipCommand(Guid Id) : IRequest<bool>;

// 2. الـ Handler لتنفيذ عملية الأرشفة
public class ArchiveScholarshipCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService user) : IRequestHandler<ArchiveScholarshipCommand, bool>
{
    public async Task<bool> Handle(ArchiveScholarshipCommand request, CancellationToken ct)
    {
        // جلب المنحة من قاعدة البيانات
        var entity = await db.Scholarships
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        // إذا لم توجد المنحة، نلقي Exception
        if (entity == null)
        {
            throw new NotFoundException(nameof(Scholarship), request.Id);
        }

        // تنفيذ الأرشفة (Soft Delete)
        // نفترض وجود خاصية IsArchived أو IsDeleted في الـ Entity
        entity.IsDeleted = true;

        // تسجيل من قام بالأرشفة ووقتها
        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.DeletedByUserId = user.UserId;
        entity.ArchivedAt = DateTimeOffset.UtcNow;
        entity.Status = ScholarshipStatus.Archived;

        // حفظ التغييرات
        await db.SaveChangesAsync(ct);

        return true;
    }
}
