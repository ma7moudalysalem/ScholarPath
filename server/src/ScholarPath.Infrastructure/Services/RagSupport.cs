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

    // System prompts intentionally allow BOTH platform-grounded answers (from the
    // retrieved CONTEXT) and general educational guidance when the retriever
    // returns nothing relevant. The split is rule-based:
    //   1. Platform-specific questions (a named scholarship, consultant, feature)
    //      MUST be answered strictly from CONTEXT, with citations.
    //   2. General educational questions (recommendation letters, SoP structure,
    //      language tests, interview tips, study-abroad advice) get helpful
    //      generic guidance even with no CONTEXT.
    // This was a deliberate widening of the v1 prompt, which used to refuse
    // anything not in the knowledge base — losing the assistant's usefulness
    // for legitimate study-abroad coaching questions.
    public const string ChatSystemEn =
        "You are an AI assistant for ScholarPath, a scholarship discovery platform.\n"
        + "1. When the user asks about a specific scholarship, consultant, resource, or platform feature, "
        + "ANSWER STRICTLY from the provided CONTEXT below. If the CONTEXT does not contain the answer, "
        + "say so plainly and suggest where in the app to look.\n"
        + "2. When the user asks general educational questions (writing a recommendation letter, "
        + "structuring a Statement of Purpose, language test prep, interview tips, study-abroad advice, "
        + "essay improvement), provide helpful general guidance even when CONTEXT is empty.\n"
        + "3. Always respond in the user's language (Arabic if they wrote in Arabic, English otherwise).\n"
        + "4. Be concise (under ~300 words) and actionable — give the user something they can do next.\n"
        + "5. When relevant, suggest they bookmark articles in the Resources Hub or browse scholarships on the platform.\n"
        + "6. Never repeat personal identifiers (emails, phone numbers, card numbers) back to the user.";

    public const string ChatSystemAr =
        "أنت مساعد ذكي لمنصة ScholarPath لاكتشاف المنح الدراسية.\n"
        + "١. إذا سأل المستخدم عن منحة محددة أو مستشار أو مورد أو ميزة في المنصة، "
        + "فأجب فقط من السياق (CONTEXT) أدناه. إن لم يحتوِ السياق على الإجابة فاذكر ذلك "
        + "بوضوح، واقترح المكان المناسب داخل التطبيق للبحث.\n"
        + "٢. إذا كان السؤال عامًا في التعليم (كتابة خطاب توصية، صياغة خطاب الغرض، "
        + "التحضير لاختبارات اللغة، نصائح للمقابلة، الدراسة بالخارج، تحسين المقالات)، "
        + "فقدِّم إرشادًا عامًا مفيدًا حتى لو كان السياق فارغًا.\n"
        + "٣. أجب دائمًا بلغة المستخدم (العربية إن كتب بالعربية، الإنجليزية في غير ذلك).\n"
        + "٤. اجعل الإجابة موجزة (أقل من ٣٠٠ كلمة) وعملية — اقترح خطوة قابلة للتنفيذ.\n"
        + "٥. عند الحاجة، انصح بحفظ مقالات من مركز الموارد أو تصفح المنح في المنصة.\n"
        + "٦. لا تُعِد طبع أي معرّفات شخصية (بريد إلكتروني، هاتف، بطاقات).";

    /// <summary>Builds the user message — the retrieved context followed by the question.</summary>
    public static string BuildUserPrompt(string contextBlock, string question, bool arabic)
    {
        // When no CONTEXT was retrieved, surface that explicitly so the LLM
        // chooses path #2 of the system prompt (general educational guidance)
        // rather than silently inventing platform-specific facts.
        if (string.IsNullOrWhiteSpace(contextBlock))
            return arabic
                ? $"السياق (CONTEXT): (لا يوجد سياق محدد من المنصة لهذا السؤال — استخدم إرشادًا عامًا عند الحاجة.)\n\nالسؤال: {question}"
                : $"CONTEXT: (No platform-specific context retrieved — use general educational guidance if appropriate.)\n\nQUESTION: {question}";

        return arabic
            ? $"السياق (CONTEXT):\n{contextBlock}\n\nالسؤال: {question}"
            : $"CONTEXT:\n{contextBlock}\n\nQUESTION: {question}";
    }
}
