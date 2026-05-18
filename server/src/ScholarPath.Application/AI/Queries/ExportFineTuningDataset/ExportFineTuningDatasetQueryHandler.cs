using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Ai.Queries.ExportFineTuningDataset;

/// <summary>
/// Generates supervised fine-tuning examples from ScholarPath's own data so a
/// fine-tuned chat model speaks the platform's domain and house voice. FAQ
/// entries become direct question/answer pairs; each scholarship yields a few
/// grounded question/answer pairs (overview, eligibility) in English and Arabic.
/// </summary>
public sealed class ExportFineTuningDatasetQueryHandler(
    IApplicationDbContext db,
    IDatasetProvider datasets)
    : IRequestHandler<ExportFineTuningDatasetQuery, FineTuningDatasetDto>
{
    private const int MaxScholarships = 100;

    private const string SystemEn =
        "You are ScholarPath's assistant. You help students discover scholarships, "
        + "check eligibility, understand deadlines, and use the ScholarPath platform. "
        + "Answer concisely and accurately, and note that AI guidance is advisory — "
        + "users should verify details with the official listing.";

    private const string SystemAr =
        "أنت مساعد ScholarPath. تساعد الطلاب في اكتشاف المنح الدراسية وفحص الأهلية "
        + "وفهم المواعيد النهائية واستخدام منصة ScholarPath. أجب بإيجاز ودقة، ونبّه إلى "
        + "أن إرشاد الذكاء الاصطناعي استرشادي وعلى المستخدم التحقق من القائمة الرسمية.";

    private static readonly JsonSerializerOptions JsonIn = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions JsonOut = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public async Task<FineTuningDatasetDto> Handle(
        ExportFineTuningDatasetQuery request, CancellationToken ct)
    {
        var examples = new List<FtExample>();

        examples.AddRange(BuildFaqExamples());
        examples.AddRange(await BuildScholarshipExamplesAsync(ct).ConfigureAwait(false));

        var sb = new StringBuilder();
        foreach (var example in examples)
            sb.Append(JsonSerializer.Serialize(example, JsonOut)).Append('\n');

        return new FineTuningDatasetDto(
            FileName: "scholarpath-finetune.jsonl",
            ExampleCount: examples.Count,
            Jsonl: sb.ToString(),
            GeneratedAt: DateTimeOffset.UtcNow);
    }

    private List<FtExample> BuildFaqExamples()
    {
        var json = datasets.GetDatasetJson("scholarpath-faq");
        if (string.IsNullOrWhiteSpace(json)) return [];

        FaqDataset? dataset;
        try { dataset = JsonSerializer.Deserialize<FaqDataset>(json, JsonIn); }
        catch (JsonException) { return []; }

        var examples = new List<FtExample>();
        foreach (var f in dataset?.Faqs ?? [])
        {
            if (!string.IsNullOrWhiteSpace(f.QuestionEn) && !string.IsNullOrWhiteSpace(f.AnswerEn))
                examples.Add(Example(SystemEn, f.QuestionEn, f.AnswerEn));
            if (!string.IsNullOrWhiteSpace(f.QuestionAr) && !string.IsNullOrWhiteSpace(f.AnswerAr))
                examples.Add(Example(SystemAr, f.QuestionAr, f.AnswerAr));
        }
        return examples;
    }

    private async Task<List<FtExample>> BuildScholarshipExamplesAsync(CancellationToken ct)
    {
        var scholarships = await db.Scholarships
            .AsNoTracking()
            .Where(s => s.Status == ScholarshipStatus.Open)
            .OrderBy(s => s.TitleEn)
            .Take(MaxScholarships)
            .ToListAsync(ct).ConfigureAwait(false);

        var examples = new List<FtExample>(scholarships.Count * 3);
        foreach (var s in scholarships)
        {
            var amount = s.FundingAmountUsd is > 0 ? $" worth about {s.FundingAmountUsd:N0} USD" : string.Empty;

            // Overview — English.
            examples.Add(Example(
                SystemEn,
                $"Tell me about the {s.TitleEn} scholarship.",
                $"{s.DescriptionEn} It is a {s.FundingType} award{amount} for {s.TargetLevel} study, "
                + $"with an application deadline of {s.Deadline:MMMM d, yyyy}. "
                + "This is advisory — confirm the details on the official listing."));

            // Overview — Arabic.
            examples.Add(Example(
                SystemAr,
                $"حدثني عن منحة {s.TitleAr}.",
                $"{s.DescriptionAr} نوع التمويل {s.FundingType} وهي لمستوى {s.TargetLevel}، "
                + $"والموعد النهائي للتقديم {s.Deadline:yyyy-MM-dd}. "
                + "هذا إرشاد استرشادي — تأكد من التفاصيل من القائمة الرسمية."));

            // Eligibility — English (only when the listing states criteria).
            if (!string.IsNullOrWhiteSpace(s.EligibilityRequirementsEn))
            {
                examples.Add(Example(
                    SystemEn,
                    $"What are the eligibility requirements for {s.TitleEn}?",
                    $"Eligibility for {s.TitleEn}: {s.EligibilityRequirementsEn} "
                    + "Always confirm the exact criteria on the official listing before applying."));
            }
        }
        return examples;
    }

    private static FtExample Example(string system, string user, string assistant)
        => new([
            new FtMessage("system", system),
            new FtMessage("user", user),
            new FtMessage("assistant", assistant),
        ]);

    private sealed record FtExample(
        [property: JsonPropertyName("messages")] List<FtMessage> Messages);

    private sealed record FtMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record FaqDataset(List<FaqEntry>? Faqs);

    private sealed record FaqEntry(
        string Key,
        string QuestionEn,
        string QuestionAr,
        string AnswerEn,
        string AnswerAr);
}
