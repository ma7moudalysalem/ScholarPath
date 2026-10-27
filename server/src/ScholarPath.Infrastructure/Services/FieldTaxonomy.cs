namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Where a student's major sits in the canonical field vocabulary.
/// </summary>
/// <remarks>
/// A listing names the broad fields it funds ("Medicine &amp; Health"); a student's
/// profile names a major in free text ("Nursing"). Compared as strings the two
/// rarely meet, so a nursing student read "no" on every medical award. This table
/// places the common majors under the fields they belong to, in English and
/// Arabic. A major it does not know falls back to word overlap in the checker.
/// The seeder places its generated majors with this table too.
/// </remarks>
public static class FieldTaxonomy
{
    private static readonly (string En, string Ar, string[] Fields)[] Majors =
    [
        ("Computer Science", "علوم الحاسب", ["Computer Science"]),
        ("Software Engineering", "هندسة البرمجيات", ["Computer Science", "Engineering"]),
        ("Artificial Intelligence", "الذكاء الاصطناعي", ["Computer Science"]),
        ("Data Science", "علم البيانات", ["Computer Science", "Mathematics & Statistics"]),
        ("Cybersecurity", "الأمن السيبراني", ["Computer Science"]),
        ("Electrical Engineering", "الهندسة الكهربائية", ["Engineering"]),
        ("Mechanical Engineering", "الهندسة الميكانيكية", ["Engineering"]),
        ("Civil Engineering", "الهندسة المدنية", ["Engineering"]),
        ("Renewable Energy", "الطاقة المتجددة", ["Engineering", "Agriculture & Environment"]),
        ("Mathematics", "الرياضيات", ["Mathematics & Statistics"]),
        ("Physics", "الفيزياء", ["Natural Sciences"]),
        ("Environmental Science", "العلوم البيئية", ["Agriculture & Environment", "Natural Sciences"]),
        ("Medicine", "الطب", ["Medicine & Health"]),
        ("Pharmacy", "الصيدلة", ["Medicine & Health"]),
        ("Public Health", "الصحة العامة", ["Medicine & Health"]),
        ("Nursing", "التمريض", ["Medicine & Health"]),
        ("Dentistry", "طب الأسنان", ["Medicine & Health"]),
        ("Biotechnology", "التقنية الحيوية", ["Natural Sciences", "Medicine & Health"]),
        ("Business Administration", "إدارة الأعمال", ["Business & Management"]),
        ("Finance", "التمويل", ["Economics & Finance"]),
        ("Economics", "الاقتصاد", ["Economics & Finance"]),
        ("Marketing", "التسويق", ["Business & Management", "Media & Communications"]),
        ("Accounting", "المحاسبة", ["Economics & Finance"]),
        ("Entrepreneurship", "ريادة الأعمال", ["Business & Management"]),
        ("Law", "القانون", ["Law"]),
        ("Education", "التربية والتعليم", ["Education"]),
        ("Architecture", "العمارة", ["Architecture & Design"]),
        ("Fine Arts", "الفنون الجميلة", ["Arts & Humanities"]),
        ("Graphic Design", "التصميم الجرافيكي", ["Architecture & Design", "Media & Communications"]),
        ("Literature", "الأدب", ["Arts & Humanities"]),
        ("Psychology", "علم النفس", ["Social Sciences"]),
        ("International Relations", "العلاقات الدولية", ["Social Sciences"]),
    ];

    private static readonly Dictionary<string, string[]> FieldsByMajor = Majors
        .SelectMany(m => new[] { (Key: m.En, m.Fields), (Key: m.Ar, m.Fields) })
        .ToDictionary(x => x.Key, x => x.Fields, StringComparer.OrdinalIgnoreCase);

    /// <summary>The canonical fields a major belongs to; empty when the major is not known.</summary>
    public static IReadOnlyList<string> FieldsOf(string major) =>
        FieldsByMajor.TryGetValue(major.Trim(), out var fields) ? fields : [];
}
