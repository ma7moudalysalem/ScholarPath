using Microsoft.Extensions.Logging;
using ScholarPath.Application.Admin.Commands.ChangeUserRole;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.UnitTests.Admin;

public class ChangeUserRoleCommandTests
{
    private readonly IUserAdministration _admin = Substitute.For<IUserAdministration>();
    private readonly ILogger<ChangeUserRoleCommandHandler> _log = Substitute.For<ILogger<ChangeUserRoleCommandHandler>>();

    [Fact]
    public async Task Add_role_delegates_to_admin()
    {
        var uid = Guid.NewGuid();
        _admin.AddRoleAsync(uid, "Consultant", Arg.Any<CancellationToken>()).Returns(true);

        var sut = new ChangeUserRoleCommandHandler(_admin, _log);
        var result = await sut.Handle(new ChangeUserRoleCommand(uid, "Consultant", RoleOp.Add), default);

        result.Should().BeTrue();
        await _admin.Received(1).AddRoleAsync(uid, "Consultant", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_role_delegates_to_admin()
    {
        var uid = Guid.NewGuid();
        _admin.RemoveRoleAsync(uid, "Company", Arg.Any<CancellationToken>()).Returns(true);

        var sut = new ChangeUserRoleCommandHandler(_admin, _log);
        await sut.Handle(new ChangeUserRoleCommand(uid, "Company", RoleOp.Remove), default);

        await _admin.Received(1).RemoveRoleAsync(uid, "Company", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_Conflict_when_identity_refuses()
    {
        var uid = Guid.NewGuid();
        _admin.AddRoleAsync(uid, "Admin", Arg.Any<CancellationToken>()).Returns(false);

        var sut = new ChangeUserRoleCommandHandler(_admin, _log);
        var act = () => sut.Handle(new ChangeUserRoleCommand(uid, "Admin", RoleOp.Add), default);

        await act.Should().ThrowAsync<ConflictException>();
    }
}

public class ChangeUserRoleCommandValidatorTests
{
    private readonly ChangeUserRoleCommandValidator _v = new();

    [Theory]
    [InlineData("Student")]
    [InlineData("Company")]
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
