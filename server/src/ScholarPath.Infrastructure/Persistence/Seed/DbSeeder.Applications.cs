using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Seed;

public static partial class DbSeeder
{
    /// <summary>
    /// Seeds <see cref="ApplicationTracker"/> rows covering EVERY
    /// <see cref="ApplicationStatus"/> — the in-app workflow states
    /// (Draft / Pending / UnderReview / Shortlisted / Accepted / Rejected /
    /// Withdrawn) and the external self-tracked states
    /// (Intending / Applied / WaitingResult) — plus a status-history child
    /// trail per application.
    /// <para>
    /// The DB enforces a unique filtered index on <c>(StudentId, ScholarshipId)</c>
    /// for non-terminal statuses, so every (student, scholarship) PAIR used here
    /// is distinct — that keeps the seed safe regardless of which states are
    /// terminal.
    /// </para>
    /// </summary>
    private static async Task<IReadOnlyList<ApplicationTracker>> SeedApplicationsAsync(
        ApplicationDbContext db, DemoUsers users, IReadOnlyList<Scholarship> scholarships,
        ILogger logger, CancellationToken ct)
    {
        var existing = await db.Applications.IgnoreQueryFilters().ToListAsync(ct).ConfigureAwait(false);
        if (existing.Count > 0)
        {
            return existing;
        }

        var now = DateTimeOffset.UtcNow;

        // Scholarships to apply against. In-app ones for InApp applications,
        // an external one for the self-tracked external states.
        var inApp = scholarships.Where(s => s.Mode == ListingMode.InApp).ToList();
        var external = scholarships.Where(s => s.Mode == ListingMode.ExternalUrl).ToList();
        if (inApp.Count == 0)
        {
            return [];
        }

        Scholarship InApp(int i) => inApp[i % inApp.Count];
        Scholarship Ext(int i) => external.Count > 0 ? external[i % external.Count] : inApp[i % inApp.Count];

        const string formData = """{"motivation":"I am passionate about advancing my field and contributing to my community.","gpa":3.7}""";
        const string attachedDocs = """["transcript.pdf","recommendation.pdf"]""";

        // Each tuple keeps a UNIQUE (student, scholarship) pair.
        var apps = new List<ApplicationTracker>
        {
            // --- in-app workflow states --------------------------------------
            // Draft — student is still filling the form
            new()
            {
                StudentId = users.Students[0].Id, ScholarshipId = InApp(0).Id,
                Mode = ApplicationMode.InApp, Status = ApplicationStatus.Draft,
                FormDataJson = formData,
                PersonalNotes = "Need to upload my updated transcript before submitting.",
                CreatedAt = now.AddDays(-3),
            },
            // Pending — submitted, awaiting company action
            new()
            {
                StudentId = users.Students[0].Id, ScholarshipId = InApp(1).Id,
                Mode = ApplicationMode.InApp, Status = ApplicationStatus.Pending,
                FormDataJson = formData, AttachedDocumentsJson = attachedDocs,
                SubmittedAt = now.AddDays(-7),
                NextReminderAt = now.AddDays(7),
                PersonalNotes = "Submitted on time. Fingers crossed.",
                CreatedAt = now.AddDays(-8),
            },
            // UnderReview — company has started reviewing
            new()
            {
                StudentId = users.Students[1].Id, ScholarshipId = InApp(0).Id,
                Mode = ApplicationMode.InApp, Status = ApplicationStatus.UnderReview,
                FormDataJson = formData, AttachedDocumentsJson = attachedDocs,
                SubmittedAt = now.AddDays(-12), ReviewStartedAt = now.AddDays(-4),
                CreatedAt = now.AddDays(-13),
            },
            // Shortlisted — intermediate, company still to decide
            new()
            {
                StudentId = users.Students[1].Id, ScholarshipId = InApp(2).Id,
                Mode = ApplicationMode.InApp, Status = ApplicationStatus.Shortlisted,
                FormDataJson = formData, AttachedDocumentsJson = attachedDocs,
                SubmittedAt = now.AddDays(-18), ReviewStartedAt = now.AddDays(-9),
                CreatedAt = now.AddDays(-19),
            },
            // Accepted — terminal, read-only
            new()
            {
                StudentId = users.Students[2].Id, ScholarshipId = InApp(0).Id,
                Mode = ApplicationMode.InApp, Status = ApplicationStatus.Accepted,
                FormDataJson = formData, AttachedDocumentsJson = attachedDocs,
                SubmittedAt = now.AddDays(-40), ReviewStartedAt = now.AddDays(-30),
                DecisionAt = now.AddDays(-20), DecisionReason = "Outstanding academic record and a compelling statement.",
                CreatedAt = now.AddDays(-41),
            },
            // Rejected — terminal
            new()
            {
                StudentId = users.Students[3].Id, ScholarshipId = InApp(1).Id,
                Mode = ApplicationMode.InApp, Status = ApplicationStatus.Rejected,
                FormDataJson = formData, AttachedDocumentsJson = attachedDocs,
                SubmittedAt = now.AddDays(-35), ReviewStartedAt = now.AddDays(-25),
                DecisionAt = now.AddDays(-15), DecisionReason = "The applicant pool was extremely competitive this cycle.",
                CreatedAt = now.AddDays(-36),
            },
            // Withdrawn — terminal, student pulled out
            new()
            {
                StudentId = users.Students[3].Id, ScholarshipId = InApp(2).Id,
                Mode = ApplicationMode.InApp, Status = ApplicationStatus.Withdrawn,
                FormDataJson = formData,
                SubmittedAt = now.AddDays(-22), WithdrawnAt = now.AddDays(-10),
                CreatedAt = now.AddDays(-23),
            },

            // --- external self-tracked states --------------------------------
            // Intending — student plans to apply externally
            new()
            {
                StudentId = users.Students[0].Id, ScholarshipId = Ext(0).Id,
                Mode = ApplicationMode.External, Status = ApplicationStatus.Intending,
                ExternalTrackingUrl = "https://apply.futurefund.org/bridge-grant",
                PersonalNotes = "Planning to start this external application next month.",
                NextReminderAt = now.AddDays(14),
                CreatedAt = now.AddDays(-2),
            },
            // Applied — student has submitted on the external portal
            new()
            {
                StudentId = users.Students[1].Id, ScholarshipId = Ext(1).Id,
                Mode = ApplicationMode.External, Status = ApplicationStatus.Applied,
                ExternalTrackingUrl = "https://global-postdoc.example.org/status",
                ExternalReferenceId = "EXT-2026-00913",
                SubmittedAt = now.AddDays(-9),
                CreatedAt = now.AddDays(-10),
            },
            // WaitingResult — applied externally, awaiting the outcome
            new()
            {
                StudentId = users.Students[2].Id, ScholarshipId = Ext(0).Id,
                Mode = ApplicationMode.External, Status = ApplicationStatus.WaitingResult,
                ExternalTrackingUrl = "https://apply.futurefund.org/bridge-grant",
                ExternalReferenceId = "FF-BRIDGE-7741",
                SubmittedAt = now.AddDays(-28),
                PersonalNotes = "Results are expected by the end of the month.",
                CreatedAt = now.AddDays(-29),
            },
        };

        // Safety net — guarantee uniqueness of the (student, scholarship) pair.
        apps = apps.DistinctBy(a => (a.StudentId, a.ScholarshipId)).ToList();

        db.Applications.AddRange(apps);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Status-history + note child rows.
        var children = new List<ApplicationTrackerChild>();
        foreach (var a in apps)
        {
            children.Add(new ApplicationTrackerChild
            {
                ApplicationTrackerId = a.Id,
                ChildType = "StatusHistory",
                Title = "Application created",
                Content = "The application record was created.",
                OccurredAt = a.CreatedAt,
                ActorUserId = a.StudentId,
                SortOrder = 0,
            });

            if (a.SubmittedAt is { } submitted)
            {
                children.Add(new ApplicationTrackerChild
                {
                    ApplicationTrackerId = a.Id,
                    ChildType = "StatusHistory",
                    Title = "Submitted",
                    Content = a.Mode == ApplicationMode.External
                        ? "Marked as submitted on the external portal."
                        : "Application submitted for review.",
                    OccurredAt = submitted,
                    ActorUserId = a.StudentId,
                    SortOrder = 1,
                });
            }

            if (a.DecisionAt is { } decided)
            {
                children.Add(new ApplicationTrackerChild
                {
                    ApplicationTrackerId = a.Id,
                    ChildType = "StatusHistory",
                    Title = $"Decision: {a.Status}",
                    Content = a.DecisionReason,
                    OccurredAt = decided,
                    SortOrder = 2,
                });
            }
        }

        db.ApplicationChildren.AddRange(children);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {N} applications (+{C} history rows) covering all statuses and modes", apps.Count, children.Count);
        return apps;
    }
}
