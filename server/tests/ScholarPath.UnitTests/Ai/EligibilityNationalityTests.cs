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
/// A listing may admit only some nationalities, independently of where the award
/// is held. The criterion appears only when the listing restricts nationality.
/// </summary>
public sealed class EligibilityNationalityTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly LocalAiService _ai;
    private readonly Guid _userId = Guid.NewGuid();

    public EligibilityNationalityTests()
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

    private async Task<AiEligibilityResult> CheckAsync(string? nationalitiesJson, string? nationality)
    {
        var s = new Scholarship
        {
            Id = Guid.NewGuid(),
            TitleEn = "Test Scholarship",
            TitleAr = "منحة اختبار",
            DescriptionEn = "Description",
            DescriptionAr = "وصف",
            Slug = $"sch-{Guid.NewGuid():N}",
            Status = ScholarshipStatus.Open,
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
            TargetLevel = AcademicLevel.Masters,
            TargetCountriesJson = """["DE"]""",
            EligibleNationalitiesJson = nationalitiesJson,
        };
        _db.Scholarships.Add(s);
        _db.UserProfiles.Add(new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            AcademicLevel = AcademicLevel.Masters,
            Nationality = nationality,
            PreferredCountriesJson = """["DE"]""",
        });
        await _db.SaveChangesAsync();
        return await _ai.CheckEligibilityAsync(_userId, s.Id, CancellationToken.None);
    }

    private static string? Nationality(AiEligibilityResult r)
        => r.Criteria.SingleOrDefault(c => c.NameEn == "Nationality")?.Match;

    [Fact]
    public async Task Unrestricted_listing_adds_no_nationality_criterion()
        => Nationality(await CheckAsync(null, "EG")).Should().BeNull();

    [Fact]
    public async Task Listed_nationality_is_met_across_name_and_code()
        => Nationality(await CheckAsync("""["Egypt","Jordan"]""", "EG")).Should().Be("yes");

    [Fact]
    public async Task Unlisted_nationality_makes_the_student_ineligible_despite_the_destination()
    {
        var r = await CheckAsync("""["Egypt"]""", "FR");
        Nationality(r).Should().Be("no");
        r.Verdict.Should().Be(EligibilityVerdict.NotEligible);
    }

    [Fact]
    public async Task Unstated_nationality_is_unknown()
        => Nationality(await CheckAsync("""["Egypt"]""", null)).Should().Be("unknown");

    [Fact]
    public async Task Recommendations_leave_out_a_listing_closed_to_the_students_nationality()
    {
        var closed = await CheckAsync("""["Egypt"]""", "FR");
        closed.Verdict.Should().Be(EligibilityVerdict.NotEligible);
        var open = new Scholarship
        {
            Id = Guid.NewGuid(), TitleEn = "Open", TitleAr = "مفتوحة", DescriptionEn = "d", DescriptionAr = "و",
            Slug = $"sch-{Guid.NewGuid():N}", Status = ScholarshipStatus.Open,
            Deadline = DateTimeOffset.UtcNow.AddDays(30), TargetLevel = AcademicLevel.Masters,
        };
        _db.Scholarships.Add(open);
        await _db.SaveChangesAsync();

        var result = await _ai.GenerateRecommendationsAsync(_userId, 5, CancellationToken.None);

        result.Items.Select(i => i.ScholarshipId).Should().Equal(open.Id);
    }

    public void Dispose() => _db.Dispose();
}
