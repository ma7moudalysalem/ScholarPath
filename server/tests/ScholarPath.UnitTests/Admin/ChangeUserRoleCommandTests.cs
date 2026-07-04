using Microsoft.Extensions.Logging;
using ScholarPath.Application.Admin.Commands.ChangeUserRole;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.UnitTests.Admin;

public class ChangeUserRoleCommandTests
{
    private readonly IUserAdministration _admin = Substitute.For<IUserAdministration>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ILogger<ChangeUserRoleCommandHandler> _log = Substitute.For<ILogger<ChangeUserRoleCommandHandler>>();
    private readonly Guid _actorId = Guid.NewGuid();

    public ChangeUserRoleCommandTests()
    {
        _currentUser.UserId.Returns(_actorId);
        _currentUser.IsInRole("SuperAdmin").Returns(true); // default: acting as SuperAdmin
    }

    private ChangeUserRoleCommandHandler Sut() => new(_admin, _currentUser, _log);

    [Fact]
    public async Task Add_role_delegates_to_admin()
    {
        var uid = Guid.NewGuid();
        _admin.AddRoleAsync(uid, "Consultant", Arg.Any<CancellationToken>()).Returns(true);

        var result = await Sut().Handle(new ChangeUserRoleCommand(uid, "Consultant", RoleOp.Add), default);

        result.Should().BeTrue();
        await _admin.Received(1).AddRoleAsync(uid, "Consultant", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_role_delegates_to_admin()
    {
        var uid = Guid.NewGuid();
        _admin.RemoveRoleAsync(uid, "ScholarshipProvider", Arg.Any<CancellationToken>()).Returns(true);

        await Sut().Handle(new ChangeUserRoleCommand(uid, "ScholarshipProvider", RoleOp.Remove), default);

        await _admin.Received(1).RemoveRoleAsync(uid, "ScholarshipProvider", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_Conflict_when_identity_refuses()
    {
        var uid = Guid.NewGuid();
        _admin.AddRoleAsync(uid, "Admin", Arg.Any<CancellationToken>()).Returns(false);

        var act = () => Sut().Handle(new ChangeUserRoleCommand(uid, "Admin", RoleOp.Add), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    public async Task Non_superadmin_cannot_grant_privileged_role(string role)
    {
        _currentUser.IsInRole("SuperAdmin").Returns(false); // acting as plain Admin

        var act = () => Sut().Handle(new ChangeUserRoleCommand(Guid.NewGuid(), role, RoleOp.Add), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        await _admin.DidNotReceiveWithAnyArgs().AddRoleAsync(default, default!, default);
    }

    [Fact]
    public async Task Cannot_change_own_roles()
    {
        var act = () => Sut().Handle(new ChangeUserRoleCommand(_actorId, "Consultant", RoleOp.Add), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        await _admin.DidNotReceiveWithAnyArgs().AddRoleAsync(default, default!, default);
    }
}

public class ChangeUserRoleCommandValidatorTests
{
    private readonly ChangeUserRoleCommandValidator _v = new();

    [Theory]
    [InlineData("Student")]
    [InlineData("ScholarshipProvider")]
    [InlineData("Consultant")]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    [InlineData("Moderator")]
    public void Allowed_roles_pass(string role)
    {
        var r = _v.Validate(new ChangeUserRoleCommand(Guid.NewGuid(), role, RoleOp.Add));
        r.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Root")]
    [InlineData("Owner")]
    [InlineData("")]
    public void Unknown_roles_fail(string role)
    {
        var r = _v.Validate(new ChangeUserRoleCommand(Guid.NewGuid(), role, RoleOp.Add));
        r.IsValid.Should().BeFalse();
    }
}
