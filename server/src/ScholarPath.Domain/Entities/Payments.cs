using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class Payment : AuditableEntity, ISoftDeletable
{
    public PaymentType Type { get; set; }
    public PaymentStatus Status { get; set; }

    // Amounts stored in cents (smallest currency unit) to avoid FP rounding
    public long AmountCents { get; set; }
    public string Currency { get; set; } = "USD";
    public long ProfitShareAmountCents { get; set; }
    public long PayeeAmountCents { get; set; }
    public long RefundedAmountCents { get; set; }

    public Guid PayerUserId { get; set; }
    public Guid? PayeeUserId { get; set; } // Consultant or Company

    // Stripe identifiers
    public string? StripePaymentIntentId { get; set; }
    public string? StripeChargeId { get; set; }
    public string IdempotencyKey { get; set; } = default!;

    // Related business record (booking or application)
    public Guid? RelatedBookingId { get; set; }
    public Guid? RelatedApplicationId { get; set; }

    // Lifecycle
    public DateTimeOffset? HeldAt { get; set; }
    public DateTimeOffset? CapturedAt { get; set; }
    public DateTimeOffset? RefundedAt { get; set; }
    public string? RefundReason { get; set; }
    public string? FailureReason { get; set; }

    // Settlement — set when this payment is included in a payout batch (null = not yet paid out)
    public Guid? PayoutId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}

public class Payout : AuditableEntity
{
    public Guid PayeeUserId { get; set; }
    public long AmountCents { get; set; }
    public string Currency { get; set; } = "USD";
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;
    public string? StripePayoutId { get; set; }
    public string? StripeConnectAccountId { get; set; }
    public DateTimeOffset? InitiatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public string? FailureReason { get; set; }

    // Aggregates N payments into one payout
    public string? IncludedPaymentIdsJson { get; set; }
}

public class StripeWebhookEvent : BaseEntity
{
    public string StripeEventId { get; set; } = default!; // unique — idempotency key
    public string EventType { get; set; } = default!;
    public string RawPayload { get; set; } = default!;
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
    public string? ProcessingError { get; set; }
    public int ProcessingAttempts { get; set; }
}

public class ProfitShareConfig : AuditableEntity
{
    public PaymentType PaymentType { get; set; }
    public decimal Percentage { get; set; } // e.g., 0.10 = 10%
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; } // null = currently active
    public Guid SetByAdminId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Admin-configured financial rule (FR-163..176): the platform fee and portal
/// profit-share for a payment type. The fee is either a percentage of the gross
/// or a flat amount — the admin chooses per rule (<see cref="FeeKind"/>); the
/// profit-share is always a percentage. At most one rule per payment type may
/// be <see cref="FinancialRuleStatus.Active"/> at a time.
/// </summary>
public class FinancialConfigRule : AuditableEntity
{
    public PaymentType PaymentType { get; set; }

    /// <summary>Whether <see cref="FeePercentage"/> or <see cref="FeeAmountCents"/> applies.</summary>
    public FeeKind FeeKind { get; set; }

    /// <summary>Platform fee as a fraction of the gross (0–1) — set when FeeKind is Percentage.</summary>
    public decimal? FeePercentage { get; set; }

    /// <summary>Platform fee in cents — set when FeeKind is FixedAmount.</summary>
    public long? FeeAmountCents { get; set; }

    /// <summary>Portal profit-share as a fraction of the gross (0–1) — always a percentage.</summary>
    public decimal ProfitSharePercentage { get; set; }

    public FinancialRuleStatus Status { get; set; } = FinancialRuleStatus.Draft;
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }

    public Guid SetByAdminId { get; set; }
    public string? Notes { get; set; }
}
