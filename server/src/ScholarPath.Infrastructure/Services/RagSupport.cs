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
        "You are an AI assistant for ScholarPath, a scholarship discovery platform.\n\n"
        + "1. When the user asks about a specific scholarship, consultant, resource, or "
        + "platform feature: ANSWER STRICTLY from the provided CONTEXT (RAG retrieved "
        + "chunks). If the context does not cover it, say so briefly.\n"
        + "2. When the user asks general educational questions (recommendation letters, "
        + "Statement of Purpose structure, IELTS/TOEFL prep, interview tips, study-abroad "
        + "advice, essay help): provide helpful general guidance based on your knowledge. "
        + "You can use templates and concrete examples.\n"
        + "3. ALWAYS respond in the user's language. If they wrote in Arabic, respond in "
        + "Arabic; if in English, respond in English.\n"
        + "4. Be concise (max ~300 words) but actionable.\n"
        + "5. When relevant, suggest exploring platform features (browse scholarships, "
        + "book a consultant, read resources). Never repeat personal identifiers.";

    public const string ChatSystemAr =
        "أنت مساعد ذكاء اصطناعي لمنصة ScholarPath لاكتشاف المنح الدراسية.\n\n"
        + "1. عندما يسأل المستخدم عن منحة محددة أو مستشار أو مورد تعليمي أو ميزة في "
        + "المنصة: أجب حصريًا من السياق (CONTEXT) المُسترجَع أدناه. إن لم يكن السياق "
        + "كافيًا فاذكر ذلك باختصار.\n"
        + "2. عندما يسأل عن أسئلة تعليمية عامة (خطابات التوصية، بنية خطاب الغرض SoP، "
        + "التحضير للآيلتس/التوفل، نصائح المقابلات، الدراسة بالخارج، تحسين المقالات): "
        + "قدّم إرشادًا عامًا مفيدًا من معرفتك، ويمكنك استخدام قوالب وأمثلة عملية.\n"
        + "3. أجب دائمًا بلغة المستخدم: إن كتب بالعربية فأجب بالعربية، وإن كتب "
        + "بالإنجليزية فأجب بالإنجليزية.\n"
        + "4. كن موجزًا (بحد أقصى ~300 كلمة) وعمليًا.\n"
        + "5. عند الملاءمة اقترح استكشاف ميزات المنصة (تصفح المنح، حجز مستشار، قراءة "
        + "الموارد). لا تكرر أي معرّفات شخصية.";

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
