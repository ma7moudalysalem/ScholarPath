using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.DTOs;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Read projections for the public consultant marketplace (PB-006). Lives in
/// Infrastructure because identifying <c>Consultant</c>-role users requires the
/// Identity join-tables (<c>AspNetUserRoles</c> / <c>AspNetRoles</c>), which are
/// deliberately kept off <see cref="IApplicationDbContext"/>. Mirrors the
/// pattern of <see cref="AdminReadService"/>.
/// </summary>
public sealed class ConsultantReadService(
    ApplicationDbContext db,
    IDateTimeService clock,
    IHttpContextAccessor httpContext) : IConsultantReadService
{
    private const string ConsultantRole = "Consultant";

    /// <summary>How far ahead recurring availability is expanded into dated slots.</summary>
    private const int OpenSlotHorizonDays = 28;

    /// <summary>Newest visible reviews surfaced on a consultant's detail page.</summary>
    private const int RecentReviewCount = 10;

    /// <summary>Resolves the request UI language ("ar" / "en") from Accept-Language.</summary>
    private string CurrentLang()
    {
        var header = httpContext.HttpContext?.Request.Headers.AcceptLanguage.ToString()
                     ?? string.Empty;
        return header.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";
    }

    public async Task<IReadOnlyList<ConsultantSummaryDto>> BrowseConsultantsAsync(CancellationToken ct)
    {
        // Active users in the Consultant role — join via AspNetUserRoles → AspNetRoles.
        var consultants = await (
                from u in db.Users.AsNoTracking()
                join ur in db.UserRoles on u.Id equals ur.UserId
                join r in db.Roles on ur.RoleId equals r.Id
                where r.Name == ConsultantRole && u.AccountStatus == AccountStatus.Active
                select new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.ProfileImageUrl,
                    Profile = u.Profile,
                })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (consultants.Count == 0)
        {
            return Array.Empty<ConsultantSummaryDto>();
        }

        var ids = consultants.Select(c => c.Id).ToList();
        var ratings = await LoadRatingAggregatesAsync(ids, ct).ConfigureAwait(false);
        var completed = await LoadCompletedCountsAsync(ids, ct).ConfigureAwait(false);
        var availabilityCounts = await LoadActiveAvailabilityCountsAsync(ids, ct).ConfigureAwait(false);

        var lang = CurrentLang();
        return consultants
            .Select(c =>
            {
                var rating = ratings.GetValueOrDefault(c.Id);
                var ruleCount = availabilityCounts.GetValueOrDefault(c.Id);
                return new ConsultantSummaryDto
                {
                    Id = c.Id,
                    Name = $"{c.FirstName} {c.LastName}".Trim(),
                    PhotoUrl = c.ProfileImageUrl,
                    Biography = lang == "ar"
                        ? c.Profile?.BiographyAr ?? c.Profile?.Biography
                        : c.Profile?.Biography ?? c.Profile?.BiographyAr,
                    ExpertiseTags = ParseJsonArray(c.Profile?.ExpertiseTagsJson),
                    Languages = ParseJsonArray(c.Profile?.LanguagesJson),
                    SessionFeeUsd = c.Profile?.SessionFeeUsd,
                    SessionDurationMinutes = c.Profile?.SessionDurationMinutes,
                    AverageRating = rating.Count > 0 ? rating.Average : null,
                    ReviewCount = rating.Count,
                    CompletedSessionCount = completed.GetValueOrDefault(c.Id),
                    ActiveAvailabilityRuleCount = ruleCount,
                    HasAvailability = ruleCount > 0,
                };
            })
            .OrderByDescending(c => c.AverageRating ?? 0d)
            .ThenBy(c => c.Name)
            .ToList();
    }

    public async Task<ConsultantDetailDto?> GetConsultantDetailAsync(
        Guid consultantId, CancellationToken ct)
    {
        var consultant = await (
                from u in db.Users.AsNoTracking()
                join ur in db.UserRoles on u.Id equals ur.UserId
                join r in db.Roles on ur.RoleId equals r.Id
                where r.Name == ConsultantRole
                      && u.Id == consultantId
                      && u.AccountStatus == AccountStatus.Active
                select new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.ProfileImageUrl,
                    u.CountryOfResidence,
                    Profile = u.Profile,
                })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (consultant is null)
        {
            return null;
        }

        var ids = new List<Guid> { consultantId };
        var ratings = await LoadRatingAggregatesAsync(ids, ct).ConfigureAwait(false);
        var completed = await LoadCompletedCountsAsync(ids, ct).ConfigureAwait(false);
        var availabilityCounts = await LoadActiveAvailabilityCountsAsync(ids, ct).ConfigureAwait(false);

        var recentReviews = await db.ConsultantReviews
            .AsNoTracking()
            .Where(rev => rev.ConsultantId == consultantId
                          && !rev.IsHiddenByAdmin
                          && !rev.IsDeleted)
            .OrderByDescending(rev => rev.CreatedAt)
            .ThenByDescending(rev => rev.Id)
            .Take(RecentReviewCount)
            .Select(rev => new ConsultantReviewDto
            {
                Id = rev.Id,
                Rating = rev.Rating,
                Comment = rev.Comment,
                StudentName = rev.Student!.FirstName + " " + rev.Student.LastName,
                CreatedAt = rev.CreatedAt,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var aggregate = ratings.GetValueOrDefault(consultantId);
        var ruleCount = availabilityCounts.GetValueOrDefault(consultantId);
        var lang = CurrentLang();

        return new ConsultantDetailDto
        {
            Id = consultant.Id,
            Name = $"{consultant.FirstName} {consultant.LastName}".Trim(),
            PhotoUrl = consultant.ProfileImageUrl,
            CountryOfResidence = consultant.CountryOfResidence,
            Biography = lang == "ar"
                ? consultant.Profile?.BiographyAr ?? consultant.Profile?.Biography
                : consultant.Profile?.Biography ?? consultant.Profile?.BiographyAr,
            LinkedInUrl = consultant.Profile?.LinkedInUrl,
            WebsiteUrl = consultant.Profile?.WebsiteUrl,
            Timezone = consultant.Profile?.Timezone,
            ExpertiseTags = ParseJsonArray(consultant.Profile?.ExpertiseTagsJson),
            Languages = ParseJsonArray(consultant.Profile?.LanguagesJson),
            SessionFeeUsd = consultant.Profile?.SessionFeeUsd,
            SessionDurationMinutes = consultant.Profile?.SessionDurationMinutes,
            AverageRating = aggregate.Count > 0 ? aggregate.Average : null,
            ReviewCount = aggregate.Count,
            CompletedSessionCount = completed.GetValueOrDefault(consultantId),
            HasAvailability = ruleCount > 0,
            RecentReviews = recentReviews,
        };
    }

    public async Task<IReadOnlyList<BookableSlotDto>?> GetConsultantOpenSlotsAsync(
        Guid consultantId, CancellationToken ct)
    {
        var isConsultant = await (
                from u in db.Users.AsNoTracking()
                join ur in db.UserRoles on u.Id equals ur.UserId
                join r in db.Roles on ur.RoleId equals r.Id
                where r.Name == ConsultantRole
                      && u.Id == consultantId
                      && u.AccountStatus == AccountStatus.Active
                select u.Id)
            .AnyAsync(ct)
            .ConfigureAwait(false);

        if (!isConsultant)
        {
            return null;
        }

        var nowUtc = clock.UtcNow;
        var horizonUtc = nowUtc.AddDays(OpenSlotHorizonDays);

        var rules = await db.Availabilities
            .AsNoTracking()
            .Where(a => a.ConsultantId == consultantId && !a.IsDeleted && a.IsActive)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (rules.Count == 0)
        {
            return Array.Empty<BookableSlotDto>();
        }

        // Windows already taken by a live (non-cancelled / non-terminal) booking.
        var blockingStatuses = new[] { BookingStatus.Requested, BookingStatus.Confirmed };
        var bookedWindows = await db.Bookings
            .AsNoTracking()
            .Where(b => b.ConsultantId == consultantId
                        && !b.IsDeleted
                        && blockingStatuses.Contains(b.Status)
                        && b.ScheduledEndAt > nowUtc)
            .Select(b => new { b.ScheduledStartAt, b.ScheduledEndAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var slots = new List<BookableSlotDto>();

        foreach (var rule in rules)
        {
            if (rule.IsRecurring)
            {
                slots.AddRange(ExpandRecurringRule(rule, nowUtc, horizonUtc));
            }
            else
            {
                var adHoc = TryBuildAdHocSlot(rule, nowUtc, horizonUtc);
                if (adHoc is not null)
                {
                    slots.Add(adHoc);
                }
            }
        }

        // Drop any slot overlapping an existing live booking.
        var open = slots
            .Where(s => !bookedWindows.Any(w =>
                s.StartAt < w.ScheduledEndAt && s.EndAt > w.ScheduledStartAt))
            .OrderBy(s => s.StartAt)
            .ToList();

        return open;
    }

    // ── Recurring / ad-hoc expansion ────────────────────────────────────────────

    /// <summary>
    /// Expands a weekly-recurring rule into one concrete slot per matching
    /// weekday between now and the horizon. The recurring rule's
    /// <see cref="TimeOnly"/> window is interpreted as a UTC wall-clock time —
    /// the stored <c>Timezone</c> is surfaced on the DTO so the client can
    /// localise it for display.
    /// </summary>
    private static IEnumerable<BookableSlotDto> ExpandRecurringRule(
        Domain.Entities.ConsultantAvailability rule,
        DateTimeOffset nowUtc,
        DateTimeOffset horizonUtc)
    {
        if (rule.DayOfWeek is null || rule.StartTime is null || rule.EndTime is null)
        {
            yield break;
        }

        if (rule.EndTime.Value <= rule.StartTime.Value)
        {
            yield break;
        }

        var durationMinutes = (int)Math.Round(
            (rule.EndTime.Value.ToTimeSpan() - rule.StartTime.Value.ToTimeSpan()).TotalMinutes);

        for (var date = nowUtc.UtcDateTime.Date;
             date <= horizonUtc.UtcDateTime.Date;
             date = date.AddDays(1))
        {
            if (date.DayOfWeek != rule.DayOfWeek.Value)
            {
                continue;
            }

            var startUtc = new DateTimeOffset(
                date.Add(rule.StartTime.Value.ToTimeSpan()), TimeSpan.Zero);
            var endUtc = new DateTimeOffset(
                date.Add(rule.EndTime.Value.ToTimeSpan()), TimeSpan.Zero);

            // Skip windows already in the past or beyond the horizon.
            if (startUtc <= nowUtc || startUtc > horizonUtc)
            {
                continue;
            }

            yield return new BookableSlotDto
            {
                AvailabilityId = rule.Id,
                StartAt = startUtc,
                EndAt = endUtc,
                DurationMinutes = durationMinutes,
                IsRecurring = true,
                Timezone = rule.Timezone,
            };
        }
    }

    /// <summary>
    /// Builds a single slot from an ad-hoc rule when its concrete window is
    /// valid and still in the future (within the horizon). Returns
    /// <see langword="null"/> otherwise.
    /// </summary>
    private static BookableSlotDto? TryBuildAdHocSlot(
        Domain.Entities.ConsultantAvailability rule,
        DateTimeOffset nowUtc,
        DateTimeOffset horizonUtc)
    {
        if (rule.SpecificStartAt is null || rule.SpecificEndAt is null)
        {
            return null;
        }

        var startUtc = rule.SpecificStartAt.Value.ToUniversalTime();
        var endUtc = rule.SpecificEndAt.Value.ToUniversalTime();

        if (endUtc <= startUtc || startUtc <= nowUtc || startUtc > horizonUtc)
        {
            return null;
        }

        return new BookableSlotDto
        {
            AvailabilityId = rule.Id,
            StartAt = startUtc,
            EndAt = endUtc,
            DurationMinutes = (int)Math.Round((endUtc - startUtc).TotalMinutes),
            IsRecurring = false,
            Timezone = rule.Timezone,
        };
    }

    // ── Aggregate loaders (batched, no N+1) ─────────────────────────────────────

    private async Task<Dictionary<Guid, (int Count, double Average)>> LoadRatingAggregatesAsync(
        IReadOnlyCollection<Guid> consultantIds, CancellationToken ct)
    {
        var rows = await db.ConsultantReviews
            .AsNoTracking()
            .Where(rev => consultantIds.Contains(rev.ConsultantId)
                          && !rev.IsHiddenByAdmin
                          && !rev.IsDeleted)
            .GroupBy(rev => rev.ConsultantId)
            .Select(g => new
            {
                ConsultantId = g.Key,
                Count = g.Count(),
                Average = g.Average(rev => (double)rev.Rating),
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.ToDictionary(
            x => x.ConsultantId,
            x => (x.Count, Math.Round(x.Average, 2)));
    }

    private async Task<Dictionary<Guid, int>> LoadCompletedCountsAsync(
        IReadOnlyCollection<Guid> consultantIds, CancellationToken ct)
    {
        var rows = await db.Bookings
            .AsNoTracking()
            .Where(b => consultantIds.Contains(b.ConsultantId)
                        && !b.IsDeleted
                        && b.Status == BookingStatus.Completed)
            .GroupBy(b => b.ConsultantId)
            .Select(g => new { ConsultantId = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.ToDictionary(x => x.ConsultantId, x => x.Count);
    }

    private async Task<Dictionary<Guid, int>> LoadActiveAvailabilityCountsAsync(
        IReadOnlyCollection<Guid> consultantIds, CancellationToken ct)
    {
        var rows = await db.Availabilities
            .AsNoTracking()
            .Where(a => consultantIds.Contains(a.ConsultantId)
                        && !a.IsDeleted
                        && a.IsActive)
            .GroupBy(a => a.ConsultantId)
            .Select(g => new { ConsultantId = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.ToDictionary(x => x.ConsultantId, x => x.Count);
    }

    // ── JSON helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a stored JSON string array (<c>ExpertiseTagsJson</c> /
    /// <c>LanguagesJson</c>) into a string list. Defensive: any null, blank, or
    /// malformed value yields an empty list rather than throwing.
    /// </summary>
    private static IReadOnlyList<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<string[]>(json);
            return parsed is null
                ? Array.Empty<string>()
                : parsed.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
