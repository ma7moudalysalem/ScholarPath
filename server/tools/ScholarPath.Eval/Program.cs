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
//   --csv         write a per-pair / per-student row file

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

var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
await using var db = new ApplicationDbContext(options);
var ai = new LocalAiService(db, new NoopRetriever(), Options.Create(new AiOptions()));
var rng = new Random(seed);

var students = await db.UserProfiles.AsNoTracking()
    .Where(p => p.AcademicLevel != null || p.FieldOfStudy != null || p.PreferredCountriesJson != null)
    .Select(p => new { p.UserId, p.FieldOfStudy })
    .ToListAsync().ConfigureAwait(false);

var listings = await db.Scholarships.AsNoTracking()
    .Where(s => s.Status == ScholarshipStatus.Open && s.Deadline > DateTimeOffset.UtcNow)
    .Select(s => new { s.Id, s.TitleEn, s.FieldsOfStudyJson })
    .ToListAsync().ConfigureAwait(false);

Console.WriteLine($"mode       : {mode}");
Console.WriteLine($"population : {students.Count} student profiles, {listings.Count} open listings");

if (mode == "retrieval")
{
    await RunRetrievalAsync().ConfigureAwait(false);
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

    var csv = new StringBuilder();
    if (csvPath.Length > 0) csv.AppendLine("userId,rank,listing,matchScore,verdict,arm");

    var done = 0;
    foreach (var s in sampled)
    {
        var result = await ai.GenerateRecommendationsAsync(s.UserId, topN, CancellationToken.None)
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

            if (csvPath.Length > 0)
            {
                csv.AppendLine($"{s.UserId},{i + 1},\"{item.ScholarshipId}\",{item.MatchScore},{v},recommended");
            }
        }
        if (anyUsable) studentsWithAny++;

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
    var oracleSample = sampled.Take(Math.Min(40, sampled.Count)).ToList();
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
        if (usable.Count == 0) continue;
        oracleHasAny++;
        var top = await ai.GenerateRecommendationsAsync(s2.UserId, topN, CancellationToken.None).ConfigureAwait(false);
        if (top.Items.Any(i => usable.Contains(i.ScholarshipId))) oracleFound++;
    }
    Console.WriteLine();

    var recUsable = Pct(recEligible + recPartial, recCount);
    var baseUsable = Pct(baseEligible + basePartial, baseCount);
    Console.WriteLine("HEADLINE");
    Console.WriteLine($"  precision@{topN}, counting eligible or partially eligible as usable");
    Console.WriteLine($"    recommended : {recUsable,5:F1}%");
    Console.WriteLine($"    random      : {baseUsable,5:F1}%");
    Console.WriteLine($"    lift        : {(baseUsable <= 0 ? double.PositiveInfinity : recUsable / baseUsable),5:F1}x");
    Console.WriteLine($"  students shown at least one usable listing : {Pct(studentsWithAny, sampled.Count):F1}%");
    Console.WriteLine($"  mean match score of a recommended listing  : {(recCount == 0 ? 0 : recScoreSum / recCount):F1}");
    Console.WriteLine();
    Console.WriteLine($"AGAINST WHAT WAS THERE TO FIND ({oracleSample.Count} students, every open listing checked)");
    Console.WriteLine($"  usable listings per student, on average    : {(double)oracleUsableTotal / Math.Max(1, oracleSample.Count):F1} of {listings.Count}");
    Console.WriteLine($"  students with at least one anywhere        : {oracleHasAny} of {oracleSample.Count}  ({Pct(oracleHasAny, oracleSample.Count):F1}%)");
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
                if (string.Equals(field.Match, "partial", StringComparison.OrdinalIgnoreCase) && !wordsShared) unrelatedReportedPartial++;
                if (string.Equals(field.Match, "yes", StringComparison.OrdinalIgnoreCase) && !wordsShared) substringOnlyMatch++;
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
    }

    if (csvPath.Length > 0)
    {
        await File.WriteAllTextAsync(csvPath, csv.ToString()).ConfigureAwait(false);
        Console.WriteLine();
        Console.WriteLine($"per-pair rows written to {Path.GetFullPath(csvPath)}");
    }
}

// ==================================================================== retrieval
async Task RunRetrievalAsync()
{
    // The knowledge base is re-embedded from scratch by each provider, so the two
    // are compared on identical text rather than on whatever happened to be stored.
    var docs = await db.KnowledgeDocuments.AsNoTracking()
        .Where(k => k.SourceType == KnowledgeSourceType.Scholarship && k.SourceId != null)
        .Select(k => new { k.Id, k.SourceId, Text = (k.TitleEn ?? "") + " " + (k.ContentEn ?? "") })
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

    queries = queries.OrderBy(_ => rng.Next()).Take(studentCount).ToList();
    Console.WriteLine($"queries    : {queries.Count} listings whose field/level/country/funding combination is unique");
    Console.WriteLine();

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
        ("local-hash-v1 (shipped default)", local),
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

    foreach (var (label, svc) in providers)
    {
        var docVectors = new List<float[]>(docs.Count);
        for (var i = 0; i < docs.Count; i += 64)
        {
            var slice = docs.Skip(i).Take(64).Select(d => d.Text).ToList();
            docVectors.AddRange(await svc.EmbedBatchAsync(slice, CancellationToken.None).ConfigureAwait(false));
            if (i % 320 == 0) Console.WriteLine($"  {label}: embedded {Math.Min(i + 64, docs.Count)}/{docs.Count} documents");
        }

        var queryVectors = new List<float[]>(queries.Count);
        for (var i = 0; i < queries.Count; i += 64)
        {
            var slice = queries.Skip(i).Take(64).Select(q => q.Query).ToList();
            queryVectors.AddRange(await svc.EmbedBatchAsync(slice, CancellationToken.None).ConfigureAwait(false));
        }

        var index = docs.Select((d, i) => (d.Id, i)).ToDictionary(x => x.Id, x => x.i);
        int hits = 0; double mrrSum = 0, rankSum = 0;
        for (var qi = 0; qi < queries.Count; qi++)
        {
            var qv = queryVectors[qi];
            var target = index[queries[qi].DocId];
            var scored = new List<(int Idx, double Score)>(docs.Count);
            for (var di = 0; di < docs.Count; di++) scored.Add((di, Cosine(qv, docVectors[di])));
            scored.Sort((a, b) => b.Score.CompareTo(a.Score));
            var rank = scored.FindIndex(x => x.Idx == target) + 1;
            rankSum += rank;
            if (rank <= TopK) hits++;
            if (rank > 0) mrrSum += 1.0 / rank;
        }

        Console.WriteLine($"{label,-34}{hits * 100.0 / queries.Count,11:F1}%{mrrSum / queries.Count,10:F3}{rankSum / queries.Count,12:F1}");
    }

    Console.WriteLine();
    Console.WriteLine($"  recall@{TopK} is the share of queries whose one correct document appeared in the");
    Console.WriteLine($"  top {TopK} the retriever actually returns; mean rank is out of {docs.Count}.");
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
