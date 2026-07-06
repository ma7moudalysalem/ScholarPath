using Microsoft.Extensions.Logging;
using ScholarPath.Application.Admin.Commands.SetUserStatus;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.UnitTests.Admin;

public class SetUserStatusCommandTests
{
    private readonly IUserAdministration _admin = Substitute.For<IUserAdministration>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ILogger<SetUserStatusCommandHandler> _log = Substitute.For<ILogger<SetUserStatusCommandHandler>>();

    public SetUserStatusCommandTests()
    {
        // Default caller: a SuperAdmin, distinct from any test target, acting on a
        // non-privileged target — so the privilege guard passes unless a test
        // overrides these.
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.IsInRole("SuperAdmin").Returns(true);
        _admin.GetRolesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<string>)Array.Empty<string>());
    }

    [Fact]
    public async Task Returns_true_and_calls_admin_when_user_found()
    {
        var uid = Guid.NewGuid();
        _admin.SetAccountStatusAsync(uid, AccountStatus.Suspended, "spam",
            Arg.Any<CancellationToken>()).Returns(true);

        var sut = new SetUserStatusCommandHandler(_admin, _currentUser, _log);
        var result = await sut.Handle(new SetUserStatusCommand(uid, AccountStatus.Suspended, "spam"), default);

        result.Should().BeTrue();
        await _admin.Received(1).SetAccountStatusAsync(uid, AccountStatus.Suspended, "spam", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_when_admin_returns_false()
    {
        var uid = Guid.NewGuid();
        _admin.SetAccountStatusAsync(uid, AccountStatus.Active, null,
            Arg.Any<CancellationToken>()).Returns(false);

        var sut = new SetUserStatusCommandHandler(_admin, _currentUser, _log);
        var act = () => sut.Handle(new SetUserStatusCommand(uid, AccountStatus.Active, null), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Plain_admin_cannot_change_a_privileged_targets_status()
    {
        var target = Guid.NewGuid();
        _currentUser.IsInRole("SuperAdmin").Returns(false);
        _admin.GetRolesAsync(target, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<string>)new[] { "Admin" });

        var sut = new SetUserStatusCommandHandler(_admin, _currentUser, _log);
        var act = () => sut.Handle(new SetUserStatusCommand(target, AccountStatus.Suspended, "x"), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        await _admin.DidNotReceive().SetAccountStatusAsync(
            Arg.Any<Guid>(), Arg.Any<AccountStatus>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Admin_cannot_change_their_own_status()
    {
        var self = Guid.NewGuid();
        _currentUser.UserId.Returns(self);

        var sut = new SetUserStatusCommandHandler(_admin, _currentUser, _log);
        var act = () => sut.Handle(new SetUserStatusCommand(self, AccountStatus.Deactivated, "x"), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task SuperAdmin_can_change_a_privileged_targets_status()
    {
        var target = Guid.NewGuid();
        _currentUser.IsInRole("SuperAdmin").Returns(true);
        _admin.GetRolesAsync(target, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<string>)new[] { "Admin" });
        _admin.SetAccountStatusAsync(target, AccountStatus.Suspended, "x", Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = new SetUserStatusCommandHandler(_admin, _currentUser, _log);
        var result = await sut.Handle(new SetUserStatusCommand(target, AccountStatus.Suspended, "x"), default);

        result.Should().BeTrue();
    }
}

public class SetUserStatusCommandValidatorTests
{
    private readonly SetUserStatusCommandValidator _v = new();

    [Fact]
    public void Reason_required_on_suspend()
    {
        var r = _v.Validate(new SetUserStatusCommand(Guid.NewGuid(), AccountStatus.Suspended, null));
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == nameof(SetUserStatusCommand.Reason));
    }

    [Fact]
    public void Reason_required_on_deactivate()
    {
        var r = _v.Validate(new SetUserStatusCommand(Guid.NewGuid(), AccountStatus.Deactivated, null));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Reason_optional_on_activate()
    {
        var r = _v.Validate(new SetUserStatusCommand(Guid.NewGuid(), AccountStatus.Active, null));
        r.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Unassigned_is_rejected()
    {
        var r = _v.Validate(new SetUserStatusCommand(Guid.NewGuid(), AccountStatus.Unassigned, null));
        r.IsValid.Should().BeFalse();
    }
}
