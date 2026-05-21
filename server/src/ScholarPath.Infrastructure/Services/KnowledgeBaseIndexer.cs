using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Builds and maintains the RAG knowledge base. It projects every open
/// scholarship and every curated FAQ entry into a <see cref="KnowledgeDocument"/>,
/// then embeds the documents with the active <see cref="IEmbeddingService"/>.
///
/// A document is (re)embedded only when it is new, its content changed (tracked
/// by <see cref="KnowledgeDocument.ContentHash"/>), or it was last embedded with
/// a different model — so a rebuild is cheap once the base is warm.
/// </summary>
public sealed class KnowledgeBaseIndexer(
    ApplicationDbContext db,
    IEmbeddingService embeddings,
    IDatasetProvider datasets,
    ILogger<KnowledgeBaseIndexer> logger) : IKnowledgeBaseIndexer
{
    private const int EmbedBatchSize = 16;
    private const string FaqDatasetName = "scholarpath-faq";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<KnowledgeBaseRebuildResultDto> RebuildAsync(bool force, CancellationToken ct)
    {
        var desired = new List<DesiredDoc>();
        desired.AddRange(await BuildScholarshipDocsAsync(ct).ConfigureAwait(false));
        desired.AddRange(BuildFaqDocs());
        // Three additional projections so the RAG retriever can match people
        // and library content the way it already matches scholarships and
        // help articles.
        desired.AddRange(await BuildConsultantDocsAsync(ct).ConfigureAwait(false));
        desired.AddRange(await BuildResourceDocsAsync(ct).ConfigureAwait(false));
        desired.AddRange(await BuildTopCommunityPostDocsAsync(ct).ConfigureAwait(false));

        var existing = await db.KnowledgeDocuments.ToListAsync(ct).ConfigureAwait(false);
        var byKey = existing.ToDictionary(d => (d.SourceType, d.SourceKey));
        var desiredKeys = new HashSet<(KnowledgeSourceType, string)>();

        var now = DateTimeOffset.UtcNow;
        var upserted = 0;

        // ── 1. Upsert the desired documents ──
        foreach (var d in desired)
        {
            desiredKeys.Add((d.SourceType, d.SourceKey));
            var hash = Sha256(d.EmbedText);

            if (byKey.TryGetValue((d.SourceType, d.SourceKey), out var doc))
            {
                doc.SourceId = d.SourceId;
                doc.TitleEn = d.TitleEn;
                doc.TitleAr = d.TitleAr;
                doc.ContentEn = d.ContentEn;
                doc.ContentAr = d.ContentAr;
                doc.MetadataJson = d.MetadataJson;
                doc.UpdatedAt = now;

                // Content changed → drop the stale embedding so it is recomputed.
                if (doc.ContentHash != hash)
                {
                    doc.ContentHash = hash;
                    doc.Embedding = [];
                    doc.EmbeddingDimensions = 0;
                    doc.EmbeddingModel = null;
                }
            }
            else
            {
                doc = new KnowledgeDocument
                {
                    SourceType = d.SourceType,
                    SourceKey = d.SourceKey,
                    SourceId = d.SourceId,
                    TitleEn = d.TitleEn,
                    TitleAr = d.TitleAr,
                    ContentEn = d.ContentEn,
                    ContentAr = d.ContentAr,
                    MetadataJson = d.MetadataJson,
                    ContentHash = hash,
                    CreatedAt = now,
                };
                db.KnowledgeDocuments.Add(doc);
                existing.Add(doc);
                upserted++;
            }
        }

        // ── 2. Remove documents whose source no longer exists ──
        var orphans = existing
            .Where(d => !desiredKeys.Contains((d.SourceType, d.SourceKey)))
            .ToList();
        if (orphans.Count > 0)
        {
            db.KnowledgeDocuments.RemoveRange(orphans);
            foreach (var orphan in orphans) existing.Remove(orphan);
        }

        // ── 3. (Re)embed everything pending ──
        var model = embeddings.ModelName;
        var pending = existing
            .Where(d => force || d.Embedding.Length == 0 || d.EmbeddingModel != model)
            .ToList();

        var reembedded = 0;
        for (var i = 0; i < pending.Count; i += EmbedBatchSize)
        {
            ct.ThrowIfCancellationRequested();

            var batch = pending.Skip(i).Take(EmbedBatchSize).ToList();
            var texts = batch.Select(EmbedText).ToList();

            IReadOnlyList<float[]> vectors;
            try
            {
                vectors = await embeddings.EmbedBatchAsync(texts, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Embedding batch failed during the knowledge-base rebuild.");
                throw new ServiceUnavailableException(
                    $"The embedding provider '{model}' is unavailable or misconfigured, so the "
                    + "knowledge base could not be rebuilt. Check the Ai:Provider setting and the "
                    + "embedding endpoint/key, then retry.",
                    ex);
            }

            for (var j = 0; j < batch.Count && j < vectors.Count; j++)
            {
                var vector = vectors[j];
                if (vector.Length == 0) continue;

                batch[j].Embedding = VectorMath.Pack(vector);
                batch[j].EmbeddingDimensions = vector.Length;
                batch[j].EmbeddingModel = model;
                batch[j].IndexedAt = now;
                reembedded++;
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Knowledge base rebuilt — {Upserted} upserted, {Reembedded} (re)embedded, {Removed} removed (model {Model}).",
            upserted, reembedded, orphans.Count, model);

        var status = await ComputeStatusAsync(ct).ConfigureAwait(false);
        var skipped = Math.Max(0, existing.Count - reembedded);
        return new KnowledgeBaseRebuildResultDto(upserted, reembedded, orphans.Count, skipped, status);
    }

    public Task<KnowledgeBaseStatusDto> GetStatusAsync(CancellationToken ct)
        => ComputeStatusAsync(ct);

    private async Task<KnowledgeBaseStatusDto> ComputeStatusAsync(CancellationToken ct)
    {
        var model = embeddings.ModelName;

        var docs = await db.KnowledgeDocuments
            .AsNoTracking()
            .Select(d => new { d.SourceType, d.EmbeddingModel, d.EmbeddingDimensions, d.IndexedAt })
            .ToListAsync(ct).ConfigureAwait(false);

        var embedded = docs.Count(d => d.EmbeddingModel == model && d.EmbeddingDimensions > 0);

        return new KnowledgeBaseStatusDto(
            TotalDocuments: docs.Count,
            ScholarshipDocuments: docs.Count(d => d.SourceType == KnowledgeSourceType.Scholarship),
            FaqDocuments: docs.Count(d => d.SourceType == KnowledgeSourceType.Faq),
            EmbeddedDocuments: embedded,
            PendingDocuments: docs.Count - embedded,
            ActiveEmbeddingModel: model,
            LastIndexedAt: docs.Count == 0 ? null : docs.Max(d => d.IndexedAt));
    }

    // ─── Document builders ────────────────────────────────────────────────

    private async Task<List<DesiredDoc>> BuildScholarshipDocsAsync(CancellationToken ct)
    {
        var scholarships = await db.Scholarships
            .AsNoTracking()
            .Where(s => s.Status == ScholarshipStatus.Open)
            .ToListAsync(ct).ConfigureAwait(false);

        var docs = new List<DesiredDoc>(scholarships.Count);
        foreach (var s in scholarships)
        {
            var countries = ParseJsonArray(s.TargetCountriesJson);
            var tags = ParseJsonArray(s.TagsJson);
            var countriesText = countries.Count > 0 ? string.Join(", ", countries) : "any country";
            var tagsText = tags.Count > 0 ? string.Join(", ", tags) : "any field";
            var amount = s.FundingAmountUsd is > 0 ? $"{s.FundingAmountUsd:N0} USD" : "amount varies";

            var contentEn =
                $"Scholarship: {s.TitleEn}\n" +
                $"{s.DescriptionEn}\n" +
                $"Funding: {s.FundingType} ({amount}). Study level: {s.TargetLevel}.\n" +
                $"Eligible countries: {countriesText}.\n" +
                $"Eligibility: {s.EligibilityRequirementsEn ?? "See the official listing."}\n" +
                $"Topics: {tagsText}.\n" +
                $"Application deadline: {s.Deadline:yyyy-MM-dd}.";

            var contentAr =
                $"منحة دراسية: {s.TitleAr}\n" +
                $"{s.DescriptionAr}\n" +
                $"التمويل: {s.FundingType} ({amount}). المستوى الدراسي: {s.TargetLevel}.\n" +
                $"الدول المؤهَّلة: {countriesText}.\n" +
                $"الأهلية: {s.EligibilityRequirementsAr ?? "راجع القائمة الرسمية."}\n" +
                $"المجالات: {tagsText}.\n" +
                $"الموعد النهائي للتقديم: {s.Deadline:yyyy-MM-dd}.";

            var metadata = JsonSerializer.Serialize(new
            {
                slug = s.Slug,
                deadline = s.Deadline,
                fundingType = s.FundingType.ToString(),
                fundingAmountUsd = s.FundingAmountUsd,
                targetLevel = s.TargetLevel.ToString(),
                mode = s.Mode.ToString(),
                externalUrl = s.ExternalApplicationUrl,
            });

            docs.Add(new DesiredDoc(
                KnowledgeSourceType.Scholarship,
                s.Id.ToString("N"),
                s.Id,
                s.TitleEn,
                s.TitleAr,
                contentEn,
                contentAr,
                metadata));
        }

        return docs;
    }

    /// <summary>
    /// Projects every verified, non-suspended consultant profile into a
    /// KnowledgeDocument. The chatbot can then answer queries like
    /// "who's good at SoP review in French?" by retrieving these docs and
    /// citing the consultants by name. Unverified or suspended consultants
    /// are excluded so the AI doesn't surface accounts the student couldn't
    /// actually book.
    /// </summary>
    private async Task<List<DesiredDoc>> BuildConsultantDocsAsync(CancellationToken ct)
    {
        var consultants = await (
            from u in db.Users.AsNoTracking()
            join p in db.UserProfiles.AsNoTracking() on u.Id equals p.UserId
            where u.ActiveRole == "Consultant"
                && !u.IsDeleted
                && p.ConsultantVerifiedAt != null
                && p.BookingIntakeSuspendedAt == null
            select new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.CountryOfResidence,
                p.Biography,
                p.BiographyAr,
                p.ProfessionalTitle,
                p.HighestDegree,
                p.FieldOfExpertise,
                p.YearsOfExperience,
                p.SessionFeeUsd,
                p.SessionDurationMinutes,
                p.ExpertiseTagsJson,
                p.LanguagesJson,
            }).ToListAsync(ct).ConfigureAwait(false);

        var docs = new List<DesiredDoc>(consultants.Count);
        foreach (var c in consultants)
        {
            var name = $"{c.FirstName} {c.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            var tags = ParseJsonArray(c.ExpertiseTagsJson);
            var languages = ParseJsonArray(c.LanguagesJson);
            var tagsText = tags.Count > 0 ? string.Join(", ", tags) : "general guidance";
            var langsText = languages.Count > 0 ? string.Join(", ", languages) : "en";
            var bioEn = string.IsNullOrWhiteSpace(c.Biography) ? "" : c.Biography.Trim();
            var bioAr = string.IsNullOrWhiteSpace(c.BiographyAr) ? "" : c.BiographyAr.Trim();
            var feeText = c.SessionFeeUsd is > 0 ? $"{c.SessionFeeUsd:N0} USD" : "varies";
            var country = c.CountryOfResidence ?? "international";

            var contentEn =
                $"Consultant: {name}\n" +
                $"Title: {c.ProfessionalTitle ?? "Consultant"}. Country: {country}.\n" +
                $"Highest degree: {c.HighestDegree ?? "not specified"}. " +
                $"Field of expertise: {c.FieldOfExpertise ?? "general"}.\n" +
                $"Years of experience: {c.YearsOfExperience?.ToString() ?? "n/a"}.\n" +
                $"Session fee: {feeText} for {c.SessionDurationMinutes ?? 45} minutes.\n" +
                $"Specializations: {tagsText}.\n" +
                $"Speaks: {langsText}.\n" +
                $"{bioEn}";

            var contentAr =
                $"مستشار: {name}\n" +
                $"المسمى: {c.ProfessionalTitle ?? "مستشار"}. الدولة: {country}.\n" +
                $"أعلى مؤهل: {c.HighestDegree ?? "غير محدد"}. " +
                $"مجال الخبرة: {c.FieldOfExpertise ?? "عام"}.\n" +
                $"سنوات الخبرة: {c.YearsOfExperience?.ToString() ?? "غير محددة"}.\n" +
                $"سعر الجلسة: {feeText} لمدة {c.SessionDurationMinutes ?? 45} دقيقة.\n" +
                $"التخصصات: {tagsText}.\n" +
                $"اللغات: {langsText}.\n" +
                $"{bioAr}";

            var metadata = JsonSerializer.Serialize(new
            {
                consultantId = c.Id,
                sessionFeeUsd = c.SessionFeeUsd,
                sessionDurationMinutes = c.SessionDurationMinutes,
                expertiseTags = tags,
                languages,
            });

            docs.Add(new DesiredDoc(
                KnowledgeSourceType.Consultant,
                c.Id.ToString("N"),
                c.Id,
                name,
                name,
                contentEn,
                contentAr,
                metadata));
        }

        return docs;
    }

    /// <summary>
    /// Projects every Published, non-deleted resource (article, guide,
    /// checklist, video link) into a KnowledgeDocument so the chatbot can
    /// point students at relevant in-house content.
    /// </summary>
    private async Task<List<DesiredDoc>> BuildResourceDocsAsync(CancellationToken ct)
    {
        var resources = await db.Resources
            .AsNoTracking()
            .Where(r => r.Status == ResourceStatus.Published && !r.IsDeleted)
            .ToListAsync(ct).ConfigureAwait(false);

        var docs = new List<DesiredDoc>(resources.Count);
        foreach (var r in resources)
        {
            var tags = ParseJsonArray(r.TagsJson);
            var tagsText = tags.Count > 0 ? string.Join(", ", tags) : "general";

            // Trim long markdown bodies so a 50-chapter guide doesn't blow
            // out the embedding token budget. 1500 chars is enough for the
            // retriever to score the doc; the chatbot doesn't replay the
            // whole body anyway.
            var bodyEn = Truncate(r.ContentMarkdownEn, 1500);
            var bodyAr = Truncate(r.ContentMarkdownAr, 1500);

            var contentEn =
                $"Resource ({r.Type}): {r.TitleEn}\n" +
                $"{r.DescriptionEn ?? string.Empty}\n" +
                $"Tags: {tagsText}.\n" +
                (string.IsNullOrEmpty(bodyEn) ? string.Empty : $"\n{bodyEn}");

            var contentAr =
                $"مورد ({r.Type}): {r.TitleAr}\n" +
                $"{r.DescriptionAr ?? string.Empty}\n" +
                $"الوسوم: {tagsText}.\n" +
                (string.IsNullOrEmpty(bodyAr) ? string.Empty : $"\n{bodyAr}");

            var metadata = JsonSerializer.Serialize(new
            {
                slug = r.Slug,
                type = r.Type.ToString(),
                authorRole = r.AuthorRole,
                isFeatured = r.IsFeatured,
                externalLinkUrl = r.ExternalLinkUrl,
            });

            docs.Add(new DesiredDoc(
                KnowledgeSourceType.Resource,
                r.Id.ToString("N"),
                r.Id,
                r.TitleEn,
                r.TitleAr,
                contentEn,
                contentAr,
                metadata));
        }

        return docs;
    }

    /// <summary>
    /// Projects the highest-quality community threads (positive net score,
    /// not hidden / locked) into KnowledgeDocuments so the chatbot can
    /// surface peer wisdom alongside the official help content. Only root
    /// threads are indexed; replies are deliberately excluded to keep the
    /// index focused.
    /// </summary>
    private async Task<List<DesiredDoc>> BuildTopCommunityPostDocsAsync(CancellationToken ct)
    {
        var posts = await db.ForumPosts
            .AsNoTracking()
            .Where(p => p.ParentPostId == null
                        && !p.IsDeleted
                        && !p.IsAutoHidden
                        && p.ModerationStatus == PostModerationStatus.Visible
                        && (p.UpvoteCount - p.DownvoteCount) >= 3)
            .OrderByDescending(p => p.UpvoteCount - p.DownvoteCount)
            .Take(200)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.BodyMarkdown,
                p.UpvoteCount,
                p.DownvoteCount,
                p.ReplyCount,
            })
            .ToListAsync(ct).ConfigureAwait(false);

        var docs = new List<DesiredDoc>(posts.Count);
        foreach (var p in posts)
        {
            var title = string.IsNullOrWhiteSpace(p.Title) ? "Community thread" : p.Title;
            var body = Truncate(p.BodyMarkdown, 1500);
            var score = p.UpvoteCount - p.DownvoteCount;

            // The body is user-generated text in whatever language the
            // author wrote — we don't try to translate it. Embed the same
            // body under both EN and AR fields so the retriever finds the
            // doc regardless of the query's language.
            var contentEn =
                $"Community discussion: {title}\n" +
                $"Score: +{score} from the community ({p.ReplyCount} replies).\n\n" +
                body;
            var contentAr = contentEn;

            var metadata = JsonSerializer.Serialize(new
            {
                postId = p.Id,
                score,
                replyCount = p.ReplyCount,
            });

            docs.Add(new DesiredDoc(
                KnowledgeSourceType.CommunityPost,
                p.Id.ToString("N"),
                p.Id,
                title,
                title,
                contentEn,
                contentAr,
                metadata));
        }

        return docs;
    }

    /// <summary>Trims a string to <paramref name="max"/> characters, preserving word boundaries.</summary>
    private static string Truncate(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var trimmed = text.Trim();
        if (trimmed.Length <= max) return trimmed;
        var cut = trimmed.LastIndexOf(' ', max);
        if (cut < max / 2) cut = max;
        return trimmed[..cut] + "…";
    }

    private List<DesiredDoc> BuildFaqDocs()
    {
        var json = datasets.GetDatasetJson(FaqDatasetName);
        if (string.IsNullOrWhiteSpace(json))
        {
            logger.LogWarning(
                "FAQ dataset '{Dataset}' not found; the knowledge base will hold scholarships only.",
                FaqDatasetName);
            return [];
        }

        FaqDataset? dataset;
        try
        {
            dataset = JsonSerializer.Deserialize<FaqDataset>(json, JsonOpts);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse the FAQ dataset.");
            return [];
        }

        var docs = new List<DesiredDoc>();
        foreach (var f in dataset?.Faqs ?? [])
        {
            if (string.IsNullOrWhiteSpace(f.Key)) continue;

            var contentEn = $"Question: {f.QuestionEn}\nAnswer: {f.AnswerEn}";
            var contentAr = $"سؤال: {f.QuestionAr}\nالإجابة: {f.AnswerAr}";
            var metadata = f.Tags is { Count: > 0 }
                ? JsonSerializer.Serialize(new { tags = f.Tags })
                : null;

            docs.Add(new DesiredDoc(
                KnowledgeSourceType.Faq,
                f.Key,
                SourceId: null,
                f.QuestionEn,
                f.QuestionAr,
                contentEn,
                contentAr,
                metadata));
        }

        return docs;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private static string EmbedText(KnowledgeDocument d) => d.ContentEn + "\n\n" + d.ContentAr;

    private static string Sha256(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private static IReadOnlyList<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record DesiredDoc(
        KnowledgeSourceType SourceType,
        string SourceKey,
        Guid? SourceId,
        string TitleEn,
        string TitleAr,
        string ContentEn,
        string ContentAr,
        string? MetadataJson)
    {
        public string EmbedText => ContentEn + "\n\n" + ContentAr;
    }

    private sealed record FaqDataset(List<FaqEntry>? Faqs);

    private sealed record FaqEntry(
        string Key,
        string QuestionEn,
        string QuestionAr,
        string AnswerEn,
        string AnswerAr,
        List<string>? Tags);
}
