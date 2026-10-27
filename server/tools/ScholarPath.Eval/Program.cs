// Evaluation harness for the deterministic advisory features.
//
//   dotnet run --project server/tools/ScholarPath.Eval -- --mode eligibility
//   dotnet run --project server/tools/ScholarPath.Eval -- --mode recommendation
//
// Both modes draw their sample from a fixed seed against a seeded database, so a
// reported figure can be reproduced rather than taken on trust.
//
//   --connection  the database to evaluate against
//   --students    how many student profiles to sample
//   --listings    how many listings to sample (eligibility mode)
//   --topn        recommendations requested per student (recommendation mode)
//   --seed        sample seed
//   --ranker      current | legacy | intermediate  (recommendation mode; the checker
//                 that judges the ranking is the current one in every case)
//   --oracle      how many sampled students get the exhaustive pass (recommendation mode)
//   --csv         write a per-pair / per-student row file
//   --as-of       the date the catalog is judged on (listings whose deadline has
//                 passed by then are closed); default 2026-09-18, the date of the
//                 paper's measurements, so a later run reproduces them
//
//   dotnet run --project server/tools/ScholarPath.Eval -- --mode answers --students 120
//   (asks the assistant the known-item questions end to end and checks each answer
//   against the listing it should be about; OPENAI_API_KEY adds the hosted model)
//
//   dotnet run --project server/tools/ScholarPath.Eval -- --mode retrieval --students 120
//   (in retrieval mode --students is the number of queries drawn)

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ScholarPath.Domain.Entities;
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

var mode = arg("mode", "eligibility").ToLowerInvariant();
var connection = arg("connection",
    "Server=localhost;Database=ScholarPath_Eval;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true");
var studentCount = int.Parse(arg("students", "200"), CultureInfo.InvariantCulture);
var listingCount = int.Parse(arg("listings", "100"), CultureInfo.InvariantCulture);
var topN = int.Parse(arg("topn", "5"), CultureInfo.InvariantCulture);
var seed = int.Parse(arg("seed", "20260918"), CultureInfo.InvariantCulture);
var csvPath = arg("csv", "");
var ranker = arg("ranker", "current").ToLowerInvariant();
var oracleCount = int.Parse(arg("oracle", "40"), CultureInfo.InvariantCulture);
var asOf = DateTimeOffset.Parse(arg("as-of", "2026-09-18T00:00:00Z"), CultureInfo.InvariantCulture,
    DateTimeStyles.AssumeUniversal);
var clock = new FixedClock(asOf);

var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
await using var db = new ApplicationDbContext(options);
var ai = new LocalAiService(db, new NoopRetriever(), Options.Create(new AiOptions()), clock);

// The ranker under test can be swapped for the pre-redesign rule while the
// eligibility checker that judges its output stays current, so a before/after
// comparison is measured against one ground truth rather than two.
var legacy = new ScholarPath.Eval.LegacyRecommender(db, asOf);
var intermediate = new ScholarPath.Eval.IntermediateRecommender(db, asOf);
Func<Guid, int, CancellationToken, Task<AiRecommendationResult>> recommend = ranker switch
{
    "legacy" => legacy.GenerateRecommendationsAsync,
    "intermediate" => intermediate.GenerateRecommendationsAsync,
    "current" => ai.GenerateRecommendationsAsync,
    _ => throw new ArgumentException($"--ranker must be 'current', 'legacy' or 'intermediate', not '{ranker}'"),
};
var rng = new Random(seed);

var students = await db.UserProfiles.AsNoTracking()
    .Where(p => p.AcademicLevel != null || p.FieldOfStudy != null || p.PreferredCountriesJson != null)
    .Select(p => new { p.UserId, p.FieldOfStudy, p.AcademicLevel, p.PreferredFieldsJson })
    .ToListAsync().ConfigureAwait(false);

var listings = await db.Scholarships.AsNoTracking()
    .Where(s => s.Status == ScholarshipStatus.Open && s.Deadline > asOf)
    .Select(s => new { s.Id, s.TitleEn, s.FieldsOfStudyJson, s.TargetLevel })
    .ToListAsync().ConfigureAwait(false);

Console.WriteLine($"mode       : {mode}");
Console.WriteLine($"as of      : {asOf:yyyy-MM-dd} (listings open on that date)");
Console.WriteLine($"population : {students.Count} student profiles, {listings.Count} open listings");
{
    // How much there is for a field criterion to work with: listings that declare a
    // discipline at all, and pairs whose declared fields meet exactly.
    var listingSets = listings
        .Select(l => new HashSet<string>(Parse(l.FieldsOfStudyJson), StringComparer.OrdinalIgnoreCase))
        .ToList();
    long sharing = 0, pairs = 0;
    foreach (var st in students)
    {
        var preferred = Parse(st.PreferredFieldsJson);
        foreach (var set in listingSets)
        {
            pairs++;
            if (preferred.Count > 0 && set.Count > 0 && preferred.Any(set.Contains)) sharing++;
        }
    }
    Console.WriteLine($"fields     : {listingSets.Count(x => x.Count > 0)} of {listings.Count} open listings declare a discipline; " +
        $"{(pairs == 0 ? 0 : sharing * 100.0 / pairs):F1}% of all (student, listing) pairs share a declared field exactly");
}

if (mode == "retrieval")
{
    await RunRetrievalAsync().ConfigureAwait(false);
}
else if (mode == "answers")
{
    await RunAnswersAsync().ConfigureAwait(false);
}
else if (mode == "recommendation")
{
    await RunRecommendationAsync().ConfigureAwait(false);
}
else
{
    await RunEligibilityAsync().ConfigureAwait(false);
}

// ==================================================================== recommendation
async Task RunRecommendationAsync()
{
    var sampled = students.OrderBy(_ => rng.Next()).Take(studentCount).ToList();
    Console.WriteLine($"sample     : {sampled.Count} students, top-{topN} each (seed {seed})");
    Console.WriteLine($"ranker     : {ranker}  (eligibility ground truth: current checker in both cases)");
    Console.WriteLine();

    // A recommender is only worth its complexity if it beats drawing at random
    // from the same catalogue, so the baseline is measured on the same students.
    var verdictOf = new Dictionary<(Guid, Guid), EligibilityVerdict>();

    async Task<EligibilityVerdict> VerdictAsync(Guid user, Guid listing)
    {
        if (verdictOf.TryGetValue((user, listing), out var cached)) return cached;
        var r = await ai.CheckEligibilityAsync(user, listing, CancellationToken.None).ConfigureAwait(false);
        verdictOf[(user, listing)] = r.Verdict;
        return r.Verdict;
    }

    int recEligible = 0, recPartial = 0, recNot = 0, recCount = 0;
    int baseEligible = 0, basePartial = 0, baseNot = 0, baseCount = 0;
    double recScoreSum = 0;
    var studentsWithAny = 0;

    // What the score itself is worth: slots spent on a level the student is not
    // at, lists whose five scores tie (so their order is arbitrary), and whether a
    // higher score actually means a more usable listing.
    var levelOf = listings.ToDictionary(l => l.Id, l => l.TargetLevel);
    var wrongLevelSlots = 0;
    var allTiedLists = 0;
    var distinctScores = new HashSet<int>();
    var bandUsable = new Dictionary<string, (int Usable, int Total)>
    {
        ["80-100"] = (0, 0), ["50-79"] = (0, 0), ["0-49"] = (0, 0),
    };

    var csv = new StringBuilder();
    if (csvPath.Length > 0) csv.AppendLine("userId,rank,listing,matchScore,verdict,arm");

    var done = 0;
    foreach (var s in sampled)
    {
        var result = await recommend(s.UserId, topN, CancellationToken.None)
            .ConfigureAwait(false);

        var anyUsable = false;
        for (var i = 0; i < result.Items.Count; i++)
        {
            var item = result.Items[i];
            var v = await VerdictAsync(s.UserId, item.ScholarshipId).ConfigureAwait(false);
            recCount++;
            recScoreSum += item.MatchScore;
            if (v == EligibilityVerdict.Eligible) { recEligible++; anyUsable = true; }
            else if (v == EligibilityVerdict.PartiallyEligible) { recPartial++; anyUsable = true; }
            else recNot++;

            distinctScores.Add(item.MatchScore);
            if (s.AcademicLevel.HasValue && levelOf.TryGetValue(item.ScholarshipId, out var lvl)
                && lvl != s.AcademicLevel.Value)
            {
                wrongLevelSlots++;
            }
            var band = item.MatchScore >= 80 ? "80-100" : item.MatchScore >= 50 ? "50-79" : "0-49";
            var (bu, bt) = bandUsable[band];
            bandUsable[band] = (bu + (v == EligibilityVerdict.NotEligible ? 0 : 1), bt + 1);

            if (csvPath.Length > 0)
            {
                csv.AppendLine($"{s.UserId},{i + 1},\"{item.ScholarshipId}\",{item.MatchScore},{v},recommended");
            }
        }
        if (anyUsable) studentsWithAny++;
        if (result.Items.Count > 1 && result.Items.Select(i => i.MatchScore).Distinct().Count() == 1) allTiedLists++;

        // Baseline: the same number of listings, drawn uniformly at random.
        foreach (var l in listings.OrderBy(_ => rng.Next()).Take(topN))
        {
            var v = await VerdictAsync(s.UserId, l.Id).ConfigureAwait(false);
            baseCount++;
            if (v == EligibilityVerdict.Eligible) baseEligible++;
            else if (v == EligibilityVerdict.PartiallyEligible) basePartial++;
            else baseNot++;

            if (csvPath.Length > 0)
            {
                csv.AppendLine($"{s.UserId},0,\"{l.Id}\",,{v},random");
            }
        }

        if (++done % 25 == 0) Console.Write($"\r  evaluated {done}/{sampled.Count} students");
    }
    Console.WriteLine($"\r  evaluated {done}/{sampled.Count} students");
    Console.WriteLine();

    static double Pct(int n, int total) => total == 0 ? 0 : n * 100.0 / total;

    Console.WriteLine("VERDICT ON WHAT WAS PUT IN FRONT OF THE STUDENT");
    Console.WriteLine($"  {"",-22}{"recommended",14}{"random",12}");
    Console.WriteLine($"  {"eligible",-22}{Pct(recEligible, recCount),13:F1}%{Pct(baseEligible, baseCount),11:F1}%");
    Console.WriteLine($"  {"partially eligible",-22}{Pct(recPartial, recCount),13:F1}%{Pct(basePartial, baseCount),11:F1}%");
    Console.WriteLine($"  {"not eligible",-22}{Pct(recNot, recCount),13:F1}%{Pct(baseNot, baseCount),11:F1}%");
    Console.WriteLine();

    // Precision alone cannot say whether the recommender is good: if the catalogue
    // holds nothing a student is eligible for, an empty top-5 is the correct answer.
    // The oracle pass checks every open listing for a smaller sample of students, so
    // the top-5 can be judged against what was actually available to find.
    var oracleSample = sampled.Take(Math.Min(oracleCount, sampled.Count)).ToList();
    double chanceSum = 0, levelChanceSum = 0;
    var levelChanceStudents = 0;
    var oracleHasAny = 0;
    var oracleFound = 0;
    var oracleUsableTotal = 0;
    Console.WriteLine($"  oracle pass over {oracleSample.Count} students x {listings.Count} listings...");
    foreach (var s2 in oracleSample)
    {
        var usable = new HashSet<Guid>();
        foreach (var l in listings)
        {
            var v = await VerdictAsync(s2.UserId, l.Id).ConfigureAwait(false);
            if (v != EligibilityVerdict.NotEligible) usable.Add(l.Id);
        }
        oracleUsableTotal += usable.Count;

        // The exact expectation of a uniform draw, rather than one realised draw:
        // with a handful of usable listings in the whole catalogue a single random
        // sample is two or three events, and a ratio over it means little.
        chanceSum += usable.Count / (double)listings.Count;
        if (s2.AcademicLevel.HasValue)
        {
            var atLevel = listings.Where(l => l.TargetLevel == s2.AcademicLevel.Value).Select(l => l.Id).ToList();
            if (atLevel.Count > 0)
            {
                levelChanceSum += atLevel.Count(usable.Contains) / (double)atLevel.Count;
                levelChanceStudents++;
            }
        }
        if (usable.Count == 0) continue;
        oracleHasAny++;
        var top = await recommend(s2.UserId, topN, CancellationToken.None).ConfigureAwait(false);
        if (top.Items.Any(i => usable.Contains(i.ScholarshipId))) { oracleFound++; continue; }
        if (args.Contains("--show-missed"))
        {
            var titles = await db.Scholarships.AsNoTracking()
                .Where(x => usable.Contains(x.Id) || top.Items.Select(i => i.ScholarshipId).Contains(x.Id))
                .Select(x => new { x.Id, x.TitleEn, x.TargetCountriesJson, x.FieldsOfStudyJson })
                .ToDictionaryAsync(x => x.Id).ConfigureAwait(false);
            var p = await db.UserProfiles.AsNoTracking().FirstAsync(x => x.UserId == s2.UserId).ConfigureAwait(false);
            Console.WriteLine($"  MISSED {s2.AcademicLevel} major={p.FieldOfStudy} fields={p.PreferredFieldsJson} countries={p.PreferredCountriesJson} nat={p.Nationality}");
            foreach (var id in usable) Console.WriteLine($"    usable : {titles[id].TitleEn} {titles[id].TargetCountriesJson} {titles[id].FieldsOfStudyJson}");
            foreach (var i in top.Items) Console.WriteLine($"    shown {i.MatchScore,3}: {titles[i.ScholarshipId].TitleEn} {titles[i.ScholarshipId].TargetCountriesJson} {titles[i.ScholarshipId].FieldsOfStudyJson}");
        }
    }
    Console.WriteLine();

    var recUsable = Pct(recEligible + recPartial, recCount);
    var baseUsable = Pct(baseEligible + basePartial, baseCount);
    Console.WriteLine("HEADLINE");
    Console.WriteLine($"  precision@{topN}, counting eligible or partially eligible as usable");
    Console.WriteLine($"    recommended : {recUsable,5:F1}%   ({recEligible + recPartial} of {recCount} slots)");
    Console.WriteLine($"    random      : {baseUsable,5:F1}%   ({baseEligible + basePartial} of {baseCount} slots, one realised draw)");
    Console.WriteLine($"    lift        : {(baseUsable <= 0 ? double.PositiveInfinity : recUsable / baseUsable),5:F1}x");
    Console.WriteLine($"  students shown at least one usable listing : {Pct(studentsWithAny, sampled.Count):F1}%");
    Console.WriteLine($"  mean match score of a recommended listing  : {(recCount == 0 ? 0 : recScoreSum / recCount):F1}");
    Console.WriteLine();
    Console.WriteLine("WHAT THE SCORE IS WORTH");
    Console.WriteLine($"  slots spent on a level the student is not at : {wrongLevelSlots} of {recCount}  ({Pct(wrongLevelSlots, recCount):F1}%)");
    Console.WriteLine($"  lists whose five scores all tie              : {allTiedLists} of {sampled.Count}  ({Pct(allTiedLists, sampled.Count):F1}%)");
    Console.WriteLine($"  distinct score values across recommendations : {distinctScores.Count}");
    Console.WriteLine($"  usable share by score band");
    foreach (var (bandName, (bu, bt)) in bandUsable)
    {
        Console.WriteLine($"    {bandName,-8}{bt,6} listings   {Pct(bu, bt),6:F1}% usable");
    }
    Console.WriteLine();
    Console.WriteLine($"AGAINST WHAT WAS THERE TO FIND ({oracleSample.Count} students, every open listing checked)");
    Console.WriteLine($"  usable listings per student, on average    : {(double)oracleUsableTotal / Math.Max(1, oracleSample.Count):F1} of {listings.Count}");
    Console.WriteLine($"  students with at least one anywhere        : {oracleHasAny} of {oracleSample.Count}  ({Pct(oracleHasAny, oracleSample.Count):F1}%)");
    var exactChance = oracleSample.Count == 0 ? 0 : chanceSum * 100.0 / oracleSample.Count;
    var levelChance = levelChanceStudents == 0 ? 0 : levelChanceSum * 100.0 / levelChanceStudents;
    Console.WriteLine($"  chance a uniformly drawn slot is usable    : {exactChance:F2}%   (exact expectation)   -> lift {(exactChance <= 0 ? 0 : recUsable / exactChance):F1}x");
    Console.WriteLine($"  same, drawing only at the student's level  : {levelChance:F2}%   (exact expectation)   -> lift {(levelChance <= 0 ? 0 : recUsable / levelChance):F1}x");
    if (oracleHasAny > 0)
    {
        Console.WriteLine($"  of those, top-{topN} surfaced one           : {oracleFound} of {oracleHasAny}  ({Pct(oracleFound, oracleHasAny):F1}%)   <- recall@{topN}");
    }

    if (csvPath.Length > 0)
    {
        await File.WriteAllTextAsync(csvPath, csv.ToString()).ConfigureAwait(false);
        Console.WriteLine();
        Console.WriteLine($"per-item rows written to {Path.GetFullPath(csvPath)}");
    }
}

// ==================================================================== eligibility
async Task RunEligibilityAsync()
{
    var sampledStudents = students.OrderBy(_ => rng.Next()).Take(studentCount).ToList();
    var sampledListings = listings.OrderBy(_ => rng.Next()).Take(listingCount).ToList();
    var pairCount = sampledStudents.Count * sampledListings.Count;

    Console.WriteLine($"sample     : {sampledStudents.Count} x {sampledListings.Count} = {pairCount} pairs (seed {seed})");
    Console.WriteLine();

    var criterionCounts = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
    var verdictCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    var unrelatedReportedPartial = 0;
    var substringOnlyMatch = 0;
    var metWithoutExact = 0;
    var metByTaxonomy = 0;
    var fieldEvaluated = 0;

    var csv = new StringBuilder();
    if (csvPath.Length > 0)
    {
        csv.AppendLine("studentField,listingFields,fieldMatch,levelMatch,countryMatch,verdict,wordsShared");
    }

    var done = 0;
    foreach (var s in sampledStudents)
    {
        foreach (var l in sampledListings)
        {
            var result = await ai.CheckEligibilityAsync(s.UserId, l.Id, CancellationToken.None)
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

            if (field is not null && listingFields.Count > 0 && studentField.Length > 0)
            {
                fieldEvaluated++;
                wordsShared = listingFields.Any(f => Words(f).Overlaps(Words(studentField)));
                // A major placed under an accepted field by the taxonomy is a match by
                // design, not a divergence; it is counted on a line of its own.
                var inTaxonomy = FieldTaxonomy.FieldsOf(studentField)
                    .Any(m => listingFields.Contains(m, StringComparer.OrdinalIgnoreCase));
                var met = string.Equals(field.Match, "yes", StringComparison.OrdinalIgnoreCase);
                if (string.Equals(field.Match, "partial", StringComparison.OrdinalIgnoreCase) && !wordsShared) unrelatedReportedPartial++;
                if (met && inTaxonomy) metByTaxonomy++;
                if (met && !wordsShared && !inTaxonomy) substringOnlyMatch++;
                if (met && !inTaxonomy
                    && !listingFields.Any(f => string.Equals(f.Trim(), studentField.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    metWithoutExact++;
                }
            }

            if (csvPath.Length > 0)
            {
                var level = result.Criteria.FirstOrDefault(c => c.NameEn == "Academic level")?.Match ?? "";
                var country = result.Criteria.FirstOrDefault(c => c.NameEn == "Country")?.Match ?? "";
                csv.AppendLine(string.Join(',', Quote(studentField), Quote(string.Join('|', listingFields)),
                    field?.Match ?? "", level, country, verdict, wordsShared));
            }

            if (++done % 2000 == 0) Console.Write($"\r  evaluated {done}/{pairCount}");
        }
    }
    Console.WriteLine($"\r  evaluated {done}/{pairCount}");
    Console.WriteLine();

    Console.WriteLine("PER-CRITERION OUTCOME");
    foreach (var (name, bucket) in criterionCounts.OrderBy(k => k.Key, StringComparer.Ordinal))
    {
        var total = bucket.Values.Sum();
        Console.WriteLine($"  {name,-16} " + string.Join("   ",
            bucket.OrderByDescending(b => b.Value).Select(b => $"{b.Key} {b.Value * 100.0 / total:F1}%")));
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
        Console.WriteLine($"  unrelated field reported as 'partially met'            : {unrelatedReportedPartial,8}  {unrelatedReportedPartial * 100.0 / fieldEvaluated,5:F1}%");
        Console.WriteLine($"  'met' on substring containment with no word in common  : {substringOnlyMatch,8}  {substringOnlyMatch * 100.0 / fieldEvaluated,5:F1}%");
        Console.WriteLine($"  'met' although no accepted field equals the student's  : {metWithoutExact,8}  {metWithoutExact * 100.0 / fieldEvaluated,5:F1}%");
        Console.WriteLine($"  'met' because the major belongs to an accepted field   : {metByTaxonomy,8}  {metByTaxonomy * 100.0 / fieldEvaluated,5:F1}%");
    }

    if (csvPath.Length > 0)
    {
        await File.WriteAllTextAsync(csvPath, csv.ToString()).ConfigureAwait(false);
        Console.WriteLine();
        Console.WriteLine($"per-pair rows written to {Path.GetFullPath(csvPath)}");
    }
}

// ==================================================================== known-item queries
// Retrieval and answer modes draw the same queries: listings whose (level, first
// field, first country, funding type) is unique, shuffled at the seed.
async Task<(List<KDoc> Docs, List<(Guid DocId, string Query)> Queries)> KnownItemQueriesAsync()
{
    // The knowledge base is re-embedded from scratch by each provider, so the two
    // are compared on identical text rather than on whatever happened to be stored.
    var docs = await db.KnowledgeDocuments.AsNoTracking()
        .Where(k => k.SourceType == KnowledgeSourceType.Scholarship && k.SourceId != null)
        .Select(k => new KDoc(k.Id, k.SourceId, (k.TitleEn ?? "") + " " + (k.ContentEn ?? "")))
        .ToListAsync().ConfigureAwait(false);

    var meta = await db.Scholarships.AsNoTracking()
        .Select(x => new { x.Id, x.TargetLevel, x.FieldsOfStudyJson, x.TargetCountriesJson, x.FundingType })
        .ToListAsync().ConfigureAwait(false);
    var metaById = meta.ToDictionary(m => m.Id);

    Console.WriteLine($"corpus     : {docs.Count} scholarship knowledge documents");

    // A query describes a listing only through its structured fields, never its
    // wording, so retrieval has to bridge from a description to a document rather
    // than match strings. Only listings whose combination is unique in the corpus
    // are used, so exactly one document is the right answer.
    static string QueryFor(string level, string field, string country, string funding) =>
        $"{funding} {level} scholarship in {field} to study in {country}";

    var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    var candidates = new List<(Guid DocId, string Query)>();
    foreach (var d in docs)
    {
        if (d.SourceId is null || !metaById.TryGetValue(d.SourceId.Value, out var m)) continue;
        var fields = Parse(m.FieldsOfStudyJson);
        var countries = Parse(m.TargetCountriesJson);
        if (fields.Count == 0 || countries.Count == 0) continue;
        var key = $"{m.TargetLevel}|{fields[0]}|{countries[0]}|{m.FundingType}";
        seen[key] = seen.GetValueOrDefault(key) + 1;
        candidates.Add((d.Id, QueryFor(m.TargetLevel.ToString(), fields[0], countries[0], m.FundingType.ToString())));
    }

    var queries = new List<(Guid DocId, string Query)>();
    foreach (var (docId, query) in candidates)
    {
        var d = docs.First(x => x.Id == docId);
        var m = metaById[d.SourceId!.Value];
        var fields = Parse(m.FieldsOfStudyJson);
        var countries = Parse(m.TargetCountriesJson);
        var key = $"{m.TargetLevel}|{fields[0]}|{countries[0]}|{m.FundingType}";
        if (seen[key] == 1) queries.Add((docId, query));
    }

    var uniqueCount = queries.Count;
    queries = queries.OrderBy(_ => rng.Next()).Take(studentCount).ToList();
    Console.WriteLine($"queries    : {queries.Count} drawn at the seed from the {uniqueCount} listings whose (level, first field, first country, funding type) combination is unique");
    Console.WriteLine();

    return (docs, queries);
}

// ==================================================================== retrieval
async Task RunRetrievalAsync()
{
    var (docs, queries) = await KnownItemQueriesAsync().ConfigureAwait(false);

    var services = new ServiceCollection();
    services.AddHttpClient();
    var sp = services.BuildServiceProvider();
    var httpFactory = sp.GetRequiredService<IHttpClientFactory>();

    var aiOpts = Options.Create(new AiOptions
    {
        AzureOpenAi = new AzureOpenAiOptions
        {
            Endpoint = Environment.GetEnvironmentVariable("AZ_OAI_ENDPOINT"),
            ApiKey = Environment.GetEnvironmentVariable("AZ_OAI_KEY"),
            EmbeddingDeploymentName = "text-embedding-3-small",
            EmbeddingDimensions = 1536,
        },
    });

    var local = new LocalEmbeddingService();
    var providers = new List<(string Label, IEmbeddingService Service)>
    {
        ("local-hash-v1 (previous fallback)", local),
    };
    if (!string.IsNullOrWhiteSpace(aiOpts.Value.AzureOpenAi.Endpoint))
    {
        providers.Add(("text-embedding-3-small", new AzureOpenAiEmbeddingService(
            httpFactory, aiOpts, local, NullLogger<AzureOpenAiEmbeddingService>.Instance)));
    }

    static double Cosine(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        var n = Math.Min(a.Length, b.Length);
        for (var i = 0; i < n; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        return na <= 0 || nb <= 0 ? 0 : dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    const int TopK = 4;   // Ai:RagTopK
    Console.WriteLine($"{"provider",-34}{"recall@" + TopK,12}{"MRR",10}{"mean rank",12}");

    // BM25 is the retriever the platform now uses when no embedding model is
    // configured, so this row measures the shipped Bm25Index itself.
    {
        var bm25 = new Bm25Index([.. docs.Select(d => d.Text)]);
        var docIndex = docs.Select((d, i) => (d.Id, i)).ToDictionary(x => x.Id, x => x.i);
        int bmHits = 0; double bmMrr = 0, bmRank = 0;
        foreach (var (docId, query) in queries)
        {
            var scores = bm25.Score(query);
            var target = docIndex[docId];
            // Ties are counted against the target, so a tie never flatters the result.
            var rank = 1 + scores.Where((sc, di) => di != target && sc >= scores[target]).Count();
            bmRank += rank;
            if (rank <= TopK) bmHits++;
            bmMrr += 1.0 / rank;
        }
        Console.WriteLine($"{"BM25 (shipped fallback)",-34}{bmHits * 100.0 / queries.Count,11:F1}%{bmMrr / queries.Count,10:F3}{bmRank / queries.Count,12:F1}");
    }

    foreach (var (label, svc) in providers)
    {
        var docVectors = new List<float[]>(docs.Count);
        for (var i = 0; i < docs.Count; i += 64)
        {
            var slice = docs.Skip(i).Take(64).Select(d => d.Text).ToList();
            docVectors.AddRange(await ScholarPath.Eval.StrictEmbed.BatchAsync(svc, slice).ConfigureAwait(false));
            if (i % 320 == 0) Console.WriteLine($"  {label}: embedded {Math.Min(i + 64, docs.Count)}/{docs.Count} documents");
        }

        var queryVectors = new List<float[]>(queries.Count);
        for (var i = 0; i < queries.Count; i += 64)
        {
            var slice = queries.Skip(i).Take(64).Select(q => q.Query).ToList();
            queryVectors.AddRange(await ScholarPath.Eval.StrictEmbed.BatchAsync(svc, slice).ConfigureAwait(false));
        }

        var index = docs.Select((d, i) => (d.Id, i)).ToDictionary(x => x.Id, x => x.i);
        var lexicalIndex = new Bm25Index([.. docs.Select(d => d.Text)]);
        var hybrid = label != "local-hash-v1 (previous fallback)";
        int hits = 0; double mrrSum = 0, rankSum = 0;
        int hHits = 0; double hMrr = 0, hRank = 0;
        for (var qi = 0; qi < queries.Count; qi++)
        {
            var qv = queryVectors[qi];
            var target = index[queries[qi].DocId];
            var cos = new double[docs.Count];
            for (var di = 0; di < docs.Count; di++) cos[di] = Cosine(qv, docVectors[di]);
            var rank = 1 + cos.Where((c, di) => di != target && c > cos[target]).Count();
            rankSum += rank;
            if (rank <= TopK) hits++;
            mrrSum += 1.0 / rank;

            if (hybrid)
            {
                var fused = ScholarPath.Eval.RankFusion.Fuse([cos.Select(c => Math.Max(c, 0)).ToArray(), lexicalIndex.Score(queries[qi].Query)]);
                var hr = 1 + fused.Where((f, di) => di != target && f >= fused[target]).Count();
                hRank += hr;
                if (hr <= TopK) hHits++;
                hMrr += 1.0 / hr;
            }
        }

        Console.WriteLine($"{label,-34}{hits * 100.0 / queries.Count,11:F1}%{mrrSum / queries.Count,10:F3}{rankSum / queries.Count,12:F1}");
        if (hybrid)
        {
            var hl = "hybrid: " + label.Split(' ')[0] + " + BM25 (RRF)";
            Console.WriteLine($"{hl,-34}{hHits * 100.0 / queries.Count,11:F1}%{hMrr / queries.Count,10:F3}{hRank / queries.Count,12:F1}");
        }
    }

    Console.WriteLine();
    Console.WriteLine($"  recall@{TopK} is the share of queries whose one correct document appeared in the");
    Console.WriteLine($"  top {TopK} the retriever actually returns; mean rank is out of {docs.Count}.");
}

// ==================================================================== answers
// End-to-end check of the assistant. Each known-item question is asked through the
// chat entry point the platform uses, and the answer is judged on two counts:
// whether it is about the listing the question describes (its first source is that
// listing), and whether every figure it states (amounts, dates, counts) appears in
// the documents it was given. The second is the grounding rule of Section 5.2
// checked mechanically: a figure absent from the retrieved text was not taken from it.
async Task RunAnswersAsync()
{
    var (docs, queries) = await KnownItemQueriesAsync().ConfigureAwait(false);
    var sourceOf = docs.ToDictionary(d => d.Id, d => d.SourceId);
    var textBySource = docs.Where(d => d.SourceId != null)
        .GroupBy(d => d.SourceId!.Value).ToDictionary(g => g.Key, g => string.Join(" ", g.Select(x => x.Text)));

    var aiOptions = new AiOptions();
    var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    if (!string.IsNullOrWhiteSpace(apiKey)) aiOptions.OpenAi.ApiKey = apiKey;
    var opts = Options.Create(aiOptions);

    var retriever = new KnowledgeRetriever(db, new LocalEmbeddingService());
    var local = new LocalAiService(db, retriever, opts, clock);
    var providers = new List<(string Label, IAiService Service)> { ("local (BM25 + extractive)", local) };
    var azEndpoint = Environment.GetEnvironmentVariable("AZ_OAI_ENDPOINT");
    var azKey = Environment.GetEnvironmentVariable("AZ_OAI_KEY");
    if (!string.IsNullOrWhiteSpace(azEndpoint) && !string.IsNullOrWhiteSpace(azKey))
    {
        aiOptions.AzureOpenAi.Endpoint = azEndpoint;
        aiOptions.AzureOpenAi.ApiKey = azKey;
        aiOptions.AzureOpenAi.DeploymentName = Environment.GetEnvironmentVariable("AZ_OAI_CHAT") ?? "gpt-4.1-mini";
        aiOptions.AzureOpenAi.EmbeddingDeploymentName = "text-embedding-3-small";
        var azServices = new ServiceCollection();
        azServices.AddHttpClient("azure-openai");
        var azHttp = azServices.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        var azEmbed = new AzureOpenAiEmbeddingService(azHttp, opts, new LocalEmbeddingService(),
            NullLogger<AzureOpenAiEmbeddingService>.Instance);
        var hybridRetriever = await ScholarPath.Eval.HybridRetriever.BuildAsync(db, azEmbed).ConfigureAwait(false);
        providers.Add(($"azure {aiOptions.AzureOpenAi.DeploymentName} + embeddings", new AzureOpenAiService(
            azHttp, opts, hybridRetriever, local, db, NullLogger<AzureOpenAiService>.Instance)));
    }
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        var services = new ServiceCollection();
        services.AddHttpClient("openai");
        var http = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        providers.Add(($"openai ({aiOptions.OpenAi.Model}) + BM25", new OpenAiService(
            http, opts, retriever, local, NullLogger<OpenAiService>.Instance)));
    }

    // A figure is an amount, a date part, or a count stated in the answer. List
    // numbering ("1.", "2." at the start of an item) is formatting, not a claim,
    // so it is removed before figures are read.
    static IEnumerable<string> Figures(string text)
    {
        var body = System.Text.RegularExpressions.Regex.Replace(text, @"(?m)^\s*(?:[-*]\s*)?\d+[.)]\s|\[\d+\]", " ");
        return System.Text.RegularExpressions.Regex.Matches(body, @"\d[\d,.]*\d|\d")
            .Select(m => m.Value.TrimEnd('.', ',').Replace(",", "", StringComparison.Ordinal).TrimStart('0'))
            .Where(v => v.Length > 0);
    }

    var ungrounded = new StringBuilder();
    Console.WriteLine($"{"provider",-34}{"answered",10}{"right listing",15}{"figures",9}{"grounded",10}{"fell back",11}");
    foreach (var (label, svc) in providers)
    {
        int answered = 0, right = 0, figures = 0, grounded = 0, fellBack = 0;
        foreach (var (docId, query) in queries)
        {
            var r = await svc.AskAsync(Guid.Empty, "eval", query, [], CancellationToken.None).ConfigureAwait(false);
            // The hosted providers answer with the local router when the call fails;
            // such answers are counted, so a fallback is never reported as the model.
            if (svc != local && r.Message.StartsWith("Here is what I found in ScholarPath's knowledge base", StringComparison.Ordinal)) fellBack++;
            if (r.Sources.Count == 0) continue;
            answered++;
            if (r.Sources[0].ScholarshipId == sourceOf[docId]) right++;

            var given = string.Join(" ", r.Sources
                .Where(s => s.ScholarshipId != null && textBySource.ContainsKey(s.ScholarshipId.Value))
                .Select(s => textBySource[s.ScholarshipId!.Value]));
            var givenFigures = new HashSet<string>(Figures(given), StringComparer.Ordinal);
            foreach (var f in Figures(r.Message))
            {
                figures++;
                if (givenFigures.Contains(f)) grounded++;
                else if (csvPath.Length > 0)
                {
                    var at = r.Message.IndexOf(f, StringComparison.Ordinal);
                    var ctx = at < 0 ? r.Message : r.Message.Substring(Math.Max(0, at - 60), Math.Min(140, r.Message.Length - Math.Max(0, at - 60)));
                    var flat = System.Text.RegularExpressions.Regex.Replace(ctx, @"\s+", " ");
                    ungrounded.AppendLine(label + "|" + f + "|" + flat);
                }
            }
        }
        Console.WriteLine($"{label,-34}{answered,10}{$"{right} ({right * 100.0 / queries.Count:F1}%)",15}{figures,9}" +
            $"{(figures == 0 ? "-" : $"{grounded * 100.0 / figures:F1}%"),10}{fellBack,11}");
    }
    Console.WriteLine();
    Console.WriteLine("  right listing: the first source of the answer is the listing the question describes.");
    Console.WriteLine("  grounded: share of figures in the answers that occur in the documents the answer was given.");
    if (csvPath.Length > 0) await File.WriteAllTextAsync(csvPath, ungrounded.ToString()).ConfigureAwait(false);
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

internal sealed record KDoc(Guid Id, Guid? SourceId, string Text);

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class NoopRetriever : IKnowledgeRetriever
{
    public Task<IReadOnlyList<RetrievedDocument>> RetrieveAsync(string query, int topK, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<RetrievedDocument>>([]);
}
