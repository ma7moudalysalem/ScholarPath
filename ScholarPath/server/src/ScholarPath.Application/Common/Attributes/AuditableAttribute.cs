namespace ScholarPath.Application.Common.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class AuditableAttribute(AuditAction action, string entityType) : Attribute
{
    public AuditAction Action { get; } = action;
    public string EntityType { get; } = entityType;
}

public enum AuditAction
{
    Logout,
    PasswordResetRequested,
    PasswordResetCompleted,
    PasswordChanged,
    ProfileUpdated,
    ProfileImageUploaded,
    NotificationRead,
    AllNotificationsRead
}
