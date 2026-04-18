using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Common.Auditing;

/// <summary>
/// Marks a command for the audit pipeline. Target id is resolved at runtime
/// by reading a property on the response (default: "Id") or the request.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AuditableAttribute : Attribute
{
    public AuditableAttribute(AuditAction action, string targetType)
    {
        Action = action;
        TargetType = targetType;
    }

    public AuditAction Action { get; }
    public string TargetType { get; }

    /// <summary>Property on the response/request to use as TargetId. Default "Id".</summary>
    public string TargetIdProperty { get; set; } = "Id";

    /// <summary>Short human summary template; may include {TargetId}.</summary>
    public string? SummaryTemplate { get; set; }

    /// <summary>Skip audit when the handler returns a null response.</summary>
    public bool SkipOnNull { get; set; } = true;
}
