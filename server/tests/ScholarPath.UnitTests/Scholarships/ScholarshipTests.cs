using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Scholarships.Commands;
using ScholarPath.Application.Scholarships.Queries;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.UnitTests.Scholarships;

public sealed class ScholarshipTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;
    private readonly CreateScholarshipCommandValidator _createValidator;

    public ScholarshipTests()
    {
     
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated(); 

        _createValidator = new CreateScholarshipCommandValidator();
    }

    //  Validation Tests 

    [Fact]
    public void CreateValidator_ShouldFail_WhenDeadlineIsLessThan7Days()
    {
        // Arrange
        var command = new CreateScholarshipCommand
        {
            Deadline = DateTimeOffset.UtcNow.AddDays(5) // less than 7 days
        };

        // Act
        var result = _createValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Deadline)
              .WithErrorMessage("Deadline must be at least 7 days from now.");
    }

    [Fact]
    public void CreateValidator_ShouldPass_WhenDeadlineIsMoreThan7Days()
    {
        // Arrange
        var command = new CreateScholarshipCommand
        {
            Deadline = DateTimeOffset.UtcNow.AddDays(10),
            TitleEn = "International Tech Scholarship"
        };

        // Act
        var result = _createValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Deadline);
    }

    //  Query Handler Tests

    [Fact]
    public async Task GetById_ShouldThrowNotFoundException_WhenRecordDoesNotExist()
    {
        // Arrange
        var handler = new GetScholarshipByIdQueryHandler(_context, Substitute.For<ICurrentUserService>());
        var query = new GetScholarshipByIdQuery(Guid.NewGuid());

        // Act
        var act = () => handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetById_ShouldReturnScholarship_WhenItExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        _context.Scholarships.Add(new Scholarship
        {
            Id = id,
            TitleEn = "Test Scholarship",
            TitleAr = "منحة تجريبية",
            Slug="test-scholarship-unique-siug",
            DescriptionEn = "Desc",
            DescriptionAr = "وصف",
            Deadline = DateTimeOffset.UtcNow.AddDays(10),
            Status = ScholarshipStatus.Open
        });
        await _context.SaveChangesAsync();

        var handler = new GetScholarshipByIdQueryHandler(_context, Substitute.For<ICurrentUserService>());
        var query = new GetScholarshipByIdQuery(id, "en");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(id);
        result.Title.Should().Be("Test Scholarship");
    }

    [Fact]
    public async Task GetById_ShouldThrowNotFound_ForUnderReviewListing_WhenViewerIsNotOwnerOrAdmin()
    {
        // Arrange — a not-yet-public listing owned by someone else.
        var id = Guid.NewGuid();
        _context.Scholarships.Add(new Scholarship
        {
            Id = id,
            TitleEn = "Pending Scholarship",
            TitleAr = "منحة قيد المراجعة",
            Slug = "pending-scholarship-unique",
            DescriptionEn = "Desc",
            DescriptionAr = "وصف",
            Deadline = DateTimeOffset.UtcNow.AddDays(10),
            Status = ScholarshipStatus.UnderReview,
        });
        await _context.SaveChangesAsync();

        var stranger = Substitute.For<ICurrentUserService>();
        stranger.UserId.Returns(Guid.NewGuid());
        stranger.IsAdminOrSuperAdmin().Returns(false);

        var handler = new GetScholarshipByIdQueryHandler(_context, stranger);

        // Act / Assert — the IDOR gate hides the listing behind a NotFound.
        var act = () => handler.Handle(new GetScholarshipByIdQuery(id, "en"), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetById_ShouldReturnUnderReviewListing_ForAdmin()
    {
        // Arrange — the admin moderation preview must be able to read it.
        var id = Guid.NewGuid();
        _context.Scholarships.Add(new Scholarship
        {
            Id = id,
            TitleEn = "Pending Scholarship",
            TitleAr = "منحة قيد المراجعة",
            Slug = "pending-scholarship-admin-unique",
            DescriptionEn = "Desc",
            DescriptionAr = "وصف",
            Deadline = DateTimeOffset.UtcNow.AddDays(10),
            Status = ScholarshipStatus.UnderReview,
        });
        await _context.SaveChangesAsync();

        var admin = Substitute.For<ICurrentUserService>();
        admin.UserId.Returns(Guid.NewGuid());
        admin.IsAdminOrSuperAdmin().Returns(true);

        var handler = new GetScholarshipByIdQueryHandler(_context, admin);

        var result = await handler.Handle(new GetScholarshipByIdQuery(id, "en"), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(id);
    }

    // FR-SCH-05: the field-of-study filter matches a whole JSON-array element
    // (not a raw substring). This mirrors the handler's exact logic —
    // storedJson.Contains(JsonSerializer.Serialize(field)) — where BOTH the stored
    // array and the needle go through the same default serializer, so the quoting
    // and the '&'→& escaping line up. (A DB-backed test can't run here: the
    // query's DateTimeOffset ORDER BY is unsupported by the SQLite test harness,
    // though SQL Server handles it in prod.)
    [Fact]
    public void FieldFilter_SerializedNeedle_MatchesStoredElement_IncludingAmpersand()
    {
        var stored = JsonSerializer.Serialize(new[] { "Arts & Humanities", "Engineering" });

        // Exact-element matches, ampersand handled by identical encoding.
        stored.Contains(JsonSerializer.Serialize("Arts & Humanities")).Should().BeTrue();
        stored.Contains(JsonSerializer.Serialize("Engineering")).Should().BeTrue();

        // Partial-word needle must NOT match (the bug this fix closes).
        JsonSerializer.Serialize(new[] { "Smart Materials" })
            .Contains(JsonSerializer.Serialize("Art")).Should().BeFalse();

        // A RAW (unserialized) ampersand needle would NOT match — proving why the
        // handler must serialize the needle rather than build "\"{field}\"".
        stored.Contains("\"Arts & Humanities\"").Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
