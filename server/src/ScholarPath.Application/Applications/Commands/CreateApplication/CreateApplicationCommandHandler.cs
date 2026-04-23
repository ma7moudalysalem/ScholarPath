using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScholarPath.Application.Applications.Commands.CreateApplication
{
    public class CreateApplicationCommandHandler(
        IApplicationDbContext db,
        IMapper mapper,
        ICurrentUserService currentUser,
        IDateTimeService clock) : IRequestHandler<CreateApplicationCommand, ApplicationDto>
    {
        public async Task<ApplicationDto> Handle(CreateApplicationCommand request, CancellationToken ct)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenAccessException();

            var scholarship = await db.Scholarships
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.ScholarshipId, ct)
                ?? throw new NotFoundException(nameof(Scholarship), request.ScholarshipId);

            // ✅ التصحيح: استخدمنا Open بدلاً من Published
            if (scholarship.Status != ScholarshipStatus.Open)
                throw new ConflictException("عذراً، هذه المنحة غير متاحة لاستقبال طلبات حالياً.");

            var hasActiveApp = await db.Applications
                .AnyAsync(a => a.StudentId == userId &&
                               a.ScholarshipId == request.ScholarshipId &&
                               a.Status != ApplicationStatus.Rejected &&
                               a.Status != ApplicationStatus.Withdrawn, ct);

            if (hasActiveApp)
                throw new ConflictException("لديك طلب تقديم نشط بالفعل لهذه المنحة.");

            var entity = new ApplicationTracker
            {
                Id = Guid.NewGuid(),
                StudentId = userId,
                ScholarshipId = request.ScholarshipId,
                // ✅ التصحيح: استخدمنا Pending بدلاً من Submitted
                Status = ApplicationStatus.Pending,
                SubmittedAt = clock.UtcNow,
                PersonalNotes = request.PersonalNotes,
                Mode = scholarship.Mode == ListingMode.InApp?ApplicationMode.InApp:ApplicationMode.External
            };

            db.Applications.Add(entity);
            await db.SaveChangesAsync(ct);

            return mapper.Map<ApplicationDto>(entity);
        }
    }
}  
