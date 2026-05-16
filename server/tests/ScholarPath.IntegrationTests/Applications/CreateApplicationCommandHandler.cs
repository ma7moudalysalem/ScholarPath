using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.IntegrationTests.Applications;

public class CreateApplicationTests : IClassFixture<ScholarshipApplicationsFactory>
{
    private readonly ScholarshipApplicationsFactory _factory;
    private readonly HttpClient _client;

    public CreateApplicationTests(ScholarshipApplicationsFactory factory)
    {
        _factory = factory;
        _client = factory.CreateStudentClient();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds an open scholarship directly into the test DB and returns its ID.
    /// </summary>
    private async Task<Guid> SeedOpenScholarshipAsync(string slug)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var scholarship = new Scholarship
        {
            Id = Guid.NewGuid(),
            TitleEn = "Test Scholarship",
            TitleAr = "منحة تجريبية",
            DescriptionEn = "Test scholarship description",
            DescriptionAr = "وصف المنحة التجريبية",
            Status = ScholarshipStatus.Open,
            Mode = ListingMode.InApp,
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
            Slug = slug
        };

        db.Scholarships.Add(scholarship);
        await db.SaveChangesAsync();

        return scholarship.Id;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Start_ValidScholarship_Returns201AndCreatesApplication()
    {
        // Arrange
        var scholarshipId = await SeedOpenScholarshipAsync("valid-scholarship");

        var payload = new
        {
            scholarshipId,
            personalNotes = "My notes"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/applications", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var applicationId = await response.Content.ReadFromJsonAsync<Guid>();
        applicationId.Should().NotBeEmpty();

        // Verify the application exists in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var application = db.Applications.FirstOrDefault(a => a.Id == applicationId);

        application.Should().NotBeNull();
        application!.ScholarshipId.Should().Be(scholarshipId);
        application.Status.Should().Be(ApplicationStatus.Draft);
        application.Mode.Should().Be(ApplicationMode.InApp);
    }

    [Fact]
    public async Task Start_DuplicateActiveApplication_Returns409Conflict()
    {
        // Arrange
        var scholarshipId = await SeedOpenScholarshipAsync("duplicate-scholarship");

        var payload = new
        {
            scholarshipId,
            personalNotes = "First attempt"
        };

        // Act — first request should succeed
        var firstResponse = await _client.PostAsJsonAsync("/api/applications", payload);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act — second request should conflict
        var secondResponse = await _client.PostAsJsonAsync("/api/applications", payload);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Start_ClosedScholarship_Returns409Conflict()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var scholarship = new Scholarship
        {
            Id = Guid.NewGuid(),
            TitleEn = "Closed Scholarship",
            TitleAr = "منحة مغلقة",
            DescriptionEn = "Closed scholarship description",
            DescriptionAr = "وصف المنحة المغلقة",
            Status = ScholarshipStatus.Closed,
            Mode = ListingMode.InApp,
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
            Slug = "closed-scholarship"
        };

        db.Scholarships.Add(scholarship);
        await db.SaveChangesAsync();

        var payload = new { scholarshipId = scholarship.Id, personalNotes = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/applications", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Start_ExternalScholarship_Returns409Conflict()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var scholarship = new Scholarship
        {
            Id = Guid.NewGuid(),
            TitleEn = "External Scholarship",
            TitleAr = "منحة خارجية",
            DescriptionEn = "External scholarship description",
            DescriptionAr = "وصف المنحة الخارجية",
            Status = ScholarshipStatus.Open,
            Mode = ListingMode.ExternalUrl,
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
            Slug = "external-scholarship"
        };

        db.Scholarships.Add(scholarship);
        await db.SaveChangesAsync();

        var payload = new { scholarshipId = scholarship.Id, personalNotes = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/applications", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Start_EmptyScholarshipId_Returns400BadRequest()
    {
        // Arrange
        var payload = new { scholarshipId = Guid.Empty, personalNotes = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/applications", payload);

        // Assert — empty id fails FluentValidation, surfaced as 422.
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
