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
        "You are ScholarPath's help assistant. Answer the user's question using the "
        + "CONTEXT below, which is retrieved from ScholarPath's knowledge base of "
        + "scholarships and help articles. Rely on the context; if it does not cover "
        + "the question, say so briefly and give general guidance. Keep answers under "
        + "700 characters. Stay on scholarships, eligibility, deadlines, applications, "
        + "consultants, and using ScholarPath. Never repeat personal identifiers.";

    public const string ChatSystemAr =
        "أنت مساعد ScholarPath. أجب عن سؤال المستخدم بالاعتماد على السياق (CONTEXT) "
        + "أدناه، وهو مُستَرجَع من قاعدة معرفة ScholarPath للمنح ومقالات المساعدة. "
        + "اعتمد على السياق؛ وإن لم يكن كافيًا فاذكر ذلك باختصار وقدّم إرشادًا عامًا. "
        + "اجعل الإجابة أقل من 700 حرف، وابقَ ضمن المنح والأهلية والمواعيد والطلبات "
        + "والمستشارين واستخدام ScholarPath. لا تكرر أي معرّفات شخصية.";

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
