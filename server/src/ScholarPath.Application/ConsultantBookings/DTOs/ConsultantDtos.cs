namespace ScholarPath.Application.ConsultantBookings.DTOs;

// ─── Consultant marketplace read models ───────────────────────────────────────

/// <summary>
/// One consultant card in the public browse list (<c>GET /api/consultants</c>).
/// Profile summary projected from <see cref="Domain.Entities.ApplicationUser"/>
/// + <see cref="Domain.Entities.UserProfile"/>, with the rating aggregated from
/// non-hidden <see cref="Domain.Entities.ConsultantReview"/> rows.
/// </summary>
public sealed record ConsultantSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? PhotoUrl { get; init; }

    /// <summary>Short bio / headline from the consultant's profile.</summary>
    public string? Biography { get; init; }

    /// <summary>Expertise tag labels (parsed from <c>UserProfile.ExpertiseTagsJson</c>).</summary>
    public IReadOnlyList<string> ExpertiseTags { get; init; } = [];

    /// <summary>Spoken languages (parsed from <c>UserProfile.LanguagesJson</c>).</summary>
    public IReadOnlyList<string> Languages { get; init; } = [];

    /// <summary>Per-session fee in USD, or null when the consultant has not set one.</summary>
    public decimal? SessionFeeUsd { get; init; }

    /// <summary>Default session length in minutes, or null when not configured.</summary>
    public int? SessionDurationMinutes { get; init; }

    /// <summary>Average rating across visible reviews (1–5), null when none yet.</summary>
    public double? AverageRating { get; init; }

    /// <summary>Count of visible reviews the <see cref="AverageRating"/> is based on.</summary>
    public int ReviewCount { get; init; }

    /// <summary>Completed-session count — a lightweight "experience" signal.</summary>
    public int CompletedSessionCount { get; init; }

    /// <summary>Number of currently-active availability rules (recurring + ad-hoc).</summary>
    public int ActiveAvailabilityRuleCount { get; init; }

    /// <summary>True when the consultant has at least one active availability rule.</summary>
    public bool HasAvailability { get; init; }
}

/// <summary>
/// Full consultant profile detail (<c>GET /api/consultants/{id}</c>) — the
/// <see cref="ConsultantSummaryDto"/> fields plus the longer-form profile data
/// shown on the consultant detail page.
/// </summary>
public sealed record ConsultantDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? PhotoUrl { get; init; }
    public string? CountryOfResidence { get; init; }

    public string? Biography { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? Timezone { get; init; }

    public IReadOnlyList<string> ExpertiseTags { get; init; } = [];
    public IReadOnlyList<string> Languages { get; init; } = [];

    public decimal? SessionFeeUsd { get; init; }
    public int? SessionDurationMinutes { get; init; }

    public double? AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int CompletedSessionCount { get; init; }

    public bool HasAvailability { get; init; }

    /// <summary>The consultant's most recent visible reviews (newest first).</summary>
    public IReadOnlyList<ConsultantReviewDto> RecentReviews { get; init; } = [];
}

/// <summary>
/// One public review row shown on a consultant's detail page. Projected from a
/// non-hidden <see cref="Domain.Entities.ConsultantReview"/>.
/// </summary>
public sealed record ConsultantReviewDto
{
    public Guid Id { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public string StudentName { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; }
}
