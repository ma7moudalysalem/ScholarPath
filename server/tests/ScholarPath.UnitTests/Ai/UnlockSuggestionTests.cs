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
/// A listing counts toward a suggestion only when that one change would make the
/// student fully eligible: exactly one criterion fails and every other is met.
/// </summary>
public sealed class UnlockSuggestionTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly LocalAiService _ai;
    private readonly Guid _userId = Guid.NewGuid();

    public UnlockSuggestionTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _ai = new LocalAiService(_db, new NoopRetriever(), Options.Create(new AiOptions()));
    }

    private sealed class NoopRetriever : IKnowledgeRetriever
    {
        public Task<IReadOnlyList<RetrievedDocument>> RetrieveAsync(string query, int topK, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<RetrievedDocument>>([]);
    }

    private void Listing(AcademicLevel level, string countries, string fields) => _db.Scholarships.Add(new Scholarship
    {
        Id = Guid.NewGuid(), TitleEn = "L", TitleAr = "ل", DescriptionEn = "d", DescriptionAr = "و",
        Slug = $"s-{Guid.NewGuid():N}", Status = ScholarshipStatus.Open, Deadline = DateTimeOffset.UtcNow.AddDays(30),
        TargetLevel = level, TargetCountriesJson = countries, FieldsOfStudyJson = fields,
    });

    [Fact]
    public async Task Counts_only_listings_one_change_away_from_full_eligibility()
    {
        _db.UserProfiles.Add(new UserProfile
        {
            Id = Guid.NewGuid(), UserId = _userId, AcademicLevel = AcademicLevel.Masters,
            FieldOfStudy = "Civil Engineering", PreferredCountriesJson = """["DE"]""", Nationality = "EG",
        });
        Listing(AcademicLevel.Masters, """["FR"]""", """["Engineering"]""");   // add France
        Listing(AcademicLevel.Masters, """["FR"]""", """["Engineering"]""");   // add France
        Listing(AcademicLevel.Masters, """["DE"]""", """["Engineering"]""");   // already eligible
        Listing(AcademicLevel.PhD, """["DE"]""", """["Engineering"]""");       // reach PhD
        Listing(AcademicLevel.Masters, """["FR"]""", """["Medicine & Health"]"""); // two criteria fail
        await _db.SaveChangesAsync();

        var result = await _ai.SuggestUnlocksAsync(_userId, 5, CancellationToken.None);

        result.Select(r => (r.Change, r.Value, r.ListingsUnlocked)).Should().Equal(
            ("AddDestination", "FR", 2),
            ("ReachLevel", "PhD", 1));
    }

    [Fact]
    public async Task No_profile_gives_no_suggestions()
        => (await _ai.SuggestUnlocksAsync(Guid.NewGuid(), 5, CancellationToken.None)).Should().BeEmpty();

    public void Dispose() => _db.Dispose();
}
