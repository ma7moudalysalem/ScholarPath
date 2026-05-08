using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Applications.Common;
using ScholarPath.Application.Common.Auditing; // تم تعديل المسار ليتوافق مع مشروعك
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Applications.Commands.WithdrawApplication;

// T-010: تسجيل عملية السحب في سجل النظام (Auditing)
[Auditable(AuditAction.Update,"Application")]
public sealed record WithdrawApplicationCommand(Guid ApplicationId) : IRequest;

public sealed class WithdrawApplicationCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<WithdrawApplicationCommand>
{
    public async Task Handle(WithdrawApplicationCommand request, CancellationToken ct)
    {
        // 1. تعديل الاستثناء ليكون متوافقاً مع C# Standard (UnauthorizedAccessException)
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        // 2. جلب الطلب مع التأكد من ملكية المستخدم له
        var application = await db.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && a.StudentId == userId, ct)
            ?? throw new NotFoundException("Application", request.ApplicationId);

        // 3. التحقق من منطق الأعمال (State Machine)
        ApplicationStateMachine.EnsureTransition(application.Status, ApplicationStatus.Withdrawn);

        var oldStatus = application.Status;
        application.Status = ApplicationStatus.Withdrawn;

        // تحديث حقل تاريخ السحب الموجود في الـ Entity الخاص بكِ
        application.WithdrawnAt = DateTimeOffset.UtcNow;

        // 4. الحل النهائي لإطلاق الحدث بناءً على ملف الـ BaseEntity الخاص بكِ
        // نستخدم RaiseDomainEvent ونمرر البارامترات الـ 5 المطلوبة في الـ Record
        application.RaiseDomainEvent(new ApplicationStatusChangedEvent(
            application.Id,
            application.StudentId,
            application.ScholarshipId,
            oldStatus,
            application.Status));

        await db.SaveChangesAsync(ct);
    }
}
