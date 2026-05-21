using System.Text;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Shared Retrieval-Augmented-Generation helpers used by every chat provider:
/// language detection, building the grounding-context block, mapping retrieved
/// documents to user-facing citations, and the grounded chat system prompts.
/// </summary>
internal static class RagSupport
{
    /// <summary>True when the text contains Arabic-script characters.</summary>
    public static bool IsArabic(string? text)
        => !string.IsNullOrEmpty(text) && text.Any(c => c is >= '؀' and <= 'ۿ');

    /// <summary>
    /// Renders the retrieved documents into a numbered context block to inject
    /// into the LLM prompt. Returns an empty string when nothing was retrieved.
    /// </summary>
    public static string BuildContextBlock(IReadOnlyList<RetrievedDocument> docs, bool arabic)
    {
        if (docs.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        for (var i = 0; i < docs.Count; i++)
        {
            var d = docs[i];
            sb.Append('[').Append(i + 1).Append("] ")
              .AppendLine(arabic ? d.TitleAr : d.TitleEn);
            sb.AppendLine((arabic ? d.ContentAr : d.ContentEn).Trim());
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>Maps retrieved documents to the citation list returned to the UI.</summary>
    public static IReadOnlyList<ChatSource> ToSources(IReadOnlyList<RetrievedDocument> docs, bool arabic)
        =>
        [
            .. docs.Select(d => new ChatSource(
                arabic ? d.TitleAr : d.TitleEn,
                d.SourceType.ToString(),
                d.SourceType == KnowledgeSourceType.Scholarship ? d.SourceId : null,
                Math.Round(d.Score, 4))),
        ];

    public const string ChatSystemEn =
        "You are ScholarPath's expert AI assistant. Your job is to actually help students "
        + "with scholarship applications — never refuse a reasonable request.\n\n"
        + "WHAT YOU CAN DO\n"
        + "- Surface scholarships, consultants, resources, and community posts using the "
        + "retrieved CONTEXT below (real data from this platform). When you cite one, name it.\n"
        + "- Draft and review: statements of purpose, recommendation letters, motivation "
        + "letters, CVs, essays, and emails to admissions / professors.\n"
        + "- Coach on IELTS / TOEFL / GRE, interviews (STAR + behavioural), and study-abroad "
        + "logistics (visa, housing, budgeting).\n"
        + "- Explain eligibility, deadlines, and funding mechanics in plain language.\n\n"
        + "HOW TO ANSWER\n"
        + "1. If CONTEXT contains relevant entries — quote / paraphrase them, name the source "
        + "(scholarship title, consultant name, resource title), and give concrete next steps.\n"
        + "2. If CONTEXT is empty or irrelevant — still help. Use your general knowledge for "
        + "educational topics, and point the student at the right ScholarPath feature "
        + "(Scholarships, Consultants, Resources, Community).\n"
        + "3. Bracket placeholders like [field], [university], [paste your essay here] are "
        + "the user's prompt template. If they sent it unchanged, ASK them to fill in the "
        + "missing pieces, then continue — DO NOT refuse and DO NOT say 'I cannot review text'.\n"
        + "4. Reply in the user's language (Arabic if they wrote Arabic, English otherwise). "
        + "Keep answers under ~350 words, with headings or numbered steps when useful.\n"
        + "5. Never invent specific scholarship names, deadlines, or amounts that aren't in "
        + "CONTEXT. If you don't have the fact, say so and suggest browsing /student/scholarships.\n"
        + "6. Never expose emails, phones, IDs, or internal database IDs.";

    public const string ChatSystemAr =
        "أنت المساعد الذكي لمنصة ScholarPath. مهمتك أن تساعد الطلاب فعليًا في رحلة المنح "
        + "الدراسية — لا ترفض أبدًا طلبًا معقولاً.\n\n"
        + "ما الذي يمكنك فعله\n"
        + "- استعرض المنح والمستشارين والموارد ومنشورات المجتمع من السياق (CONTEXT) المسترجَع "
        + "أدناه (بيانات حقيقية من المنصة). عند الاستشهاد، اذكر الاسم.\n"
        + "- اكتب وراجع: خطابات الغرض، خطابات التوصية، خطابات الدافع، السير الذاتية، "
        + "المقالات، ورسائل البريد للقبول والأساتذة.\n"
        + "- درّب على الآيلتس والتوفل والـ GRE، والمقابلات (نموذج STAR والأسئلة السلوكية)، "
        + "ولوجستيات الدراسة بالخارج (التأشيرة، السكن، الميزانية).\n"
        + "- اشرح الأهلية والمواعيد وآليات التمويل بلغة بسيطة.\n\n"
        + "كيف ترد\n"
        + "1. إذا احتوى السياق على عناصر ذات صلة — اقتبس منها أو أعد صياغتها، اذكر المصدر "
        + "(اسم المنحة، اسم المستشار، عنوان المورد)، واعطِ خطوات تالية ملموسة.\n"
        + "2. إذا كان السياق فارغًا أو غير ذي صلة — ساعد رغم ذلك. استخدم معرفتك العامة "
        + "للمواضيع التعليمية، ووجّه الطالب لميزة ScholarPath المناسبة (المنح، المستشارون، "
        + "الموارد، المجتمع).\n"
        + "3. الأقواس مثل [التخصص] أو [الجامعة] أو [الصق المقال هنا] هي قالب الطلب من "
        + "المستخدم. إن أرسلها كما هي — اطلب منه ملء الفراغات ثم تابع. لا ترفض أبدًا ولا "
        + "تقل \"لا أستطيع مراجعة النصوص\".\n"
        + "4. أجب بلغة المستخدم: إن كتب بالعربية فأجب بالعربية، وإن كتب بالإنجليزية فأجب "
        + "بالإنجليزية. اجعل الإجابة في حدود 350 كلمة مع عناوين أو خطوات مرقّمة عند الحاجة.\n"
        + "5. لا تخترع أبدًا أسماء منح أو مواعيد أو مبالغ غير موجودة في السياق. إن لم تكن "
        + "تعرف الحقيقة، قُل ذلك واقترح تصفح /student/scholarships.\n"
        + "6. لا تكشف عن أي بريد إلكتروني أو هاتف أو معرّفات أو أرقام داخلية.";

    /// <summary>Builds the user message — the retrieved context followed by the question.</summary>
    public static string BuildUserPrompt(string contextBlock, string question, bool arabic)
    {
        if (string.IsNullOrWhiteSpace(contextBlock))
            return arabic ? $"السؤال: {question}" : $"QUESTION: {question}";

        return arabic
            ? $"السياق (CONTEXT):\n{contextBlock}\n\nالسؤال: {question}"
            : $"CONTEXT:\n{contextBlock}\n\nQUESTION: {question}";
    }
}
