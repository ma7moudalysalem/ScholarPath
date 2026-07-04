using FluentValidation;
using MediatR;
using ScholarPath.Application.Common.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings;
using ScholarPath.Application.ConsultantBookings.Services;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Admin.Commands.ResolveNoShowReport;

/// <summary>
/// Admin validation of a no-show report (PB-006R, FR-CBR-25..32). This is the ONLY
/// place no-show penalties are applied:
/// <list type="bullet">
/// <item>Validated student no-show → 7-day student booking block, fee forfeited.</item>
/// <item>Validated consultant no-show → −40% consultant rating, full refund to student.</item>
/// <item>Falsely-reported student (consultant lied) → −70% consultant rating.</item>
/// <item>Falsely-reported consultant (student lied) → 14-day student booking block.</item>
/// </list>
/// </summary>
[Auditable(AuditAction.Update, "NoShowReport",
    TargetIdProperty = nameof(ReportId),
    SummaryTemplate = "Admin resolved no-show report {ReportId}")]
public sealed record ResolveNoShowReportCommand(Guid ReportId, bool IsValid, string? AdminNote)
    : IRequest;

public sealed class ResolveNoShowReportCommandValidator : AbstractValidator<ResolveNoShowReportCommand>
{
    public ResolveNoShowReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.AdminNote).MaximumLength(1000);
    }
}

public sealed class ResolveNoShowReportCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IStripeService stripe,
    ConsultantRatingService ratingService,
    INotificationDispatcher notifications,
    IOptions<BookingOptions> bookingOptions,
    ILogger<ResolveNoShowReportCommandHandler> logger)
    : IRequestHandler<ResolveNoShowReportCommand>
{
    private readonly BookingOptions _options = bookingOptions.Value;

    public async Task Handle(ResolveNoShowReportCommand command, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Admin") && !currentUser.IsInRole("SuperAdmin"))
        {
            throw new ForbiddenAccessException("Only an administrator can resolve a no-show report.");
        }

        var adminId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Authenticated admin id is missing.");

        var report = await db.NoShowReports
            .FirstOrDefaultAsync(r => r.Id == command.ReportId, ct)
            ?? throw new NotFoundException(nameof(NoShowReport), command.ReportId);

        if (report.Status != NoShowReportStatus.PendingReview)
        {
            throw new ConflictException("This no-show report has already been resolved.");
        }

        var booking = await db.Bookings
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == report.BookingId, ct)
            ?? throw new NotFoundException(nameof(ConsultantBooking), report.BookingId);

        var nowUtc = DateTimeOffset.UtcNow;
        Guid? consultantToDeduct = null;
        decimal deductionFactor = 1m;

        if (command.IsValid)
        {
            if (report.AccusedRole == NoShowAccusedRole.Student)
            {
                // Validated STUDENT no-show → 7-day block, fee forfeited (no refund).
                await BlockStudentAsync(booking.StudentId, BookingBlockReason.ValidatedNoShow,
                    _options.ValidatedNoShowBlockDays, nowUtc, ct);
                booking.IsNoShowStudent = true;
                booking.Status = BookingStatus.NoShowStudent;
                booking.CancellationReason = CancellationReason.StudentNoShow;
            }
            else
            {
                // Validated CONSULTANT no-show → −40% rating + full refund to student.
                await RefundStudentAsync(booking, ct);
                booking.IsNoShowConsultant = true;
                booking.Status = BookingStatus.NoShowConsultant;
                booking.CancellationReason = CancellationReason.ConsultantNoShow;
                consultantToDeduct = booking.ConsultantId;
                deductionFactor = ConsultantRatingThresholds.ValidatedNoShowFactor;
            }
        }
        else
        {
            // Report rejected as false → the REPORTER is penalized; the session is
            // treated as having happened, so the booking returns to Completed.
            if (report.AccusedRole == NoShowAccusedRole.Consultant)
            {
                // Student falsely accused the consultant → 14-day student block.
                await BlockStudentAsync(booking.StudentId, BookingBlockReason.FalseNoShowReport,
                    _options.FalseNoShowReportBlockDays, nowUtc, ct);
            }
            else
            {
                // Consultant falsely accused the student → −70% consultant rating.
                consultantToDeduct = booking.ConsultantId;
                deductionFactor = ConsultantRatingThresholds.FalseReportFactor;
            }
            booking.Status = BookingStatus.Completed;
        }

        report.Status = command.IsValid ? NoShowReportStatus.ValidatedNoShow : NoShowReportStatus.RejectedAsFalse;
        report.ResolvedByAdminId = adminId;
        report.ResolvedAt = nowUtc;
        report.AdminNote = string.IsNullOrWhiteSpace(command.AdminNote) ? null : command.AdminNote.Trim();

        await db.SaveChangesAsync(ct);

        // Apply the rating deduction last — ApplyPenaltyFactorAsync recomputes the
        // snapshot and commits, so all prior tracked changes persist with it.
        if (consultantToDeduct is { } consultantId)
        {
            await ratingService.ApplyPenaltyFactorAsync(consultantId, deductionFactor, ct);
        }

        logger.LogInformation(
            "Admin {AdminId} resolved no-show report {ReportId} as {Verdict} (accused {Role}).",
            adminId, report.Id, report.Status, report.AccusedRole);

        await NotifyPartiesAsync(report, ct);
    }

    private async Task BlockStudentAsync(
        Guid studentId, BookingBlockReason reason, int days, DateTimeOffset nowUtc, CancellationToken ct)
    {
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == studentId, ct);
        if (profile is null)
        {
            logger.LogWarning("UserProfile not found for student {StudentId} — booking block not applied.", studentId);
            return;
        }
        BookingBlockService.ApplyBlock(profile, reason, days, nowUtc);
    }

    private async Task RefundStudentAsync(ConsultantBooking booking, CancellationToken ct)
    {
        // Free booking (no Stripe intent / Payment row): nothing to refund.
        var isFree = booking.PriceUsd == 0m && booking.Payment is null;
        if (isFree)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(booking.StripePaymentIntentId))
        {
            throw new ConflictException("Booking has no Stripe payment intent to refund.");
        }

        // Refund the amount actually captured (the Payment row is authoritative).
        var amountCents = booking.Payment is { } capturedPayment
            ? capturedPayment.AmountCents
            : (long)decimal.Round(booking.PriceUsd * 100m, 0, MidpointRounding.AwayFromZero);

        if (amountCents < 0)
        {
            throw new ConflictException("Booking amount cannot be negative.");
        }

        var refundResult = await stripe.RefundPaymentAsync(
            paymentIntentId: booking.StripePaymentIntentId,
            amountCents: amountCents,
            reason: CancellationReason.ConsultantNoShow.ToString(),
            idempotencyKey: $"booking-noshow-refund:{booking.Id:N}",
            ct: ct);

        if (string.IsNullOrWhiteSpace(refundResult.Id))
        {
            throw new ConflictException("Stripe refund failed.");
        }

        if (booking.Payment is { } payment)
        {
            payment.Status = PaymentStatus.Refunded;
            payment.RefundedAmountCents = payment.AmountCents;
            payment.RefundedAt = DateTimeOffset.UtcNow;
            payment.RefundReason = CancellationReason.ConsultantNoShow.ToString();
            payment.ProfitShareAmountCents = 0;
            payment.PayeeAmountCents = 0;
        }
    }

    private async Task NotifyPartiesAsync(NoShowReport report, CancellationToken ct)
    {
        var parameters = new NotificationParams { Reason = report.Status.ToString() };
        foreach (var recipientId in new[] { report.ReporterUserId, report.AccusedUserId })
        {
            try
            {
                await notifications.DispatchAsync(
                    recipientId,
                    NotificationType.NoShowReportResolved,
                    parameters,
                    deepLink: "/bookings",
                    idempotencyKey: $"noshow-resolved:{report.Id:N}:{recipientId:N}",
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to dispatch NoShowReportResolved to {RecipientId} for report {ReportId}.",
                    recipientId, report.Id);
            }
        }
    }
}
