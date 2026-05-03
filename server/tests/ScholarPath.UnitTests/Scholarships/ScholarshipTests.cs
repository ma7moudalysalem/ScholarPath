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

namespace ScholarPath.UnitTests.Scholarships;

public sealed class ScholarshipTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;
    private readonly CreateScholarshipCommandValidator _createValidator;

    public ScholarshipTests()
    {
        // Blocker T-011: إعداد Sqlite In-Memory لضمان اختبار الـ Constraints بشكل حقيقي
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated(); // بناء الجداول فعلياً

        _createValidator = new CreateScholarshipCommandValidator();
    }

    // --- 1. Validation Tests (تغطية شرط الـ 7 أيام) ---

    [Fact]
    public void CreateValidator_ShouldFail_WhenDeadlineIsLessThan7Days()
    {
        // Arrange
        var command = new CreateScholarshipCommand
        {
            Deadline = DateTimeOffset.UtcNow.AddDays(5) // أقل من 7 أيام
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

    // --- 2. Query Handler Tests (باستخدام Sqlite الحقيقي) ---

    [Fact]
    public async Task GetById_ShouldThrowNotFoundException_WhenRecordDoesNotExist()
    {
        // Arrange
        var handler = new GetScholarshipByIdQueryHandler(_context);
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

        var handler = new GetScholarshipByIdQueryHandler(_context);
        var query = new GetScholarshipByIdQuery(id, "en");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(id);
        result.Title.Should().Be("Test Scholarship");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
