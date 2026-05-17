using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Hubs;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using Xunit;

namespace ScholarPath.UnitTests.Notifications;

public class NotificationDispatcherTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static NotificationDispatcher Sut(ApplicationDbContext db) =>
        new(db, new NotificationCatalog(),
            Substitute.For<IHubContext<NotificationHub>>(),
            Substitute.For<IEmailService>(),
            NullLogger<NotificationDispatcher>.Instance);

    [Fact]
    public async Task Dispatch_persists_inapp_and_email_notification_rows()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();

        await Sut(db).DispatchAsync(userId, NotificationType.ResourceApproved,
            new NotificationParams { TitleEn = "Guide", TitleAr = "دليل" },
            deepLink: null, idempotencyKey: null, default);

        var rows = await db.Notifications.Where(n => n.RecipientUserId == userId).ToListAsync();
        rows.Should().HaveCount(2);
        rows.Should().Contain(n => n.Channel == NotificationChannel.InApp);
        rows.Should().Contain(n => n.Channel == NotificationChannel.Email);
        rows.Should().OnlyContain(n => n.BodyEn.Contains("Guide"));
    }

    [Fact]
    public async Task Dispatch_is_idempotent_on_the_idempotency_key()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        const string key = "evt-123";

        await Sut(db).DispatchAsync(userId, NotificationType.ResourceApproved,
            NotificationParams.Empty, null, key, default);
        await Sut(db).DispatchAsync(userId, NotificationType.ResourceApproved,
            NotificationParams.Empty, null, key, default);

        // First run -> InApp + Email rows; the replay with the same key is skipped.
        (await db.Notifications.CountAsync(n => n.RecipientUserId == userId)).Should().Be(2);
    }

    [Fact]
    public async Task Disabled_email_preference_suppresses_the_email_row()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.NotificationPreferences.Add(new NotificationPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = NotificationType.ResourceApproved,
            Channel = NotificationChannel.Email,
            IsEnabled = false,
        });
        await db.SaveChangesAsync();

        await Sut(db).DispatchAsync(userId, NotificationType.ResourceApproved,
            NotificationParams.Empty, null, null, default);

        var rows = await db.Notifications.Where(n => n.RecipientUserId == userId).ToListAsync();
        rows.Should().ContainSingle().Which.Channel.Should().Be(NotificationChannel.InApp);
    }
}
