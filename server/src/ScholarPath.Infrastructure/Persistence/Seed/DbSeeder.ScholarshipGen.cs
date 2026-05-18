using System.Text;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Seed;

/// <summary>
/// Generates the bulk bilingual scholarship catalogue. Kept separate from
/// <c>DbSeeder.Scholarships.cs</c> (which holds the hand-curated rows and the
/// orchestration) so the templated building blocks stay navigable.
/// </summary>
public static partial class DbSeeder
{
    // ── Bilingual building blocks ────────────────────────────────────────────
    private static readonly (string En, string Ar, string Cat)[] GenFields =
    [
        ("Computer Science", "علوم الحاسب", "stem"),
        ("Software Engineering", "هندسة البرمجيات", "stem"),
        ("Artificial Intelligence", "الذكاء الاصطناعي", "stem"),
        ("Data Science", "علم البيانات", "stem"),
        ("Cybersecurity", "الأمن السيبراني", "stem"),
        ("Electrical Engineering", "الهندسة الكهربائية", "stem"),
        ("Mechanical Engineering", "الهندسة الميكانيكية", "stem"),
        ("Civil Engineering", "الهندسة المدنية", "stem"),
        ("Renewable Energy", "الطاقة المتجددة", "stem"),
        ("Mathematics", "الرياضيات", "stem"),
        ("Physics", "الفيزياء", "stem"),
        ("Environmental Science", "العلوم البيئية", "stem"),
        ("Medicine", "الطب", "medical"),
        ("Pharmacy", "الصيدلة", "medical"),
        ("Public Health", "الصحة العامة", "medical"),
        ("Nursing", "التمريض", "medical"),
        ("Dentistry", "طب الأسنان", "medical"),
        ("Biotechnology", "التقنية الحيوية", "medical"),
        ("Business Administration", "إدارة الأعمال", "business"),
        ("Finance", "التمويل", "business"),
        ("Economics", "الاقتصاد", "business"),
        ("Marketing", "التسويق", "business"),
        ("Accounting", "المحاسبة", "business"),
        ("Entrepreneurship", "ريادة الأعمال", "business"),
        ("Law", "القانون", "arts-humanities"),
        ("Education", "التربية والتعليم", "arts-humanities"),
        ("Architecture", "العمارة", "arts-humanities"),
        ("Fine Arts", "الفنون الجميلة", "arts-humanities"),
        ("Graphic Design", "التصميم الجرافيكي", "arts-humanities"),
        ("Literature", "الأدب", "arts-humanities"),
        ("Psychology", "علم النفس", "arts-humanities"),
        ("International Relations", "العلاقات الدولية", "arts-humanities"),
    ];

    private static readonly (string En, string Ar)[] GenProviders =
    [
        ("Horizon Foundation", "مؤسسة هورايزون"),
        ("Bright Future Trust", "صندوق المستقبل المشرق"),
        ("Global Knowledge Council", "مجلس المعرفة العالمي"),
        ("Pioneer Education Fund", "صندوق بيونير للتعليم"),
        ("Crescent Scholars Initiative", "مبادرة الهلال للدارسين"),
        ("Nile Academy Trust", "صندوق أكاديمية النيل"),
        ("Summit Excellence Foundation", "مؤسسة القمة للتميز"),
        ("Aspire Education Network", "شبكة أسباير التعليمية"),
        ("Cedar International Trust", "صندوق سيدار الدولي"),
        ("Atlas Learning Foundation", "مؤسسة أطلس للتعلّم"),
        ("Vanguard Scholars Fund", "صندوق فانغارد للدارسين"),
        ("Lighthouse Academic Trust", "صندوق المنارة الأكاديمي"),
        ("Meridian Education Group", "مجموعة ميريديان التعليمية"),
        ("Falcon Talent Foundation", "مؤسسة الصقر للمواهب"),
        ("Oasis Higher Education Fund", "صندوق الواحة للتعليم العالي"),
        ("Beacon Global Scholars", "بيكون للدارسين عالمياً"),
        ("Unity Education Trust", "صندوق الوحدة للتعليم"),
        ("Polaris Research Foundation", "مؤسسة بولاريس للبحث"),
        ("Evergreen Learning Trust", "صندوق إيفرغرين للتعلّم"),
        ("Continental Scholars Council", "مجلس القارة للدارسين"),
    ];

    private static readonly (AcademicLevel Level, string En, string Ar)[] GenLevels =
    [
        (AcademicLevel.HighSchool, "High School", "الثانوية"),
        (AcademicLevel.Undergrad, "Undergraduate", "البكالوريوس"),
        (AcademicLevel.Masters, "Master's", "الماجستير"),
        (AcademicLevel.PhD, "PhD", "الدكتوراه"),
        (AcademicLevel.PostDoc, "Postdoctoral", "ما بعد الدكتوراه"),
    ];

    private static readonly string[] GenCountries =
        ["US", "GB", "CA", "AU", "DE", "FR", "NL", "SE", "CH", "EG", "JO", "AE", "SA", "JP", "IT", "ES"];

    /// <summary>
    /// Generates <paramref name="count"/> fully bilingual scholarships from the
    /// templated building blocks above — varied across field, provider, level,
    /// funding type, status, mode, deadline and target countries. Slugs are
    /// guaranteed unique against <paramref name="usedSlugs"/>.
    /// </summary>
    private static List<Scholarship> GenerateScholarshipCatalogue(
        int count,
        DateTimeOffset now,
        IReadOnlyList<Category> categories,
        IReadOnlyList<ApplicationUser> companies,
        Guid adminId,
        HashSet<string> usedSlugs)
    {
        var rng = new Random(20260518);
        var list = new List<Scholarship>(count);
        FundingType[] fundings =
        [
            FundingType.FullyFunded, FundingType.PartiallyFunded,
            FundingType.TuitionOnly, FundingType.StipendOnly, FundingType.Other,
        ];

        for (var i = 0; i < count; i++)
        {
            var field = GenFields[rng.Next(GenFields.Length)];
            var provider = GenProviders[rng.Next(GenProviders.Length)];
            var lvl = GenLevels[rng.Next(GenLevels.Length)];
            var funding = fundings[rng.Next(fundings.Length)];
            var category = categories.FirstOrDefault(c => c.Slug == field.Cat) ?? categories[0];

            var roll = rng.Next(100);
            var status = roll < 74 ? ScholarshipStatus.Open
                : roll < 84 ? ScholarshipStatus.Closed
                : roll < 91 ? ScholarshipStatus.Draft
                : roll < 96 ? ScholarshipStatus.UnderReview
                : ScholarshipStatus.Archived;

            var external = rng.Next(100) < 28;
            var mode = external ? ListingMode.ExternalUrl : ListingMode.InApp;

            var fund = FundingCopy(funding, lvl.Level, rng);
            var elig = EligibilityCopy(lvl.Level);

            var titleEn = $"{provider.En} {field.En} {lvl.En} Scholarship";
            var titleAr = $"منحة {provider.Ar} في {field.Ar} لمرحلة {lvl.Ar}";
            var descEn = $"{provider.En} offers a {fund.FundEn} scholarship for {lvl.En}-level students of {field.En}. {fund.CoverEn}";
            var descAr = $"تقدّم {provider.Ar} منحة {fund.FundAr} لطلاب مرحلة {lvl.Ar} في تخصص {field.Ar}. {fund.CoverAr}";

            var slug = MakeUniqueSlug($"{provider.En} {field.En} {lvl.En}", i, usedSlugs);

            var adminCurated = companies.Count == 0 || rng.Next(100) < 10;
            var owner = adminCurated ? (Guid?)null : companies[rng.Next(companies.Count)].Id;

            var createdAt = now.AddDays(-rng.Next(1, 300));
            DateTimeOffset deadline;
            DateTimeOffset? openedAt = null, archivedAt = null;
            switch (status)
            {
                case ScholarshipStatus.Open:
                    deadline = now.AddDays(rng.Next(8, 130));
                    openedAt = now.AddDays(-rng.Next(1, 40));
                    break;
                case ScholarshipStatus.Closed:
                    deadline = now.AddDays(-rng.Next(3, 70));
                    openedAt = now.AddDays(-rng.Next(80, 200));
                    break;
                case ScholarshipStatus.Archived:
                    deadline = now.AddDays(-rng.Next(120, 400));
                    openedAt = now.AddDays(-rng.Next(420, 700));
                    archivedAt = now.AddDays(-rng.Next(60, 110));
                    break;
                default: // Draft / UnderReview
                    deadline = now.AddDays(rng.Next(40, 160));
                    break;
            }

            var countryCount = rng.Next(1, 4);
            var countries = new HashSet<string>();
            while (countries.Count < countryCount)
            {
                countries.Add(GenCountries[rng.Next(GenCountries.Length)]);
            }

            list.Add(new Scholarship
            {
                TitleEn = titleEn,
                TitleAr = titleAr,
                DescriptionEn = descEn,
                DescriptionAr = descAr,
                Slug = slug,
                CategoryId = category.Id,
                OwnerCompanyId = owner,
                CreatedByAdminId = adminCurated ? adminId : null,
                Mode = mode,
                ExternalApplicationUrl = external ? $"https://apply.example.org/{slug}" : null,
                Status = status,
                Deadline = deadline,
                OpenedAt = openedAt,
                ArchivedAt = archivedAt,
                FundingType = funding,
                FundingAmountUsd = fund.Amount,
                TargetLevel = lvl.Level,
                TargetCountriesJson = JsonArray(countries),
                EligibilityRequirementsEn = elig.En,
                EligibilityRequirementsAr = elig.Ar,
                TagsJson = JsonArray([field.Cat, funding.ToString()]),
                ReviewFeeUsd = mode == ListingMode.InApp ? rng.Next(2, 9) * 5m : 0m,
                CreatedAt = createdAt,
            });
        }

        return list;
    }

    private static (string FundEn, string FundAr, string CoverEn, string CoverAr, decimal Amount)
        FundingCopy(FundingType f, AcademicLevel lvl, Random rng)
    {
        var baseAmt = lvl switch
        {
            AcademicLevel.HighSchool => 3_000m,
            AcademicLevel.Undergrad => 12_000m,
            AcademicLevel.Masters => 32_000m,
            AcademicLevel.PhD => 45_000m,
            _ => 55_000m,
        };
        var amount = baseAmt + rng.Next(0, 16) * 1_000m;
        return f switch
        {
            FundingType.FullyFunded => ("fully funded", "ممولة بالكامل",
                "It covers full tuition, a monthly living stipend, and travel costs.",
                "تغطي الرسوم الدراسية كاملةً وراتب معيشة شهرياً وتكاليف السفر.", amount),
            FundingType.PartiallyFunded => ("partially funded", "ممولة جزئياً",
                "It covers part of the tuition fees and a contribution toward living costs.",
                "تغطي جزءاً من الرسوم الدراسية ومساهمةً في تكاليف المعيشة.", amount * 0.5m),
            FundingType.TuitionOnly => ("tuition-only", "تغطي الرسوم الدراسية",
                "It covers the full tuition fees for the programme.",
                "تغطي الرسوم الدراسية كاملةً للبرنامج.", amount * 0.7m),
            FundingType.StipendOnly => ("stipend", "راتب معيشة",
                "It provides a monthly living stipend for the duration of study.",
                "توفّر راتب معيشة شهرياً طوال فترة الدراسة.", amount * 0.35m),
            _ => ("supplementary", "داعمة",
                "It provides supplementary support toward study-related expenses.",
                "توفّر دعماً إضافياً للمصروفات المتعلقة بالدراسة.", amount * 0.25m),
        };
    }

    private static (string En, string Ar) EligibilityCopy(AcademicLevel lvl) => lvl switch
    {
        AcademicLevel.HighSchool => (
            "Open to high-school students with strong academic results.",
            "متاحة لطلاب المرحلة الثانوية ذوي النتائج الأكاديمية القوية."),
        AcademicLevel.Undergrad => (
            "Requires a high-school certificate and a competitive academic record.",
            "تتطلب شهادة الثانوية العامة وسجلاً أكاديمياً تنافسياً."),
        AcademicLevel.Masters => (
            "Requires a relevant Bachelor's degree and proof of English proficiency.",
            "تتطلب شهادة بكالوريوس في تخصص ذي صلة وإثبات إتقان اللغة الإنجليزية."),
        AcademicLevel.PhD => (
            "Requires a relevant Master's degree and an accepted research proposal.",
            "تتطلب شهادة ماجستير في تخصص ذي صلة ومقترحاً بحثياً مقبولاً."),
        _ => (
            "Requires a PhD awarded within the last five years.",
            "تتطلب شهادة دكتوراه ممنوحة خلال السنوات الخمس الماضية."),
    };

    /// <summary>Serialises a string collection into a compact JSON array.</summary>
    private static string JsonArray(IEnumerable<string> values)
        => "[" + string.Join(",", values.Select(v => $"\"{v}\"")) + "]";

    /// <summary>Builds a URL slug unique against <paramref name="used"/>.</summary>
    private static string MakeUniqueSlug(string text, int index, HashSet<string> used)
    {
        var baseSlug = ToAsciiSlug(text);
        if (baseSlug.Length == 0) baseSlug = "scholarship";
        var candidate = $"{baseSlug}-{index + 1}";
        var suffix = 1;
        while (!used.Add(candidate))
        {
            candidate = $"{baseSlug}-{index + 1}-{suffix++}";
        }
        return candidate;
    }

    /// <summary>
    /// ASCII slugifier — keeps a-z0-9, collapses everything else to single
    /// hyphens. Distinct from <c>DbSeeder.Slugify</c> (a plain lower-caser);
    /// avoids ToLowerInvariant so the CA1308 analyzer stays quiet.
    /// </summary>
    private static string ToAsciiSlug(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            var c = ch is >= 'A' and <= 'Z' ? (char)(ch + 32) : ch;
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(c);
            }
            else if (sb.Length > 0 && sb[^1] != '-')
            {
                sb.Append('-');
            }
        }
        return sb.ToString().Trim('-');
    }
}
