using System.Text;
using NSubstitute;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Admin.Queries.ExportUsersCsv;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using Xunit;
using FluentAssertions;

namespace ScholarPath.UnitTests.Admin;

/// <summary>SRS FR-162 — admin CSV export of the user list.</summary>
public sealed class ExportUsersCsvQueryHandlerTests
{
    private readonly IAdminReadService _admin = Substitute.For<IAdminReadService>();
    private readonly ExportUsersCsvQueryHandler _handler;

    public ExportUsersCsvQueryHandlerTests()
    {
        _handler = new ExportUsersCsvQueryHandler(_admin);
    }

    private static AdminUserRow Row(string email, string fullName, params string[] roles) =>
        new(Guid.NewGuid(), email, fullName, AccountStatus.Active, true,
            roles, DateTimeOffset.UtcNow, null, false, null);

    private static string Decode(byte[] content)
    {
        // Strip the UTF-8 BOM the handler prepends.
        var preamble = Encoding.UTF8.GetPreamble();
        var hasBom = content.Length >= preamble.Length
            && content.Take(preamble.Length).SequenceEqual(preamble);
        return Encoding.UTF8.GetString(content, hasBom ? preamble.Length : 0,
            content.Length - (hasBom ? preamble.Length : 0));
    }

    [Fact]
    public async Task Handle_WritesHeaderAndRows()
    {
        _admin.SearchUsersAsync(null, null, null, false, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AdminUserRow>(
                new[] { Row("a@test.com", "Alice Ahmed", "Student") }, 1, 100, 1));

        var result = await _handler.Handle(
            new ExportUsersCsvQuery(null, null, null, false), CancellationToken.None);

        result.FileName.Should().EndWith(".csv");
        var text = Decode(result.Content);
        text.Should().StartWith("Id,Email,FullName,AccountStatus,IsOnboardingComplete,Roles,CreatedAt,LastLoginAt,IsAtRisk,RiskScore");
        text.Should().Contain("a@test.com");
        text.Should().Contain("Alice Ahmed");
    }

    [Fact]
    public async Task Handle_EscapesFieldsContainingCommas()
    {
        _admin.SearchUsersAsync(null, null, null, false, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AdminUserRow>(
                new[] { Row("b@test.com", "Doe, John", "Admin", "Consultant") }, 1, 100, 1));

        var result = await _handler.Handle(
            new ExportUsersCsvQuery(null, null, null, false), CancellationToken.None);

        var text = Decode(result.Content);
        // The name has a comma so it must be wrapped in quotes.
        text.Should().Contain("\"Doe, John\"");
        // Multiple roles are joined with "; " so the cell needs no quoting.
        text.Should().Contain("Admin; Consultant");
    }

    [Fact]
    public async Task Handle_EmptyResult_WritesHeaderOnly()
    {
        _admin.SearchUsersAsync(null, null, null, false, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AdminUserRow>(Array.Empty<AdminUserRow>(), 1, 100, 0));

        var result = await _handler.Handle(
            new ExportUsersCsvQuery(null, null, null, false), CancellationToken.None);

        var text = Decode(result.Content);
        text.TrimEnd().Should().Be(
            "Id,Email,FullName,AccountStatus,IsOnboardingComplete,Roles,CreatedAt,LastLoginAt,IsAtRisk,RiskScore");
    }

    [Fact]
    public async Task Handle_PagesUntilExhausted()
    {
        // First page is full (100), second page returns the remainder.
        var firstPage = Enumerable.Range(0, 100)
            .Select(i => Row($"u{i}@test.com", $"User {i}")).ToArray();
        var secondPage = new[] { Row("last@test.com", "Last User") };

        _admin.SearchUsersAsync(null, null, null, false, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AdminUserRow>(firstPage, 1, 100, 101));
        _admin.SearchUsersAsync(null, null, null, false, 2, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AdminUserRow>(secondPage, 2, 100, 101));

        var result = await _handler.Handle(
            new ExportUsersCsvQuery(null, null, null, false), CancellationToken.None);

        var text = Decode(result.Content);
        text.Should().Contain("last@test.com");
        await _admin.Received(1).SearchUsersAsync(
            null, null, null, false, 2, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
