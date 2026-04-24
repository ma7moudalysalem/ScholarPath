using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Commands.SendBroadcast;

/// <summary>
/// Admin broadcast to every Active user on the platform, optionally narrowed
/// to a single role. Maps to NotificationType.Broadcast (see Enums.cs, 900)
/// and hits the platform's INotificationDispatcher, so it respects each user's
/// NotificationPreferences per channel.
/// </summary>
[Auditable(AuditAction.Create, "Broadcast",
    SummaryTemplate = "Admin broadcast sent: {TitleEn}")]
public sealed record SendBroadcastCommand(
    string TitleEn,
    string TitleAr,
    string BodyEn,
    string BodyAr,
    string? TargetRole) : IRequest<int>;   // returns recipient count
