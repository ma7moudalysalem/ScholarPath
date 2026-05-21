using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Deterministic, zero-network AI provider. Scores open scholarships against a
/// student's profile (field / country / level overlap) and drops eligibility
/// criteria out of the listing text so the UI has real data to render even
/// with the "Stub" provider selected.
///
/// The shape matches a real AI provider: inputs, outputs, token counts, cost.
/// Tokens/cost are synthetic (deterministic) so tests and dashboards still work.
/// </summary>
public sealed class LocalAiService(
    ApplicationDbContext db,
    IKnowledgeRetriever retriever,
    IOptions<AiOptions> opts) : IAiService
{
    private const string Disclaimer = "AI-generated guidance. Verify with official sources before acting.";

    // Synthetic cost model so cost caps behave reasonably in stub mode.
    private const decimal FakeCostPerRecommendation = 0.0008m;
    private const decimal FakeCostPerEligibility = 0.0005m;
    private const decimal FakeCostPerChatTurn = 0.0003m;

    public async Task<AiRecommendationResult> GenerateRecommendationsAsync(
        Guid userId, int topN, CancellationToken ct)
    {
        topN = Math.Clamp(topN, 1, 20);

        var profile = await db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            .ConfigureAwait(false);

        var preferredFields = ParseJsonArray(profile?.PreferredFieldsJson);
        var preferredCountries = ParseJsonArray(profile?.PreferredCountriesJson);
        var userLevel = profile?.AcademicLevel;

        var candidates = await db.Scholarships
            .AsNoTracking()
            .Where(s => s.Status == ScholarshipStatus.Open && s.Deadline > DateTimeOffset.UtcNow)
            .Select(s => new
            {
                s.Id,
                s.TitleEn,
                s.TitleAr,
                s.TargetLevel,
                s.TargetCountriesJson,
                s.FieldsOfStudyJson,
                s.FundingAmountUsd,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var scored = new List<(Guid Id, int Score, string TitleEn, string TitleAr)>(candidates.Count);

        foreach (var c in candidates)
        {
            var fosFields = ParseJsonArray(c.FieldsOfStudyJson);
            var countries = ParseJsonArray(c.TargetCountriesJson);

            var score = 0;

            if (userLevel.HasValue && userLevel.Value == c.TargetLevel) score += 30;
            score += OverlapScore(preferredFields, fosFields, maxPoints: 40);
            score += OverlapScore(preferredCountries, countries, maxPoints: 25);

            // Funding bonus — bigger awards nudge up by a few points
            if (c.FundingAmountUsd >= 10_000) score += 5;

            scored.Add((c.Id, Math.Min(score, 100), c.TitleEn, c.TitleAr));
        }

        var top = scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.TitleEn)
            .Take(topN)
            .Select(x => new AiRecommendationItem(
                ScholarshipId: x.Id,
                MatchScore: x.Score,
                ExplanationEn: BuildExplanationEn(x.Score, x.TitleEn),
                ExplanationAr: BuildExplanationAr(x.Score, x.TitleAr)))
            .ToList();

        // Synthetic token accounting — 40 prompt + 12 per recommendation
        var promptTokens = 40;
        var completionTokens = 12 * top.Count;

        return new AiRecommendationResult(top, Disclaimer, promptTokens, completionTokens);
    }

    public async Task<AiEligibilityResult> CheckEligibilityAsync(
        Guid userId, Guid scholarshipId, CancellationToken ct)
    {
        var scholarship = await db.Scholarships
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scholarshipId, ct)
            .ConfigureAwait(false);

        if (scholarship is null)
        {
            return new AiEligibilityResult(
                Array.Empty<AiEligibilityCriterion>(),
                "Scholarship not found.",
                "المنحة غير موجودة.",
                Disclaimer,
                EligibilityVerdict.NotEligible);
        }

        var profile = await db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            .ConfigureAwait(false);

        var criteria = new List<AiEligibilityCriterion>();

        // Academic level
        criteria.Add(new AiEligibilityCriterion(
            NameEn: "Academic level",
            NameAr: "المستوى الأكاديمي",
            StudentValue: profile?.AcademicLevel?.ToString() ?? "unknown",
            ListingRequirement: scholarship.TargetLevel.ToString(),
            Match: profile?.AcademicLevel == scholarship.TargetLevel ? "yes"
                : profile?.AcademicLevel is null ? "unknown" : "no"));

        // Country match (nullable)
        var listingCountries = ParseJsonArray(scholarship.TargetCountriesJson);
        var studentCountry = profile?.Nationality ?? string.Empty;
        criteria.Add(new AiEligibilityCriterion(
            NameEn: "Country",
            NameAr: "الدولة",
            StudentValue: string.IsNullOrWhiteSpace(studentCountry) ? "unknown" : studentCountry,
            ListingRequirement: listingCountries.Count == 0 ? "any" : string.Join(", ", listingCountries),
            Match: listingCountries.Count == 0 ? "yes"
                : string.IsNullOrWhiteSpace(studentCountry) ? "unknown"
                : listingCountries.Any(c => string.Equals(c, studentCountry, StringComparison.OrdinalIgnoreCase)) ? "yes" : "no"));

        // Field of study — use dedicated FieldsOfStudyJson; fall back to tags only if not set
        var fosFields = ParseJsonArray(scholarship.FieldsOfStudyJson);
        var fos = profile?.FieldOfStudy ?? string.Empty;
        criteria.Add(new AiEligibilityCriterion(
            NameEn: "Field of study",
            NameAr: "مجال الدراسة",
            StudentValue: string.IsNullOrWhiteSpace(fos) ? "unknown" : fos,
            ListingRequirement: fosFields.Count == 0 ? "any" : string.Join(", ", fosFields),
            Match: fosFields.Count == 0 ? "yes"
                : string.IsNullOrWhiteSpace(fos) ? "unknown"
                : fosFields.Any(f => f.Contains(fos, StringComparison.OrdinalIgnoreCase)
                                  || fos.Contains(f, StringComparison.OrdinalIgnoreCase)) ? "yes" : "partial"));

        var matches = criteria.Count(c => c.Match == "yes");
        var verdict = DeriveVerdict(criteria);

        // When every criterion is "unknown" the student's profile holds no
        // usable data — any verdict would be misleading, so the summary says
        // so plainly and points the student at completing their profile.
        var profileIncomplete = criteria.Count > 0
            && criteria.All(c => string.Equals(c.Match, "unknown", StringComparison.OrdinalIgnoreCase));

        var summaryEn = profileIncomplete
            ? "We couldn't assess your eligibility — your profile has no academic level, nationality, or field of study yet. Complete your profile, then run the check again."
            : verdict switch
            {
                EligibilityVerdict.Eligible =>
                    "You appear to meet all listed criteria.",
                EligibilityVerdict.NotEligible =>
                    $"You match {matches} of {criteria.Count} criteria, but one or more requirements are not met. Review the items marked 'no' before applying.",
                _ =>
                    $"You match {matches} of {criteria.Count} criteria. Review the partial items before applying.",
            };

        var summaryAr = profileIncomplete
            ? "تعذّر تقييم أهليتك — ملفك لا يحتوي بعد على المستوى الأكاديمي أو الجنسية أو مجال الدراسة. أكمل ملفك ثم أعد الفحص."
            : verdict switch
            {
                EligibilityVerdict.Eligible =>
                    "يبدو أنك تستوفي جميع المعايير المدرجة.",
                EligibilityVerdict.NotEligible =>
                    $"تطابقت مع {matches} من {criteria.Count} معايير، لكن واحدًا أو أكثر من المتطلبات غير مستوفى. راجع البنود غير المطابقة قبل التقديم.",
                _ =>
                    $"تطابقت مع {matches} من {criteria.Count} معايير. راجع البنود الجزئية قبل التقديم.",
            };

        return new AiEligibilityResult(criteria, summaryEn, summaryAr, Disclaimer, verdict);
    }

    /// <summary>
    /// SRS FR-117 — collapse the per-criterion verdicts into the mandated
    /// overall classification. Any outright "no" makes the student
    /// NotEligible; otherwise a "partial" (or an "unknown") leaves them
    /// PartiallyEligible; all-clear is Eligible.
    /// </summary>
    internal static EligibilityVerdict DeriveVerdict(IReadOnlyList<AiEligibilityCriterion> criteria)
    {
        if (criteria.Count == 0) return EligibilityVerdict.NotEligible;

        if (criteria.Any(c => string.Equals(c.Match, "no", StringComparison.OrdinalIgnoreCase)))
            return EligibilityVerdict.NotEligible;

        if (criteria.Any(c =>
                string.Equals(c.Match, "partial", StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.Match, "unknown", StringComparison.OrdinalIgnoreCase)))
            return EligibilityVerdict.PartiallyEligible;

        return EligibilityVerdict.Eligible;
    }

    /// <summary>
    /// Retrieval-Augmented chat — the offline path. It runs the same RAG
    /// retrieval as the cloud providers, then answers <em>extractively</em>:
    /// it surfaces the most relevant indexed document rather than generating
    /// free text. When nothing relevant is found it falls back to the keyword
    /// router. This keeps RAG demonstrable with no API key configured.
    /// </summary>
    public async Task<AiChatResponse> AskAsync(
        Guid userId,
        string sessionId,
        string message,
        IReadOnlyList<AiChatHistoryTurn> history,
        CancellationToken ct)
    {
        // The local router doesn't call an LLM, so it has nothing to do with
        // the supplied history — silence the unused-param warning explicitly
        // so the signature stays consistent with the real providers.
        _ = history;

        var msg = (message ?? string.Empty).Trim();
        var arabic = RagSupport.IsArabic(msg);

        var docs = await retriever.RetrieveAsync(msg, opts.Value.RagTopK, ct).ConfigureAwait(false);
        var relevant = docs.Where(d => d.Score >= opts.Value.RagMinScore).ToList();

        string reply;
        if (relevant.Count > 0)
        {
            var top = relevant[0];
            var body = (arabic ? top.ContentAr : top.ContentEn).Trim();
            var lead = arabic
                ? "وجدت هذا في قاعدة معرفة ScholarPath:"
                : "Here is what I found in ScholarPath's knowledge base:";
            reply = $"{lead}\n\n{body}";
        }
        else
        {
            reply = RouteChat(msg);
        }

        var promptTokens = Math.Max(1, msg.Length / 4);
        var completionTokens = Math.Max(1, reply.Length / 4);

        return new AiChatResponse(
            Message: reply,
            Disclaimer: Disclaimer,
            PromptTokens: promptTokens,
            CompletionTokens: completionTokens,
            EstimatedCostUsd: FakeCostPerChatTurn,
            Sources: RagSupport.ToSources(relevant, arabic));
    }

    // ─── helpers ──────────────────────────────────────────────────────────

    internal static decimal CostForRecommendations(int itemCount) =>
        FakeCostPerRecommendation * Math.Max(itemCount, 1);

    internal static decimal CostForEligibility() => FakeCostPerEligibility;

    internal static decimal CostForChat() => FakeCostPerChatTurn;

    private static IReadOnlyList<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            var arr = JsonSerializer.Deserialize<string[]>(json);
            return arr ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static int OverlapScore(IReadOnlyList<string> a, IReadOnlyList<string> b, int maxPoints)
    {
        if (a.Count == 0 || b.Count == 0) return 0;
        var set = new HashSet<string>(a, StringComparer.OrdinalIgnoreCase);
        var hits = b.Count(x => set.Contains(x));
        if (hits == 0) return 0;
        return Math.Min(maxPoints, hits * (maxPoints / Math.Max(a.Count, 1)));
    }

    private static string BuildExplanationEn(int score, string title) => score switch
    {
        >= 80 => $"Strong match for your profile — '{title}' aligns with your preferred fields and level.",
        >= 50 => $"Partial match — '{title}' overlaps with some of your preferences.",
        >= 20 => $"Loose match — '{title}' touches areas you've listed; worth a quick look.",
        _     => $"Weak match — '{title}' doesn't align well with your current profile.",
    };

    private static string BuildExplanationAr(int score, string title) => score switch
    {
        >= 80 => $"توافق قوي مع ملفك الشخصي — '{title}' قريبة من اهتماماتك ومستواك الأكاديمي.",
        >= 50 => $"توافق جزئي — '{title}' تتقاطع مع بعض تفضيلاتك.",
        >= 20 => $"توافق محدود — '{title}' تلمس المجالات التي أضفتها، يستحق نظرة سريعة.",
        _     => $"توافق ضعيف — '{title}' لا تتماشى حالياً مع ملفك.",
    };

    private static string RouteChat(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Tell me what you're looking for — scholarship suggestions, deadlines, or application tips.";

        static bool ContainsCi(string haystack, string needle) =>
            haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

        var arabic = RagSupport.IsArabic(message);

        // ── Platform-specific intents ────────────────────────────────────
        if (ContainsCi(message, "deadline") || ContainsCi(message, "موعد"))
            return arabic
                ? "تظهر المواعيد على صفحة كل منحة. افتح تبويب \"المنح\" ثم رتّب حسب \"الموعد النهائي\" (الأقرب أولاً)."
                : "Deadlines are shown on each scholarship page. Open the Scholarships tab, then sort by Deadline (nearest first).";

        if (ContainsCi(message, "eligib") || ContainsCi(message, "هل انا مؤهل") || ContainsCi(message, "أهلية"))
            return arabic
                ? "افتح أي منحة واضغط \"افحص الأهلية\" — سنقارن ملفك بالمعايير المُدرجة."
                : "Open any scholarship detail and tap \"Check eligibility\" — we'll compare your profile against the listed criteria.";

        if (ContainsCi(message, "recommend") || ContainsCi(message, "ترشح") || ContainsCi(message, "اقتراح"))
            return arabic
                ? "أداة \"الاقتراحات\" في لوحتك ترتب المنح المفتوحة حسب التوافق. حدّث المجالات المفضلة في ملفك لتحسين الترتيب."
                : "The Recommendations widget on your dashboard ranks open scholarships by fit. Update your profile's preferred fields to sharpen the list.";

        if (ContainsCi(message, "apply") || ContainsCi(message, "تقديم"))
            return arabic
                ? "الطلبات في تبويب \"طلباتي\". يمكنك الاحتفاظ بطلب واحد نشط لكل منحة في كل وقت."
                : "Applications live under the Applications tab. You can only keep one active application per scholarship at a time.";

        if (ContainsCi(message, "consult") || ContainsCi(message, "مستشار"))
            return arabic
                ? "المستشارون في تبويب \"المستشارون\". يمكنك حجز جلسة مدفوعة؛ يبقى المبلغ في الضمان حتى تكتمل الجلسة."
                : "Consultants are under the Consultants tab. You can book a paid session; funds are held in escrow until the session completes.";

        // ── General educational guidance ────────────────────────────────
        // These topics are not tied to platform data, so we answer with concise
        // generic guidance to keep the assistant useful even when RAG misses.
        if (ContainsCi(message, "recommendation letter") || ContainsCi(message, "letter of recommendation")
            || ContainsCi(message, "خطاب توصية") || ContainsCi(message, "توصية"))
            return arabic
                ? "خطاب التوصية عادةً يحتوي: (1) كيف يعرفك الكاتب ومنذ متى، (2) إنجازاتك الأكاديمية ومهاراتك بأمثلة محددة، (3) صفاتك الشخصية، (4) لماذا أنت مرشح قوي لهذا البرنامج. اطلب الخطاب من أستاذ يعرف عملك جيدًا قبل ٤–٦ أسابيع من الموعد، وزوّده بسيرتك الذاتية وخطاب الغرض."
                : "A strong recommendation letter usually includes: (1) how the writer knows you and for how long, (2) your academic achievements and skills with specific examples, (3) your character traits, (4) why you're a strong fit for the program. Ask a professor who knows your work well 4–6 weeks before the deadline, and share your CV plus your Statement of Purpose with them.";

        if (ContainsCi(message, "statement of purpose") || ContainsCi(message, "sop") || ContainsCi(message, "personal statement")
            || ContainsCi(message, "خطاب الغرض") || ContainsCi(message, "بيان شخصي"))
            return arabic
                ? "هيكل خطاب الغرض: (1) فقرة افتتاحية تذكر هدفك ولماذا الآن، (2) خلفيتك الأكاديمية وخبراتك ذات الصلة، (3) سبب اختيارك لهذا البرنامج/الجامعة تحديدًا، (4) خططك بعد التخرج وكيف تخدم مجتمعك. اجعله ٥٠٠–٨٠٠ كلمة، صادقًا ومحددًا، وتجنب الكليشيهات."
                : "Statement of Purpose structure: (1) opening that states your goal and why now, (2) academic background + relevant experience, (3) why this specific program/university, (4) post-graduation plans and how you'll contribute. Keep it 500–800 words, be authentic and specific, and avoid clichés. Tailor every paragraph to the program — generic SoPs are easy to spot.";

        if (ContainsCi(message, "interview") || ContainsCi(message, "مقابلة"))
            return arabic
                ? "نصائح لمقابلة المنحة: (1) راجع موقع المنحة وقيمها الأساسية، (2) جهّز إجابات لأسئلة \"لماذا هذا البرنامج\" و\"خططك المستقبلية\" و\"تحدٍ واجهته\"، (3) جهّز سؤالين أو ثلاثة لتسألهم في النهاية، (4) ارتدِ ملابس رسمية حتى لو كانت عبر الإنترنت، (5) تدرّب بصوت عالٍ ولكن لا تحفظ حرفيًا."
                : "Scholarship interview tips: (1) review the scholarship website and core values, (2) prepare answers for \"why this program\", \"future plans\", and \"a challenge you faced\", (3) have 2–3 questions ready to ask them, (4) dress formally even on video calls, (5) practice out loud but don't memorise — sound conversational.";

        if (ContainsCi(message, "ielts") || ContainsCi(message, "toefl") || ContainsCi(message, "gre") || ContainsCi(message, "gmat")
            || ContainsCi(message, "آيلتس") || ContainsCi(message, "توفل"))
            return arabic
                ? "اختبارات اللغة الشائعة: IELTS (٠–٩) وTOEFL iBT (٠–١٢٠). معظم المنح تطلب IELTS ≥ ٦.٥ أو TOEFL ≥ ٨٠. خصّص ٦–٨ أسابيع للتحضير: تدرّب على الأقسام الأربعة، خذ اختبارًا تجريبيًا أسبوعيًا، وركّز على نقاط ضعفك. تأكد من صلاحية درجتك (سنتان عادةً)."
                : "Common language tests: IELTS (0–9 scale) and TOEFL iBT (0–120). Most scholarships ask for IELTS ≥ 6.5 or TOEFL ≥ 80. Plan 6–8 weeks of prep: practice all four sections, take one full mock test per week, and focus on your weakest skill. Make sure your score is still valid (usually 2 years) at the application deadline.";

        if (ContainsCi(message, "essay") || ContainsCi(message, "writing") || ContainsCi(message, "مقال") || ContainsCi(message, "كتاب"))
            return arabic
                ? "لتحسين مقالك: (1) ابدأ بافتتاحية محددة (مشهد أو لحظة) بدلًا من العبارات العامة، (2) أظهر ولا تقل — استعمل أمثلة ملموسة، (3) اربط كل فقرة بهدفك من المنحة، (4) راجع بصوت عالٍ لاكتشاف الجمل الركيكة، (5) اطلب مراجعة من شخصين على الأقل قبل التسليم."
                : "To improve your essay: (1) open with a specific scene or moment, not a generic statement, (2) show, don't tell — use concrete examples, (3) connect every paragraph back to your scholarship goal, (4) read it aloud to catch awkward sentences, (5) get feedback from at least two readers before submitting.";

        if (ContainsCi(message, "document") || ContainsCi(message, "أوراق") || ContainsCi(message, "مستندات"))
            return arabic
                ? "الأوراق الشائعة لمعظم طلبات المنح: (1) شهادات أكاديمية وكشوف درجات، (2) سيرة ذاتية، (3) خطاب غرض / بيان شخصي، (4) خطابي توصية على الأقل، (5) شهادة لغة (IELTS/TOEFL)، (6) جواز سفر ساري، (7) إثبات هوية. راجع متطلبات المنحة المحددة — تتفاوت."
                : "Common documents for most scholarship applications: (1) academic certificates + transcripts, (2) CV/résumé, (3) Statement of Purpose / personal statement, (4) at least 2 recommendation letters, (5) language test (IELTS/TOEFL), (6) valid passport, (7) photo ID. Always check the specific scholarship's checklist — requirements vary.";

        if (ContainsCi(message, "fully funded") || ContainsCi(message, "fully-funded") || ContainsCi(message, "partial")
            || ContainsCi(message, "كاملة") || ContainsCi(message, "جزئية"))
            return arabic
                ? "المنح الكاملة (Fully Funded) تغطي عادةً: الرسوم الدراسية، السكن، السفر، التأمين الصحي، ومصروف شهري. المنح الجزئية تغطي جزءًا فقط (الرسوم فقط، أو نسبة منها). الكاملة أكثر تنافسية لكنها تزيل العبء المالي تمامًا — قدّم لكليهما إن كان ملفك يسمح."
                : "Fully funded scholarships typically cover: tuition, accommodation, travel, health insurance, and a monthly stipend. Partial scholarships only cover part of the cost (tuition only, or a percentage). Fully funded ones are more competitive but remove the financial burden entirely — apply to both tiers if your profile allows.";

        return arabic
            ? "يمكنني المساعدة في المنح، الأهلية، المواعيد، والطلبات — وأيضًا في كتابة خطابات التوصية، خطاب الغرض، التحضير للمقابلات، واختبارات اللغة. اسأل بشكل محدد وسأرشدك للمكان المناسب."
            : "I can help with scholarships, eligibility, deadlines, and applications — and also with writing recommendation letters, structuring a Statement of Purpose, interview prep, and language tests. Ask me something specific and I'll point you to the right place.";
    }
}
