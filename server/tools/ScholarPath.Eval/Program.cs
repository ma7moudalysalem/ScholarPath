// Eligibility-checker evaluation harness.
//
// Runs the delivered eligibility checker over a sample of (student, listing) pairs
// drawn from a seeded database and reports, per criterion, how its verdicts are
// distributed and where they diverge from the semantics the interface promises.
//
//   dotnet run --project server/tools/ScholarPath.Eval -- \
//       --connection "Server=localhost;Database=ScholarPath_Eval;Trusted_Connection=True;TrustServerCertificate=true" \
//       --students 200 --listings 100 --seed 20260918 --csv eligibility-eval.csv
//
// The sample is drawn with a fixed seed, so a reported figure can be reproduced.

using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using ScholarPath.Infrastructure.Settings;

var arg = (string name, string fallback) =>
{
    var i = Array.IndexOf(args, "--" + name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
};

var connection = arg("connection",
    "Server=localhost;Database=ScholarPath_Eval;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true");
var studentCount = int.Parse(arg("students", "200"), CultureInfo.InvariantCulture);
var listingCount = int.Parse(arg("listings", "100"), CultureInfo.InvariantCulture);
var seed = int.Parse(arg("seed", "20260918"), CultureInfo.InvariantCulture);
var csvPath = arg("csv", "");

var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
await using var db = new ApplicationDbContext(options);

var checker = new LocalAiService(db, new NoopRetriever(), Options.Create(new AiOptions()));

// ---------------------------------------------------------------- sample
var rng = new Random(seed);

var students = await db.UserProfiles.AsNoTracking()
    .Where(p => p.AcademicLevel != null || p.FieldOfStudy != null || p.PreferredCountriesJson != null)
    .Select(p => new { p.UserId, p.AcademicLevel, p.FieldOfStudy })
    .ToListAsync().ConfigureAwait(false);

var listings = await db.Scholarships.AsNoTracking()
    .Where(s => s.Status == ScholarshipStatus.Open && s.Deadline > DateTimeOffset.UtcNow)
    .Select(s => new { s.Id, s.TitleEn, s.TargetLevel, s.FieldsOfStudyJson, s.TargetCountriesJson })
    .ToListAsync().ConfigureAwait(false);

Console.WriteLine($"population : {students.Count} student profiles, {listings.Count} open listings");

var sampledStudents = students.OrderBy(_ => rng.Next()).Take(studentCount).ToList();
var sampledListings = listings.OrderBy(_ => rng.Next()).Take(listingCount).ToList();
var pairCount = sampledStudents.Count * sampledListings.Count;

Console.WriteLine($"sample     : {sampledStudents.Count} x {sampledListings.Count} = {pairCount} pairs (seed {seed})");
Console.WriteLine();

// ---------------------------------------------------------------- run
var criterionCounts = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
var verdictCounts = new Dictionary<string, int>(StringComparer.Ordinal);

// Divergences between what the checker reports and what the interface promises.
var unrelatedReportedPartial = 0;   // "partially met" for a field with nothing in common
var substringOnlyMatch = 0;         // "met" via substring containment with no word in common
var fieldEvaluated = 0;

var csv = new StringBuilder();
if (csvPath.Length > 0)
{
    csv.AppendLine("studentField,listingFields,fieldMatch,levelMatch,countryMatch,verdict,wordsShared,substringOnly");
}

var done = 0;
foreach (var s in sampledStudents)
{
    foreach (var l in sampledListings)
    {
        var result = await checker.CheckEligibilityAsync(s.UserId, l.Id, CancellationToken.None)
            .ConfigureAwait(false);

        foreach (var c in result.Criteria)
        {
            if (!criterionCounts.TryGetValue(c.NameEn, out var bucket))
            {
                bucket = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                criterionCounts[c.NameEn] = bucket;
            }
            bucket[c.Match] = bucket.GetValueOrDefault(c.Match) + 1;
        }

        var verdict = result.Verdict.ToString();
        verdictCounts[verdict] = verdictCounts.GetValueOrDefault(verdict) + 1;

        var field = result.Criteria.FirstOrDefault(c => c.NameEn == "Field of study");
        var listingFields = Parse(l.FieldsOfStudyJson);
        var studentField = s.FieldOfStudy ?? "";

        var wordsShared = false;
        var substringOnly = false;
        if (field is not null && listingFields.Count > 0 && studentField.Length > 0)
        {
            fieldEvaluated++;
            wordsShared = listingFields.Any(f => Words(f).Overlaps(Words(studentField)));

            if (string.Equals(field.Match, "partial", StringComparison.OrdinalIgnoreCase) && !wordsShared)
            {
                unrelatedReportedPartial++;
            }

            if (string.Equals(field.Match, "yes", StringComparison.OrdinalIgnoreCase) && !wordsShared)
            {
                substringOnly = true;
                substringOnlyMatch++;
            }
        }

        if (csvPath.Length > 0)
        {
            var level = result.Criteria.FirstOrDefault(c => c.NameEn == "Academic level")?.Match ?? "";
            var country = result.Criteria.FirstOrDefault(c => c.NameEn == "Country")?.Match ?? "";
            csv.AppendLine(string.Join(',',
                Quote(studentField), Quote(string.Join('|', listingFields)),
                field?.Match ?? "", level, country, verdict, wordsShared, substringOnly));
        }

        if (++done % 2000 == 0) Console.Write($"\r  evaluated {done}/{pairCount}");
    }
}
Console.WriteLine($"\r  evaluated {done}/{pairCount}");
Console.WriteLine();

// ---------------------------------------------------------------- report
Console.WriteLine("PER-CRITERION OUTCOME");
foreach (var (name, bucket) in criterionCounts.OrderBy(k => k.Key, StringComparer.Ordinal))
{
    var total = bucket.Values.Sum();
    var parts = bucket.OrderByDescending(b => b.Value)
        .Select(b => $"{b.Key} {b.Value * 100.0 / total:F1}%");
    Console.WriteLine($"  {name,-16} {string.Join("   ", parts)}");
}

Console.WriteLine();
Console.WriteLine("OVERALL VERDICT");
foreach (var (v, n) in verdictCounts.OrderByDescending(v => v.Value))
{
    Console.WriteLine($"  {v,-20} {n,8}  {n * 100.0 / pairCount,5:F1}%");
}

Console.WriteLine();
Console.WriteLine("DIVERGENCE FROM THE PROMISED SEMANTICS");
Console.WriteLine($"  pairs where the field criterion was actually evaluated : {fieldEvaluated}");
if (fieldEvaluated > 0)
{
    Console.WriteLine($"  unrelated field reported as 'partially met'            : {unrelatedReportedPartial,8}  "
        + $"{unrelatedReportedPartial * 100.0 / fieldEvaluated,5:F1}%");
    Console.WriteLine($"  'met' on substring containment with no word in common  : {substringOnlyMatch,8}  "
        + $"{substringOnlyMatch * 100.0 / fieldEvaluated,5:F1}%");
}

if (csvPath.Length > 0)
{
    await File.WriteAllTextAsync(csvPath, csv.ToString()).ConfigureAwait(false);
    Console.WriteLine();
    Console.WriteLine($"per-pair rows written to {Path.GetFullPath(csvPath)}");
}

static List<string> Parse(string? json)
{
    if (string.IsNullOrWhiteSpace(json)) return [];
    try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
    catch (JsonException) { return []; }
}

static HashSet<string> Words(string value)
{
    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var t in value.Split([' ', '&', ',', '/', '-', '(', ')'], StringSplitOptions.RemoveEmptyEntries))
    {
        if (t.Length >= 3 && t is not ("and" or "the" or "for" or "other")) set.Add(t);
    }
    return set;
}

static string Quote(string v) => '"' + v.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';

internal sealed class NoopRetriever : IKnowledgeRetriever
{
    public Task<IReadOnlyList<RetrievedDocument>> RetrieveAsync(string query, int topK, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<RetrievedDocument>>([]);
}
