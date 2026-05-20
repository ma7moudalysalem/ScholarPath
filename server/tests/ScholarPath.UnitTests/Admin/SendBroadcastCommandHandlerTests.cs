using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Admin.Commands.SendBroadcast;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Admin;

/// <summary>
/// Unit tests for <see cref="SendBroadcastCommandHandler"/>.
/// Covers:  platform-wide broadcast, role-scoped broadcast, and zero-recipients short-circuit.
/// </summary>
public sealed class SendBroadcastCommandHandlerTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ApplicationUser ActiveUser(string email = "u@test.local") => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        UserName = email,
        FirstName = "Test",
        LastName = "User",
        AccountStatus = AccountStatus.Active,
    };

    private static SendBroadcastCommand MakeCommand(string? targetRole = null) => new(
        "Hello EN", "مرحبًا", "Body EN", "نص", targetRole);

    // ── helpers ─────────────────────────────────────────────────────────────

    private static SendBroadcastCommandHandler BuildHandler(
        ApplicationDbContext db,
        IAdminReadService? adminRead = null,
        INotificationDispatcher? dispatcher = null)
    {
        adminRead ??= Substitute.For<IAdminReadService>();
        dispatcher ??= Substitute.For<INotificationDispatcher>();
        return new SendBroadcastCommandHandler(
            db, adminRead, dispatcher,
            NullLogger<SendBroadcastCommandHandler>.Instance);
    }

    // ── tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Platform_wide_broadcast_dispatches_to_all_active_users()
    {
        await using var db = CreateDb();
        db.Users.AddRange(ActiveUser("a@x.com"), ActiveUser("b@x.com"));
        db.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(), Email = "s@x.com", UserName = "s@x.com",
            FirstName = "S", LastName = "U",
            AccountStatus = AccountStatus.Suspended, // must be excluded
        });
        await db.SaveChangesAsync();

        var dispatcher = Substitute.For<INotificationDispatcher>();
        var sut = BuildHandler(db, dispatcher: dispatcher);

        var result = await sut.Handle(MakeCommand(targetRole: null), CancellationToken.None);

        result.Should().Be(2);
        await dispatcher.Received(1).DispatchBroadcastAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2),
            NotificationType.Broadcast,
            Arg.Any<NotificationParams>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Role_scoped_broadcast_calls_admin_read_service_and_dispatches_only_to_matching_users()
    {
        await using var db = CreateDb();
        var targetId = Guid.NewGuid();

        var adminRead = Substitute.For<IAdminReadService>();
        adminRead.GetActiveUserIdsByRoleAsync("Student", Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { targetId });

        var dispatcher = Substitute.For<INotificationDispatcher>();
        var sut = BuildHandler(db, adminRead, dispatcher);

        var result = await sut.Handle(MakeCommand(targetRole: "Student"), CancellationToken.None);

        result.Should().Be(1);
        await adminRead.Received(1).GetActiveUserIdsByRoleAsync("Student", Arg.Any<CancellationToken>());
        await dispatcher.Received(1).DispatchBroadcastAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(targetId)),
            NotificationType.Broadcast,
            Arg.Any<NotificationParams>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Zero_recipients_returns_zero_and_skips_dispatch()
    {
        await using var db = CreateDb(); // empty DB — no users

        var dispatcher = Substitute.For<INotificationDispatcher>();
        var sut = BuildHandler(db, dispatcher: dispatcher);

        var result = await sut.Handle(MakeCommand(), CancellationToken.None);

        result.Should().Be(0);
        await dispatcher.DidNotReceiveWithAnyArgs().DispatchBroadcastAsync(
            default!, default, default!, default);
    }

    [Fact]
    public async Task Role_scoped_broadcast_with_zero_matching_role_members_skips_dispatch()
    {
        await using var db = CreateDb();

        var adminRead = Substitute.For<IAdminReadService>();
        adminRead.GetActiveUserIdsByRoleAsync("Admin", Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        var dispatcher = Substitute.For<INotificationDispatcher>();
        var sut = BuildHandler(db, adminRead, dispatcher);

        var result = await sut.Handle(MakeCommand(targetRole: "Admin"), CancellationToken.None);

        result.Should().Be(0);
        await dispatcher.DidNotReceiveWithAnyArgs().DispatchBroadcastAsync(
            default!, default, default!, default);
    }
}
