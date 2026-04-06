using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class CompanyReview : AuditableEntity, ISoftDeletable
{
    public Guid ApplicationTrackerId { get; set; } // one review per finalized application
    public Guid StudentId { get; set; }
    public Guid CompanyId { get; set; }
    public int Rating { get; set; } // 1..5
    public string? Comment { get; set; }
    public bool IsHiddenByAdmin { get; set; }
    public string? AdminNote { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public ApplicationTracker? ApplicationTracker { get; set; }
    public ApplicationUser? Student { get; set; }
    public ApplicationUser? Company { get; set; }
}

public class CompanyReviewPayment : AuditableEntity
{
    public Guid ApplicationTrackerId { get; set; }
    public Guid CompanyId { get; set; }
    public decimal AmountUsd { get; set; }
    public decimal ProfitShareAmountUsd { get; set; }
    public decimal PayeeAmountUsd { get; set; }
    public string StripePaymentIntentId { get; set; } = default!;
    public string IdempotencyKey { get; set; } = default!;
    public Enums.PaymentStatus Status { get; set; }
    public DateTimeOffset? CapturedAt { get; set; }
    public decimal? RefundedAmountUsd { get; set; }
    public string? RefundReason { get; set; }
}

public class ConsultantReview : AuditableEntity, ISoftDeletable
{
    public Guid BookingId { get; set; } // one review per completed booking
    public Guid StudentId { get; set; }
    public Guid ConsultantId { get; set; }
    public int Rating { get; set; } // 1..5
    public string? Comment { get; set; }
    public bool IsHiddenByAdmin { get; set; }
    public string? AdminNote { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public ConsultantBooking? Booking { get; set; }
    public ApplicationUser? Student { get; set; }
    public ApplicationUser? Consultant { get; set; }
}
