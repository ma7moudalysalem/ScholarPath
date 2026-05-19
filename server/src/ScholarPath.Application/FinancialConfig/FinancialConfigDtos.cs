using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.FinancialConfig;

/// <summary>A single financial-configuration rule row (FR-163..176).</summary>
public sealed record FinancialConfigRuleDto(
    Guid Id,
    PaymentType PaymentType,
    FeeKind FeeKind,
    decimal? FeePercentage,
    long? FeeAmountCents,
    decimal ProfitSharePercentage,
    FinancialRuleStatus Status,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    Guid SetByAdminId,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

/// <summary>
/// Result of simulating a financial rule against a gross amount (FR-167/175):
/// the platform fee, the profit-share, and what the payee would actually
/// receive. <see cref="IsViable"/> is false when the platform take exceeds the
/// gross — i.e. the rule would leave the payee with a negative balance.
/// </summary>
public sealed record FinancialCalculationPreviewDto(
    Guid? RuleId,
    PaymentType PaymentType,
    long GrossAmountCents,
    FeeKind FeeKind,
    long FeeCents,
    long ProfitShareCents,
    long PlatformTotalCents,
    long PayeeNetCents,
    decimal EffectiveFeeRate,
    bool IsViable,
    bool UsedFallback);
