using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Jobs;

public interface IDeadlineReminderJob
{
    Task RunAsync(CancellationToken ct);
}

/// <summary>
/// Daily sweep (FR-046). For every open scholarship whose deadline falls within the
/// next <see cref="ReminderWindowDays"/> days, reminds each student who bookmarked it
/// or has a live (non-terminal, non-draft) application that the deadline is near.
/// The dispatch <c>idempotencyKey</c> is keyed to the scholarship, recipient, and
/// deadline date, so the daily cron sends each reminder exactly once — and a fresh
/// reminder only if the deadline is later rescheduled.
/// </summary>
public sealed class DeadlineReminderJob(
    Persistence.ApplicationDbContext db,
    INotificationDispatcher notifications,
    ILogger<DeadlineReminderJob> logger) : IDeadlineReminderJob
{
    private const int ReminderWindowDays = 7;

    public async Task RunAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var windowEnd = now.AddDays(ReminderWindowDays);

        var closingSoon = await db.Scholarships
            .Where(s => s.Status == ScholarshipStatus.Open
                && s.Deadline > now
                && s.Deadline <= windowEnd)
            .Select(s => new { s.Id, s.TitleEn, s.TitleAr, s.Deadline })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (closingSoon.Count == 0)
        {
            logger.LogInformation("[deadline-reminder] no scholarships closing within {Days} days.",
                ReminderWindowDays);
            return;
        }

        var sent = 0;
        foreach (var scholarship in closingSoon)
        {
            // Students who bookmarked the listing.
            var bookmarkers = await db.SavedScholarships
                .Where(b => b.ScholarshipId == scholarship.Id)
                .Select(b => b.UserId)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            // Students with a live application — drafts are nudged by the draft job,
            // and a terminal application (accepted/rejected/withdrawn) needs no reminder.
            var applicants = await db.Applications
                .Where(a => a.ScholarshipId == scholarship.Id
                    && a.Status != ApplicationStatus.Draft
                    && a.Status != ApplicationStatus.Accepted
                    && a.Status != ApplicationStatus.Rejected
                    && a.Status != ApplicationStatus.Withdrawn)
                .Select(a => a.StudentId)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var recipients = bookmarkers.Concat(applicants).Distinct().ToList();
            var daysLeft = Math.Max(1, (int)Math.Ceiling((scholarship.Deadline - now).TotalDays));
            var deadlineStamp = scholarship.Deadline.UtcDateTime.ToString(
                "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);

            foreach (var recipientId in recipients)
            {
                try
                {
                    await notifications.DispatchAsync(
                        recipientId,
                        NotificationType.ApplicationDeadlineApproaching,
                        new NotificationParams
                        {
                            TitleEn = scholarship.TitleEn,
                            TitleAr = scholarship.TitleAr,
                            Count = daysLeft,
                        },
                        deepLink: $"/student/scholarships/{scholarship.Id}",
                        idempotencyKey: $"deadline-reminder:{scholarship.Id:N}:{recipientId:N}:{deadlineStamp}",
                        ct).ConfigureAwait(false);
                    sent++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "[deadline-reminder] dispatch failed for scholarship {ScholarshipId}, recipient {RecipientId}.",
                        scholarship.Id, recipientId);
                }
            }
        }

        logger.LogInformation(
            "[deadline-reminder] run complete — {Scholarships} scholarship(s), {Sent} reminder(s) dispatched.",
            closingSoon.Count, sent);
    }
}

public interface INotificationDispatcherJob
{
    Task RunAsync(CancellationToken ct);
}

/// <summary>
/// Daily draft-nudge sweep (FR-062). Reminds students who have an unsubmitted in-app
/// <c>Draft</c> application for a scholarship that is still open and whose deadline has
/// not yet passed. The dispatch <c>idempotencyKey</c> is keyed to the draft and the
/// scholarship deadline, so each draft is nudged once per deadline rather than every
/// daily tick. Status-change notifications fire synchronously elsewhere (event
/// handlers); this job covers the deadline/draft half of FR-062.
/// </summary>
public sealed class NotificationDispatcherJob(
    Persistence.ApplicationDbContext db,
    INotificationDispatcher notifications,
    ILogger<NotificationDispatcherJob> logger) : INotificationDispatcherJob
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var drafts = await db.Applications
            .Where(a => a.Status == ApplicationStatus.Draft
                && a.Mode == ApplicationMode.InApp
                && a.Scholarship != null
                && a.Scholarship.Status == ScholarshipStatus.Open
                && a.Scholarship.Deadline > now)
            .Select(a => new
            {
                a.Id,
                a.StudentId,
                a.ScholarshipId,
                ScholarshipTitleEn = a.Scholarship!.TitleEn,
                ScholarshipTitleAr = a.Scholarship!.TitleAr,
                Deadline = a.Scholarship!.Deadline,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (drafts.Count == 0)
        {
            logger.LogInformation("[draft-reminder] no open unsubmitted drafts to nudge.");
            return;
        }

        var sent = 0;
        foreach (var draft in drafts)
        {
            var deadlineStamp = draft.Deadline.UtcDateTime.ToString(
                "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
            try
            {
                await notifications.DispatchAsync(
                    draft.StudentId,
                    NotificationType.ApplicationDraftReminder,
                    new NotificationParams
                    {
                        TitleEn = draft.ScholarshipTitleEn,
                        TitleAr = draft.ScholarshipTitleAr,
                    },
                    deepLink: $"/student/applications/{draft.Id}",
                    idempotencyKey: $"draft-reminder:{draft.Id:N}:{deadlineStamp}",
                    ct).ConfigureAwait(false);
                sent++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "[draft-reminder] dispatch failed for draft application {ApplicationId}.", draft.Id);
            }
        }

        logger.LogInformation(
            "[draft-reminder] run complete — {Drafts} draft(s), {Sent} reminder(s) dispatched.",
            drafts.Count, sent);
    }
}

public interface IIntegrityCheckJob
{
    Task RunAsync(CancellationToken ct);
}

/// <summary>
/// Daily sweep for orphan / inconsistent rows. Surfaces as warnings that
/// the admin dashboard rolls up.
/// </summary>
public sealed class IntegrityCheckJob(
    Persistence.ApplicationDbContext db,
    ILogger<IntegrityCheckJob> logger) : IIntegrityCheckJob
{
    public async Task RunAsync(CancellationToken ct)
    {
        var orphanPayments = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(db.Payments
                .Where(p => p.RelatedBookingId == null && p.RelatedApplicationId == null), ct)
            .ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var overdueBookings = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(db.Bookings
                .Where(b => b.Status == Domain.Enums.BookingStatus.Confirmed
                    && b.ScheduledEndAt < now.AddHours(-6)), ct)
            .ConfigureAwait(false);

        var stuckWebhooks = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(db.StripeWebhookEvents
                .Where(e => !e.IsProcessed && e.ProcessingAttempts >= 5), ct)
            .ConfigureAwait(false);

        if (orphanPayments > 0 || overdueBookings > 0 || stuckWebhooks > 0)
        {
            logger.LogWarning(
                "[integrity] orphanPayments={OrphanPayments} overdueBookings={OverdueBookings} stuckWebhooks={StuckWebhooks}",
                orphanPayments, overdueBookings, stuckWebhooks);
        }
        else
        {
            logger.LogInformation("[integrity] clean sweep");
        }
    }
}
