using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Events;

public record UserRegisteredEvent(Guid UserId, string Email) : DomainEvent;

public record UserLoggedInEvent(Guid UserId, string Email, string? IpAddress) : DomainEvent;

public record UserLockedOutEvent(Guid UserId, string Email, DateTimeOffset LockoutEndUtc) : DomainEvent;

public record PasswordResetRequestedEvent(Guid UserId, string Email) : DomainEvent;

public record PasswordResetCompletedEvent(Guid UserId) : DomainEvent;

public record OnboardingCompletedEvent(Guid UserId, string ChosenRole, AccountStatus ResultingStatus) : DomainEvent;

public record UpgradeRequestSubmittedEvent(Guid UserId, Guid UpgradeRequestId, UpgradeTarget Target) : DomainEvent;

public record UpgradeRequestApprovedEvent(Guid UserId, Guid UpgradeRequestId, UpgradeTarget Target) : DomainEvent;

public record UpgradeRequestRejectedEvent(Guid UserId, Guid UpgradeRequestId, UpgradeTarget Target, string? Reason) : DomainEvent;

public record ProfileUpdatedEvent(Guid UserId) : DomainEvent;

public record ProfilePhotoChangedEvent(Guid UserId, string NewUrl) : DomainEvent;
