using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Events;

public record ScholarshipPublishedEvent(Guid ScholarshipId, Guid? OwnerCompanyId) : DomainEvent;
public record ScholarshipArchivedEvent(Guid ScholarshipId) : DomainEvent;
public record ScholarshipFeaturedEvent(Guid ScholarshipId, int FeaturedOrder) : DomainEvent;

public record ApplicationSubmittedEvent(Guid ApplicationId, Guid StudentId, Guid ScholarshipId) : DomainEvent;
public record ApplicationStatusChangedEvent(Guid ApplicationId, Guid StudentId, Guid ScholarshipId, ApplicationStatus OldStatus, ApplicationStatus NewStatus) : DomainEvent;
public record ApplicationWithdrawnEvent(Guid ApplicationId, Guid StudentId, Guid ScholarshipId) : DomainEvent;

public record BookingRequestedEvent(Guid BookingId, Guid StudentId, Guid ConsultantId) : DomainEvent;
public record BookingConfirmedEvent(Guid BookingId, Guid StudentId, Guid ConsultantId) : DomainEvent;
public record BookingRejectedEvent(Guid BookingId, Guid StudentId, Guid ConsultantId) : DomainEvent;
public record BookingExpiredEvent(Guid BookingId, Guid StudentId, Guid ConsultantId) : DomainEvent;
public record BookingCancelledEvent(Guid BookingId, Guid StudentId, Guid ConsultantId, Guid CancelledByUserId, string? Reason) : DomainEvent;
public record BookingCompletedEvent(Guid BookingId, Guid StudentId, Guid ConsultantId) : DomainEvent;

public record CompanyRatingSubmittedEvent(Guid CompanyReviewId, Guid CompanyId, Guid StudentId, int Rating) : DomainEvent;
public record ConsultantRatingSubmittedEvent(Guid ConsultantReviewId, Guid ConsultantId, Guid StudentId, int Rating) : DomainEvent;

public record PaymentCapturedEvent(Guid PaymentId, PaymentType Type, long AmountCents, Guid PayerUserId, Guid? PayeeUserId) : DomainEvent;
public record PaymentRefundedEvent(Guid PaymentId, long RefundedAmountCents, string? Reason) : DomainEvent;
public record PayoutInitiatedEvent(Guid PayoutId, Guid PayeeUserId, long AmountCents) : DomainEvent;

public record PostAutoHiddenEvent(Guid PostId, int FlagCount) : DomainEvent;

public record ResourcePublishedEvent(Guid ResourceId, Guid AuthorUserId) : DomainEvent;
public record ResourceRejectedEvent(Guid ResourceId, Guid AuthorUserId, string Reason) : DomainEvent;

public record AdminBroadcastSentEvent(Guid BroadcastId, int RecipientCount) : DomainEvent;

public record ForumPostCreatedEvent(Guid PostId, Guid AuthorId, Guid CategoryId) : DomainEvent;
public record ForumReplyCreatedEvent(Guid ReplyId, Guid ParentPostId, Guid AuthorId) : DomainEvent;
