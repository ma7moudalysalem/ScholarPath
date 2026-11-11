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

        // Official long forms: "Tanzania, United Republic of", "Syrian Arab
        // Republic (Syria)". Try the part before the comma, then the bracketed
        // short name, then the name without the bracket.
        var open = trimmed.IndexOf('(', StringComparison.Ordinal);
        var close = trimmed.IndexOf(')', StringComparison.Ordinal);
        var comma = trimmed.IndexOf(',', StringComparison.Ordinal);
        var variants = new List<string>();
        if (comma > 0) variants.Add(trimmed[..comma]);
        if (open >= 0 && close > open)
        {
            variants.Add(trimmed[(open + 1)..close]);
            variants.Add(trimmed[..open]);
        }
        foreach (var v in variants)
            if (Lookup.TryGetValue(Fold(v), out var viso))
                return viso;

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

    // ISO alpha-2 → display name, filled by BuildLookup (declared first so the
    // lookup's initializer can fill them).
    private static readonly Dictionary<string, string> EnglishByIso = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> ArabicByIso = new(StringComparer.OrdinalIgnoreCase);

    // Folded name / alias / Arabic name → ISO alpha-2. Built once.
    private static readonly IReadOnlyDictionary<string, string> Lookup = BuildLookup();

    /// <summary>The country's name in English or Arabic; the input when it is not known.</summary>
    public static string DisplayName(string? value, bool arabic)
    {
        var iso = ToKey(value);
        return (arabic ? ArabicByIso : EnglishByIso).TryGetValue(iso, out var name) ? name : value ?? string.Empty;
    }

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
            // Countries named in real catalogs (DAAD origin lists, government programmes).
            ("أندورا","AD"),("أنتيغوا وباربودا","AG"),("أنغويلا","AI"),("أنغولا","AO"),
            ("باربادوس","BB"),("بوركينا فاسو","BF"),("بوروندي","BI"),("بنين","BJ"),
            ("برمودا","BM"),("بروناي","BN"),("جزر البهاما","BS"),("بوتان","BT"),
            ("بوتسوانا","BW"),("بليز","BZ"),("جمهورية الكونغو الديمقراطية","CD"),
            ("جمهورية أفريقيا الوسطى","CF"),("جمهورية الكونغو","CG"),("ساحل العاج","CI"),
            ("جزر كوك","CK"),("كوستاريكا","CR"),("الرأس الأخضر","CV"),("قبرص","CY"),
            ("جيبوتي","DJ"),("دومينيكا","DM"),("جمهورية الدومينيكان","DO"),("إستونيا","EE"),
            ("إريتريا","ER"),("فيجي","FJ"),("ميكرونيزيا","FM"),("الغابون","GA"),
            ("غرينادا","GD"),("غويانا الفرنسية","GF"),("غامبيا","GM"),("غينيا","GN"),
            ("غوادلوب","GP"),("غينيا الاستوائية","GQ"),("غوام","GU"),("غينيا بيساو","GW"),
            ("غيانا","GY"),("هونغ كونغ","HK"),("هندوراس","HN"),("هايتي","HT"),
            ("إسرائيل","IL"),("آيسلندا","IS"),("جامايكا","JM"),("كيريباتي","KI"),
            ("جزر القمر","KM"),("سانت كيتس ونيفيس","KN"),("لاوس","LA"),("سانت لوسيا","LC"),
            ("ليختنشتاين","LI"),("ليبيريا","LR"),("ليسوتو","LS"),("ليتوانيا","LT"),
            ("لوكسمبورغ","LU"),("لاتفيا","LV"),("موناكو","MC"),("مولدوفا","MD"),
            ("الجبل الأسود","ME"),("مدغشقر","MG"),("جزر مارشال","MH"),("مقدونيا الشمالية","MK"),
            ("مالي","ML"),("منغوليا","MN"),("ماكاو","MO"),("مارتينيك","MQ"),
            ("موريتانيا","MR"),("مونتسرات","MS"),("مالطا","MT"),("موريشيوس","MU"),
            ("جزر المالديف","MV"),("ملاوي","MW"),("موزمبيق","MZ"),("ناميبيا","NA"),
            ("كاليدونيا الجديدة","NC"),("النيجر","NE"),("نيكاراغوا","NI"),("ناورو","NR"),
            ("نيوي","NU"),("بنما","PA"),("بولينيزيا الفرنسية","PF"),("بابوا غينيا الجديدة","PG"),
            ("بورتوريكو","PR"),("بالاو","PW"),("باراغواي","PY"),("لا ريونيون","RE"),
            ("رواندا","RW"),("جزر سليمان","SB"),("سيشل","SC"),("سنغافورة","SG"),
            ("سلوفينيا","SI"),("سلوفاكيا","SK"),("سيراليون","SL"),("سان مارينو","SM"),
            ("الصومال","SO"),("سورينام","SR"),("جنوب السودان","SS"),("ساو تومي وبرينسيب","ST"),
            ("السلفادور","SV"),("إسواتيني","SZ"),("تشاد","TD"),("توغو","TG"),
            ("تيمور الشرقية","TL"),("تونغا","TO"),("ترينيداد وتوباغو","TT"),("توفالو","TV"),
            ("الفاتيكان","VA"),("سانت فينسنت والغرينادين","VC"),("فانواتو","VU"),
            ("واليس وفوتونا","WF"),("ساموا","WS"),("كوسوفو","XK"),("زامبيا","ZM"),
        };

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, iso) in names) { map[Fold(name)] = iso; EnglishByIso.TryAdd(iso, name); }
        foreach (var (alias, iso) in aliases) map[Fold(alias)] = iso;
        foreach (var (name, iso) in arabic) { map[Fold(name)] = iso; ArabicByIso.TryAdd(iso, name); }

        // Every other ISO 3166 region the runtime knows, by its English name, so
        // catalogs that list countries by name (e.g. DAAD's origin lists) resolve
        // even where the curated list above does not cover them.
        for (var a = 'A'; a <= 'Z'; a++)
        {
            for (var b = 'A'; b <= 'Z'; b++)
            {
                var code = $"{a}{b}";
                RegionInfo region;
                try { region = new RegionInfo(code); }
                catch (ArgumentException) { continue; }
                if (region.TwoLetterISORegionName != code) continue;
                map.TryAdd(Fold(region.EnglishName), code);
                // "Antigua & Barbuda" / "St. Lucia" are also written out in full.
                map.TryAdd(Fold(region.EnglishName.Replace("&", "and", StringComparison.Ordinal)
                    .Replace("St. ", "Saint ", StringComparison.Ordinal)), code);
                EnglishByIso.TryAdd(code, region.EnglishName);
            }
        }
        foreach (var (alias, iso) in new (string, string)[]
        {
            ("Palestinian Territories", "PS"), ("Tahiti", "PF"), ("Bermudas", "BM"),
            ("Brunei Darussalam", "BN"), ("Cape Verde", "CV"), ("Côte d'Ivoire", "CI"),
            ("Cote d'Ivoire", "CI"), ("Ivory Coast", "CI"),
            ("Democratic Republic of the Congo", "CD"), ("DR Congo", "CD"), ("Eswatini", "SZ"),
            ("Eswatani", "SZ"), ("Swaziland", "SZ"), ("French Guiana", "GF"), ("French Guayana", "GF"),
            ("Hong Kong", "HK"), ("Macao", "MO"), ("Macau", "MO"),
            ("Lao People's Democratic Republic", "LA"), ("Laos", "LA"),
            ("Sao Tome and Principe", "ST"), ("Saint Vincent and the Grenadines", "VC"),
        })
            map.TryAdd(Fold(alias), iso);
        return map;
    }
}
