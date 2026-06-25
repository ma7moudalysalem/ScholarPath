using System.Globalization;
using System.Text;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Reconciles the two country representations the platform stores. Scholarships
/// persist <c>TargetCountriesJson</c> as ISO 3166-1 alpha-2 codes
/// (<c>["EG","JO","AE"]</c>), while a student's nationality / preferred-country
/// values are full English names ("Egypt") or free-typed text (possibly Arabic,
/// e.g. "مصر"). A naive string compare of "EG" against "Egypt" never matches,
/// which made the eligibility "Country" criterion always read <c>no</c>.
///
/// <see cref="ToKey"/> collapses any of those forms to a single canonical key
/// (the uppercase ISO alpha-2 code when the country is known), so both sides of
/// a comparison line up. Unknown values fall back to a trimmed, case- and
/// diacritic-folded form, so two equal free-text values still match.
/// </summary>
public static class CountryNormalizer
{
    /// <summary>Canonical key for two countries being "the same" place.</summary>
    public static bool Matches(string? a, string? b)
    {
        var ka = ToKey(a);
        var kb = ToKey(b);
        return ka.Length > 0 && string.Equals(ka, kb, StringComparison.Ordinal);
    }

    /// <summary>
    /// Maps an ISO alpha-2 code, full English name, common alias, or Arabic
    /// name to the uppercase ISO alpha-2 code. Returns a folded form of the
    /// input when the country is unrecognised, and "" for null/blank.
    /// </summary>
    public static string ToKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var trimmed = value.Trim();
        var folded = Fold(trimmed);

        // Known name / alias / Arabic name wins first, so two-letter aliases that
        // are NOT their own ISO code (e.g. "UK" → GB) resolve correctly.
        if (Lookup.TryGetValue(folded, out var iso))
            return iso;

        // Otherwise a two-letter token is taken to be an ISO alpha-2 code (the
        // scholarship storage form, e.g. "EG").
        if (trimmed.Length == 2 && trimmed.All(char.IsLetter))
            return trimmed.ToUpperInvariant();

        return folded;
    }

    /// <summary>
    /// Case-folds (to upper-invariant — CA1308's recommended direction for
    /// normalization keys), trims, collapses whitespace and strips diacritics.
    /// </summary>
    private static string Fold(string value)
    {
        var cased = value.ToUpperInvariant().Trim();

        var decomposed = cased.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        var lastWasSpace = false;
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasSpace && sb.Length > 0) sb.Append(' ');
                lastWasSpace = true;
                continue;
            }
            sb.Append(ch);
            lastWasSpace = false;
        }

        return sb.ToString().TrimEnd().Normalize(NormalizationForm.FormC);
    }

    // Folded name / alias / Arabic name → ISO alpha-2. Built once.
    private static readonly IReadOnlyDictionary<string, string> Lookup = BuildLookup();

    private static Dictionary<string, string> BuildLookup()
    {
        // Canonical English name → ISO alpha-2 (mirrors the client COUNTRIES list).
        var names = new (string Name, string Iso)[]
        {
            ("Afghanistan","AF"),("Albania","AL"),("Algeria","DZ"),("Argentina","AR"),
            ("Armenia","AM"),("Australia","AU"),("Austria","AT"),("Azerbaijan","AZ"),
            ("Bahrain","BH"),("Bangladesh","BD"),("Belarus","BY"),("Belgium","BE"),
            ("Bolivia","BO"),("Bosnia and Herzegovina","BA"),("Brazil","BR"),
            ("Bulgaria","BG"),("Cambodia","KH"),("Cameroon","CM"),("Canada","CA"),
            ("Chile","CL"),("China","CN"),("Colombia","CO"),("Croatia","HR"),
            ("Cuba","CU"),("Czech Republic","CZ"),("Denmark","DK"),("Ecuador","EC"),
            ("Egypt","EG"),("Ethiopia","ET"),("Finland","FI"),("France","FR"),
            ("Georgia","GE"),("Germany","DE"),("Ghana","GH"),("Greece","GR"),
            ("Guatemala","GT"),("Hungary","HU"),("India","IN"),("Indonesia","ID"),
            ("Iran","IR"),("Iraq","IQ"),("Ireland","IE"),("Italy","IT"),
            ("Japan","JP"),("Jordan","JO"),("Kazakhstan","KZ"),("Kenya","KE"),
            ("Kuwait","KW"),("Kyrgyzstan","KG"),("Lebanon","LB"),("Libya","LY"),
            ("Malaysia","MY"),("Mexico","MX"),("Morocco","MA"),("Myanmar","MM"),
            ("Nepal","NP"),("Netherlands","NL"),("New Zealand","NZ"),("Nigeria","NG"),
            ("Norway","NO"),("Oman","OM"),("Pakistan","PK"),("Palestine","PS"),
            ("Peru","PE"),("Philippines","PH"),("Poland","PL"),("Portugal","PT"),
            ("Qatar","QA"),("Romania","RO"),("Russia","RU"),("Saudi Arabia","SA"),
            ("Senegal","SN"),("Serbia","RS"),("South Africa","ZA"),("South Korea","KR"),
            ("Spain","ES"),("Sri Lanka","LK"),("Sudan","SD"),("Sweden","SE"),
            ("Switzerland","CH"),("Syria","SY"),("Taiwan","TW"),("Tajikistan","TJ"),
            ("Tanzania","TZ"),("Thailand","TH"),("Tunisia","TN"),("Turkey","TR"),
            ("Turkmenistan","TM"),("Uganda","UG"),("Ukraine","UA"),
            ("United Arab Emirates","AE"),("United Kingdom","GB"),("United States","US"),
            ("Uruguay","UY"),("Uzbekistan","UZ"),("Venezuela","VE"),("Vietnam","VN"),
            ("Yemen","YE"),("Zimbabwe","ZW"),
        };

        // Common English aliases / abbreviations users actually type.
        var aliases = new (string Alias, string Iso)[]
        {
            ("usa","US"),("u.s.a","US"),("u.s","US"),("us","US"),("america","US"),
            ("united states of america","US"),
            ("uk","GB"),("u.k","GB"),("britain","GB"),("great britain","GB"),
            ("england","GB"),("united kingdom of great britain and northern ireland","GB"),
            ("uae","AE"),("emirates","AE"),("the emirates","AE"),
            ("ksa","SA"),("kingdom of saudi arabia","SA"),
            ("russian federation","RU"),("korea","KR"),("republic of korea","KR"),
            ("south korea","KR"),("czechia","CZ"),("holland","NL"),
            ("the netherlands","NL"),("turkiye","TR"),("türkiye","TR"),
        };

        // Arabic names → ISO alpha-2 (mirrors the client COUNTRY_AR map), so a
        // free-typed Arabic tag like "مصر" still resolves.
        var arabic = new (string Name, string Iso)[]
        {
            ("أفغانستان","AF"),("ألبانيا","AL"),("الجزائر","DZ"),("الأرجنتين","AR"),
            ("أرمينيا","AM"),("أستراليا","AU"),("النمسا","AT"),("أذربيجان","AZ"),
            ("البحرين","BH"),("بنغلاديش","BD"),("بيلاروسيا","BY"),("بلجيكا","BE"),
            ("بوليفيا","BO"),("البوسنة والهرسك","BA"),("البرازيل","BR"),("بلغاريا","BG"),
            ("كمبوديا","KH"),("الكاميرون","CM"),("كندا","CA"),("تشيلي","CL"),
            ("الصين","CN"),("كولومبيا","CO"),("كرواتيا","HR"),("كوبا","CU"),
            ("التشيك","CZ"),("الدنمارك","DK"),("الإكوادور","EC"),("مصر","EG"),
            ("إثيوبيا","ET"),("فنلندا","FI"),("فرنسا","FR"),("جورجيا","GE"),
            ("ألمانيا","DE"),("غانا","GH"),("اليونان","GR"),("غواتيمالا","GT"),
            ("المجر","HU"),("الهند","IN"),("إندونيسيا","ID"),("إيران","IR"),
            ("العراق","IQ"),("أيرلندا","IE"),("إيطاليا","IT"),("اليابان","JP"),
            ("الأردن","JO"),("كازاخستان","KZ"),("كينيا","KE"),("الكويت","KW"),
            ("قيرغيزستان","KG"),("لبنان","LB"),("ليبيا","LY"),("ماليزيا","MY"),
            ("المكسيك","MX"),("المغرب","MA"),("ميانمار","MM"),("نيبال","NP"),
            ("هولندا","NL"),("نيوزيلندا","NZ"),("نيجيريا","NG"),("النرويج","NO"),
            ("عمان","OM"),("عُمان","OM"),("باكستان","PK"),("فلسطين","PS"),("بيرو","PE"),
            ("الفلبين","PH"),("بولندا","PL"),("البرتغال","PT"),("قطر","QA"),
            ("رومانيا","RO"),("روسيا","RU"),("السعودية","SA"),
            ("المملكة العربية السعودية","SA"),("السنغال","SN"),("صربيا","RS"),
            ("جنوب أفريقيا","ZA"),("كوريا الجنوبية","KR"),("إسبانيا","ES"),
            ("سريلانكا","LK"),("السودان","SD"),("السويد","SE"),("سويسرا","CH"),
            ("سوريا","SY"),("تايوان","TW"),("طاجيكستان","TJ"),("تنزانيا","TZ"),
            ("تايلاند","TH"),("تونس","TN"),("تركيا","TR"),("تركمانستان","TM"),
            ("أوغندا","UG"),("أوكرانيا","UA"),("الإمارات العربية المتحدة","AE"),
            ("الإمارات","AE"),("المملكة المتحدة","GB"),("الولايات المتحدة","US"),
            ("الأوروغواي","UY"),("أوزبكستان","UZ"),("فنزويلا","VE"),("فيتنام","VN"),
            ("اليمن","YE"),("زيمبابوي","ZW"),
        };

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, iso) in names) map[Fold(name)] = iso;
        foreach (var (alias, iso) in aliases) map[Fold(alias)] = iso;
        foreach (var (name, iso) in arabic) map[Fold(name)] = iso;
        return map;
    }
}
