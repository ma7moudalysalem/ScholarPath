using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using Xunit;
using FluentAssertions;

namespace ScholarPath.UnitTests.Ai;

/// <summary>
/// SRS FR-117 — the eligibility checker must return an overall
/// Eligible / PartiallyEligible / NotEligible verdict derived from the
/// per-criterion verdicts. Exercises <see cref="LocalAiService"/> end-to-end.
/// </summary>
public sealed class EligibilityVerdictTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly LocalAiService _ai;

    public EligibilityVerdictTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _ai = new LocalAiService(_db);
    }

    private async Task<Guid> SeedScholarshipAsync(
        AcademicLevel level, string? countriesJson, string? tagsJson)
    {
        var scholarship = new Scholarship
        {
            Id = Guid.NewGuid(),
            OwnerCompanyId = Guid.NewGuid(),
            TitleEn = "Test Scholarship",
            TitleAr = "منحة اختبار",
            DescriptionEn = "Description",
            DescriptionAr = "وصف",
            Slug = $"sch-{Guid.NewGuid():N}",
            CategoryId = Guid.NewGuid(),
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
            TargetLevel = level,
            TargetCountriesJson = countriesJson,
            TagsJson = tagsJson,
        };
        _db.Scholarships.Add(scholarship);
        await _db.SaveChangesAsync();
        return scholarship.Id;
    }

    private async Task SeedProfileAsync(
        Guid userId, AcademicLevel? level, string? nationality, string? fieldOfStudy)
    {
        _db.UserProfiles.Add(new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AcademicLevel = level,
            Nationality = nationality,
            FieldOfStudy = fieldOfStudy,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task AllCriteriaMet_ReturnsEligible()
    {
        var userId = Guid.NewGuid();
        var scholarshipId = await SeedScholarshipAsync(
            AcademicLevel.Masters, "[\"Egypt\"]", "[\"Engineering\"]");
        await SeedProfileAsync(userId, AcademicLevel.Masters, "Egypt", "Engineering");

        var result = await _ai.CheckEligibilityAsync(userId, scholarshipId, CancellationToken.None);

        result.Verdict.Should().Be(EligibilityVerdict.Eligible);
        result.Criteria.Should().OnlyContain(c => c.Match == "yes");
    }

    [Fact]
    public async Task FailingCriterion_ReturnsNotEligible()
    {
        var userId = Guid.NewGuid();
        // Listing requires Masters; the student is an Undergrad — an outright "no".
        var scholarshipId = await SeedScholarshipAsync(
            AcademicLevel.Masters, "[\"Egypt\"]", "[\"Engineering\"]");
        await SeedProfileAsync(userId, AcademicLevel.Undergrad, "Egypt", "Engineering");

        var result = await _ai.CheckEligibilityAsync(userId, scholarshipId, CancellationToken.None);

        result.Verdict.Should().Be(EligibilityVerdict.NotEligible);
    }

    [Fact]
    public async Task UnknownCriterion_NoFailures_ReturnsPartiallyEligible()
    {
        var userId = Guid.NewGuid();
        var scholarshipId = await SeedScholarshipAsync(
            AcademicLevel.Masters, "[\"Egypt\"]", "[\"Engineering\"]");
        // Academic level matches; nationality + field are unknown (null) — no
        // outright failure, so the overall verdict is PartiallyEligible.
        await SeedProfileAsync(userId, AcademicLevel.Masters, nationality: null, fieldOfStudy: null);

        var result = await _ai.CheckEligibilityAsync(userId, scholarshipId, CancellationToken.None);

        result.Verdict.Should().Be(EligibilityVerdict.PartiallyEligible);
    }

    [Fact]
    public async Task ScholarshipNotFound_ReturnsNotEligible()
    {
        var result = await _ai.CheckEligibilityAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Verdict.Should().Be(EligibilityVerdict.NotEligible);
        result.Criteria.Should().BeEmpty();
    }

    public void Dispose() => _db.Dispose();
}
