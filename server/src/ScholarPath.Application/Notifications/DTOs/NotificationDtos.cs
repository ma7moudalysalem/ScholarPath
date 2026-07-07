namespace ScholarPath.Application.Notifications.DTOs;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string TitleEn,
    string TitleAr,
    string BodyEn,
    string BodyAr,
    string? DeepLink,
    bool IsRead,
    DateTimeOffset? ReadAt,
    int Priority,
    DateTimeOffset CreatedAt);

public sealed record NotificationsPageDto(
    IReadOnlyList<NotificationDto> Items,
    int Page,
    int PageSize,
    int Total,
    int UnreadCount);

/// <summary>
/// One delivery-preference row: a notification <see cref="Type"/> on a given
/// <see cref="Channel"/>, and whether the user has it enabled (FR-228). When no
/// stored row exists for a pair the channel defaults to enabled.
/// </summary>
public sealed record NotificationPreferenceDto(
    string Type,
    string Channel,
    bool IsEnabled);

/// <summary>
/// Global "do not disturb" settings (FR-228). Quiet times are "HH:mm" strings in
/// <see cref="QuietTimezone"/> (an IANA zone), or null when unset.
/// </summary>
public sealed record NotificationSettingsDto(
    bool Muted,
    bool QuietHoursEnabled,
    string? QuietStart,
    string? QuietEnd,
    string? QuietTimezone);

/// <summary>The current user's full notification-preference matrix + DND settings (FR-228).</summary>
public sealed record NotificationPreferencesDto(
    IReadOnlyList<NotificationPreferenceDto> Preferences,
    NotificationSettingsDto Settings);
