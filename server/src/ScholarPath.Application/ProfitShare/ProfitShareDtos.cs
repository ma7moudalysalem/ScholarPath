using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ProfitShare;

/// <summary>A single profit-share configuration row (PB-014). Active when <see cref="IsActive"/>.</summary>
public sealed record ProfitShareConfigDto(
    Guid Id,
    PaymentType PaymentType,
    decimal Percentage,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    Guid SetByAdminId,
    string? Notes,
    bool IsActive);

/// <summary>Aggregated platform profit-share over a date window (PB-014 AC#6).</summary>
public sealed record ProfitShareAnalyticsDto(
    DateTimeOffset From,
    DateTimeOffset To,
    long TotalGrossCents,
    long TotalProfitShareCents,
    long TotalPayeeCents,
    int CapturedPaymentCount,
    IReadOnlyList<ProfitShareMonthlyBucket> Monthly);

public sealed record ProfitShareMonthlyBucket(
    int Year,
    int Month,
    long ProfitShareCents,
    long GrossCents,
    int PaymentCount);
