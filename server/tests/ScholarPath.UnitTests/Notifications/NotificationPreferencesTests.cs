using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications.Commands.UpdateNotificationPreference;
using ScholarPath.Application.Notifications.Queries.GetNotificationPreferences;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.UnitTests.Notifications;

/// <summary>
/// FR-228 — reading and updating a user's notification-delivery preferences.
/// Uses InMemory EF so the real query/upsert path is exercised.
/// </summary>
public class NotificationPreferencesTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static ICurrentUserService UserContext(Guid userId)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        return currentUser;
    }

    // SystemTest is a user-fired diagnostic, excluded from the preference matrix.
    private static readonly int ExpectedRowCount =
        Enum.GetValues<NotificationType>().Count(t => t != NotificationType.SystemTest)
        * Enum.GetValues<NotificationChannel>().Length;

    // ─── GetNotificationPreferencesQuery ─────────────────────────────────────

    [Fact]
    public async Task Get_returns_full_matrix_all_enabled_when_no_rows_stored()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        var handler = new GetNotificationPreferencesQueryHandler((IApplicationDbContext)db, UserContext(userId));

        var result = await handler.Handle(new GetNotificationPreferencesQuery(), CancellationToken.None);

        result.Preferences.Should().HaveCount(ExpectedRowCount);
        result.Preferences.Should().OnlyContain(p => p.IsEnabled);
    }

    [Fact]
    public async Task Get_reflects_a_stored_disabled_preference()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.NotificationPreferences.Add(new NotificationPreference
        {
            UserId = userId,
            Type = NotificationType.ApplicationDeadlineApproaching,
            Channel = NotificationChannel.Email,
            IsEnabled = false,
        });
        await db.SaveChangesAsync();

        var handler = new GetNotificationPreferencesQueryHandler((IApplicationDbContext)db, UserContext(userId));
        var result = await handler.Handle(new GetNotificationPreferencesQuery(), CancellationToken.None);

        var disabled = result.Preferences.Single(p =>
            p.Type == nameof(NotificationType.ApplicationDeadlineApproaching)
            && p.Channel == nameof(NotificationChannel.Email));
        disabled.IsEnabled.Should().BeFalse();

        // Everything else stays enabled.
        result.Preferences.Count(p => !p.IsEnabled).Should().Be(1);
    }

    [Fact]
    public async Task Get_does_not_leak_another_users_preferences()
    {
        using var db = CreateDb();
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();
        db.NotificationPreferences.Add(new NotificationPreference
        {
            UserId = other,
            Type = NotificationType.PaymentSuccess,
            Channel = NotificationChannel.Email,
            IsEnabled = false,
        });
        await db.SaveChangesAsync();

        var handler = new GetNotificationPreferencesQueryHandler((IApplicationDbContext)db, UserContext(me));
        var result = await handler.Handle(new GetNotificationPreferencesQuery(), CancellationToken.None);

        // The other user's disabled row must not affect my matrix.
        result.Preferences.Should().OnlyContain(p => p.IsEnabled);
    }

    [Fact]
    public async Task Get_throws_when_not_authenticated()
    {
        using var db = CreateDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns((Guid?)null);
        var handler = new GetNotificationPreferencesQueryHandler((IApplicationDbContext)db, currentUser);

        var act = () => handler.Handle(new GetNotificationPreferencesQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    // ─── UpdateNotificationPreferenceCommand ─────────────────────────────────

    [Fact]
    public async Task Update_disabling_a_channel_inserts_a_disabled_row()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        var handler = new UpdateNotificationPreferenceCommandHandler((IApplicationDbContext)db, UserContext(userId));

        await handler.Handle(
            new UpdateNotificationPreferenceCommand(
                NotificationType.ApplicationDeadlineApproaching, NotificationChannel.Email, IsEnabled: false),
            CancellationToken.None);

        var row = await db.NotificationPreferences.SingleAsync();
        row.UserId.Should().Be(userId);
        row.Type.Should().Be(NotificationType.ApplicationDeadlineApproaching);
        row.Channel.Should().Be(NotificationChannel.Email);
        row.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Update_enabling_with_no_stored_row_is_a_no_op()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        var handler = new UpdateNotificationPreferenceCommandHandler((IApplicationDbContext)db, UserContext(userId));

        // Enabled is the default — no row needed to represent it.
        await handler.Handle(
            new UpdateNotificationPreferenceCommand(
                NotificationType.PaymentSuccess, NotificationChannel.Email, IsEnabled: true),
            CancellationToken.None);

        (await db.NotificationPreferences.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Update_re_enabling_a_disabled_channel_flips_the_existing_row()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.NotificationPreferences.Add(new NotificationPreference
        {
            UserId = userId,
            Type = NotificationType.ApplicationDraftReminder,
            Channel = NotificationChannel.Email,
            IsEnabled = false,
        });
        await db.SaveChangesAsync();

        var handler = new UpdateNotificationPreferenceCommandHandler((IApplicationDbContext)db, UserContext(userId));
        await handler.Handle(
            new UpdateNotificationPreferenceCommand(
                NotificationType.ApplicationDraftReminder, NotificationChannel.Email, IsEnabled: true),
            CancellationToken.None);

        var row = await db.NotificationPreferences.SingleAsync();
        row.IsEnabled.Should().BeTrue();
        (await db.NotificationPreferences.CountAsync()).Should().Be(1); // updated, not duplicated
    }

    [Fact]
    public async Task Update_throws_when_not_authenticated()
    {
        using var db = CreateDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns((Guid?)null);
        var handler = new UpdateNotificationPreferenceCommandHandler((IApplicationDbContext)db, currentUser);

        var act = () => handler.Handle(
            new UpdateNotificationPreferenceCommand(
                NotificationType.PaymentSuccess, NotificationChannel.Email, IsEnabled: false),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}

public class UpdateNotificationPreferenceValidatorTests
{
    private readonly UpdateNotificationPreferenceCommandValidator _v = new();

    [Fact]
    public void Valid_type_and_channel_passes()
    {
        var r = _v.Validate(new UpdateNotificationPreferenceCommand(
            NotificationType.ApplicationDeadlineApproaching, NotificationChannel.InApp, IsEnabled: false));
        r.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Undefined_type_fails()
    {
        var r = _v.Validate(new UpdateNotificationPreferenceCommand(
            (NotificationType)99999, NotificationChannel.InApp, IsEnabled: false));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Undefined_channel_fails()
    {
        var r = _v.Validate(new UpdateNotificationPreferenceCommand(
            NotificationType.PaymentSuccess, (NotificationChannel)99999, IsEnabled: false));
        r.IsValid.Should().BeFalse();
    }
}
