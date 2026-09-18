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
/// The field-of-study criterion must distinguish a field that is genuinely adjacent
/// to what a listing accepts from one that has nothing to do with it. Reporting both
/// as "partially met" gives a student nothing to act on.
/// </summary>
public sealed class EligibilityFieldMatchTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly LocalAiService _ai;
    private readonly Guid _userId = Guid.NewGuid();

    public EligibilityFieldMatchTests()
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

    private async Task<Guid> SeedListingAsync(string? fieldsJson)
    {
        var s = new Scholarship
        {
            Id = Guid.NewGuid(),
            OwnerScholarshipProviderId = Guid.NewGuid(),
            TitleEn = "Test Scholarship",
            TitleAr = "منحة اختبار",
            DescriptionEn = "Description",
            DescriptionAr = "وصف",
            Slug = $"sch-{Guid.NewGuid():N}",
            CategoryId = Guid.NewGuid(),
            Status = ScholarshipStatus.Open,
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
            TargetLevel = AcademicLevel.Masters,
            FieldsOfStudyJson = fieldsJson,
        };
        _db.Scholarships.Add(s);
        await _db.SaveChangesAsync();
        return s.Id;
    }

    private async Task SeedProfileAsync(string? fieldOfStudy)
    {
        _db.UserProfiles.Add(new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            AcademicLevel = AcademicLevel.Masters,
            FieldOfStudy = fieldOfStudy,
        });
        await _db.SaveChangesAsync();
    }

    private async Task<string> FieldMatchAsync(string? listingFieldsJson, string? studentField)
    {
        var id = await SeedListingAsync(listingFieldsJson);
        await SeedProfileAsync(studentField);
        var result = await _ai.CheckEligibilityAsync(_userId, id, CancellationToken.None);
        return result.Criteria.Single(c => c.NameEn == "Field of study").Match;
    }

    [Fact]
    public async Task Exactly_the_accepted_discipline_is_met()
        => (await FieldMatchAsync("""["Computer Science"]""", "Computer Science"))
            .Should().Be("yes");

    [Fact]
    public async Task Adjacent_discipline_sharing_a_word_is_partially_met()
        => (await FieldMatchAsync("""["Engineering"]""", "Software Engineering"))
            .Should().Be("partial");

    [Fact]
    public async Task Unrelated_discipline_is_not_met_rather_than_partially_met()
        => (await FieldMatchAsync("""["Computer Science"]""", "Law"))
            .Should().Be("no");

    [Fact]
    public async Task Listing_open_to_any_discipline_is_met_without_consulting_the_profile()
        => (await FieldMatchAsync("[]", "Law"))
            .Should().Be("yes");

    [Fact]
    public async Task Student_who_has_not_stated_a_field_is_unknown_not_ineligible()
        => (await FieldMatchAsync("""["Computer Science"]""", null))
            .Should().Be("unknown");

    [Fact]
    public async Task Any_accepted_discipline_may_satisfy_the_criterion()
        => (await FieldMatchAsync("""["Law","Computer Science","Education"]""", "Computer Science"))
            .Should().Be("yes");

    [Fact]
    public async Task A_shared_connective_alone_does_not_make_a_field_adjacent()
        // "and" carries no meaning, so these two have nothing in common.
        => (await FieldMatchAsync("""["Arts & Humanities"]""", "Science and Technology"))
            .Should().Be("no");

    [Fact]
    public async Task Unrelated_field_alone_does_not_make_a_student_ineligible_overall()
    {
        // The field criterion now reports "no", which the aggregation rule turns into
        // NotEligible. That is the intended behaviour, and it is stated here so the
        // consequence of the change is visible rather than incidental.
        var id = await SeedListingAsync("""["Computer Science"]""");
        await SeedProfileAsync("Law");

        var result = await _ai.CheckEligibilityAsync(_userId, id, CancellationToken.None);

        result.Verdict.Should().Be(EligibilityVerdict.NotEligible);
        result.SummaryEn.Should().Contain("not met");
    }

    public void Dispose() => _db.Dispose();
}
