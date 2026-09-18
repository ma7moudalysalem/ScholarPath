using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using ScholarPath.Infrastructure.Settings;
using Xunit;
using FluentAssertions;

namespace ScholarPath.UnitTests.Ai;

/// <summary>
/// The match score is the share of the attainable weight a listing earns across the
/// criteria it actually constrains. These tests pin that contract: a criterion the
/// listing leaves open must not count against it, a wording difference must not cost
/// a student everything, and the explanation must name what actually matched.
/// </summary>
public sealed class RecommendationScoringTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly LocalAiService _ai;
    private readonly Guid _userId = Guid.NewGuid();

    public RecommendationScoringTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _ai = new LocalAiService(_db, new NoopRetriever(), Options.Create(new AiOptions()));
    }

    private sealed class NoopRetriever : IKnowledgeRetriever
    {
        public Task<IReadOnlyList<RetrievedDocument>> RetrieveAsync(
            string query, int topK, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<RetrievedDocument>>([]);
    }

    private async Task<Guid> SeedListingAsync(
        AcademicLevel level,
        string? fieldsJson = null,
        string? countriesJson = null,
        decimal funding = 0m,
        string title = "Test Scholarship")
    {
        var s = new Scholarship
        {
            Id = Guid.NewGuid(),
            OwnerScholarshipProviderId = Guid.NewGuid(),
            TitleEn = title,
            TitleAr = "منحة اختبار",
            DescriptionEn = "Description",
            DescriptionAr = "وصف",
            Slug = $"sch-{Guid.NewGuid():N}",
            CategoryId = Guid.NewGuid(),
            Status = ScholarshipStatus.Open,
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
            TargetLevel = level,
            FieldsOfStudyJson = fieldsJson,
            TargetCountriesJson = countriesJson,
            FundingAmountUsd = funding,
        };
        _db.Scholarships.Add(s);
        await _db.SaveChangesAsync();
        return s.Id;
    }

    private async Task SeedProfileAsync(
        AcademicLevel? level, string? preferredFieldsJson = null, string? preferredCountriesJson = null)
    {
        _db.UserProfiles.Add(new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            AcademicLevel = level,
            PreferredFieldsJson = preferredFieldsJson,
            PreferredCountriesJson = preferredCountriesJson,
        });
        await _db.SaveChangesAsync();
    }

    private async Task<int> ScoreOfSingleListingAsync()
    {
        var result = await _ai.GenerateRecommendationsAsync(_userId, 5, CancellationToken.None);
        result.Items.Should().HaveCount(1);
        return result.Items[0].MatchScore;
    }

    [Fact]
    public async Task Listing_open_to_every_discipline_is_not_penalised_for_that_openness()
    {
        // The listing constrains only the level, and the student meets it. The field
        // preference has nothing to be compared against, so it drops out of the ratio
        // instead of scoring zero against the student.
        await SeedProfileAsync(AcademicLevel.Masters, """["Computer Science"]""");
        await SeedListingAsync(AcademicLevel.Masters, fieldsJson: "[]");

        (await ScoreOfSingleListingAsync()).Should().Be(100);
    }

    [Fact]
    public async Task Listing_meeting_every_stated_preference_scores_full()
    {
        await SeedProfileAsync(AcademicLevel.Masters, """["Computer Science"]""", """["EG"]""");
        await SeedListingAsync(AcademicLevel.Masters,
            fieldsJson: """["Computer Science"]""", countriesJson: """["EG"]""");

        (await ScoreOfSingleListingAsync()).Should().Be(100);
    }

    [Fact]
    public async Task Wording_difference_earns_partial_credit_rather_than_nothing()
    {
        // "Software Engineering" shares a significant word with "Engineering", so the
        // field criterion is half met rather than missed outright.
        await SeedProfileAsync(AcademicLevel.Masters, """["Software Engineering"]""");
        await SeedListingAsync(AcademicLevel.Masters, fieldsJson: """["Engineering"]""");

        // level 30/30 + field 20/40 = 50 of an attainable 70.
        (await ScoreOfSingleListingAsync()).Should().Be(71);
    }

    [Fact]
    public async Task Unrelated_field_earns_no_credit()
    {
        await SeedProfileAsync(AcademicLevel.Masters, """["Computer Science"]""");
        await SeedListingAsync(AcademicLevel.Masters, fieldsJson: """["Law"]""");

        // level 30/30 + field 0/40 = 30 of an attainable 70.
        (await ScoreOfSingleListingAsync()).Should().Be(43);
    }

    [Fact]
    public async Task Profile_stating_nothing_comparable_scores_zero()
    {
        // Nothing is known about the student, so the system reports no fit rather
        // than inventing one.
        await SeedProfileAsync(level: null);
        await SeedListingAsync(AcademicLevel.Masters, fieldsJson: """["Law"]""", funding: 50_000m);

        (await ScoreOfSingleListingAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Funding_bonus_never_substitutes_for_fit()
    {
        // A large award nudges a listing that already fits; it cannot lift one that
        // does not fit at all.
        await SeedProfileAsync(AcademicLevel.PhD, """["Computer Science"]""");
        await SeedListingAsync(AcademicLevel.HighSchool, fieldsJson: """["Law"]""", funding: 100_000m);

        (await ScoreOfSingleListingAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Funding_bonus_is_added_on_top_of_a_listing_that_fits()
    {
        await SeedProfileAsync(AcademicLevel.Masters, """["Computer Science"]""");
        await SeedListingAsync(AcademicLevel.Masters,
            fieldsJson: """["Computer Science"]""", funding: 25_000m);

        // Full fit is already 100; the bonus cannot push the score past the ceiling.
        (await ScoreOfSingleListingAsync()).Should().Be(100);
    }

    [Fact]
    public async Task Explanation_names_the_criteria_that_matched()
    {
        await SeedProfileAsync(AcademicLevel.Masters, """["Computer Science"]""", """["EG"]""");
        await SeedListingAsync(AcademicLevel.Masters,
            fieldsJson: """["Computer Science"]""", countriesJson: """["EG"]""", title: "Nile Grant");

        var result = await _ai.GenerateRecommendationsAsync(_userId, 5, CancellationToken.None);
        var item = result.Items.Single();

        item.ExplanationEn.Should().Contain("your academic level");
        item.ExplanationEn.Should().Contain("preferred field");
        item.ExplanationEn.Should().Contain("preferred countr");
        item.ExplanationEn.Should().Contain("Nile Grant");
        item.ExplanationAr.Should().Contain("مستواك الأكاديمي");
    }

    [Fact]
    public async Task Explanation_says_so_plainly_when_nothing_matched()
    {
        await SeedProfileAsync(AcademicLevel.PhD, """["Computer Science"]""");
        await SeedListingAsync(AcademicLevel.HighSchool, fieldsJson: """["Law"]""");

        var result = await _ai.GenerateRecommendationsAsync(_userId, 5, CancellationToken.None);

        result.Items.Single().ExplanationEn.Should().Contain("does not match");
    }

    [Fact]
    public async Task Better_fitting_listing_outranks_a_larger_but_weaker_one()
    {
        await SeedProfileAsync(AcademicLevel.Masters, """["Computer Science"]""");
        await SeedListingAsync(AcademicLevel.Masters,
            fieldsJson: """["Computer Science"]""", funding: 1_000m, title: "Exact Fit Small Award");
        await SeedListingAsync(AcademicLevel.HighSchool,
            fieldsJson: """["Law"]""", funding: 500_000m, title: "Huge Unrelated Award");

        var result = await _ai.GenerateRecommendationsAsync(_userId, 5, CancellationToken.None);

        result.Items[0].ExplanationEn.Should().Contain("Exact Fit Small Award");
    }

    public void Dispose() => _db.Dispose();
}
