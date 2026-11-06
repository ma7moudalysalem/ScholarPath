using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Services;

namespace ScholarPath.Infrastructure.Persistence.Seed;

/// <summary>
/// Generates the bulk user roster — students, consultants and companies — so
/// every list view and the consultant marketplace look realistic at scale.
/// </summary>
public static partial class DbSeeder
{
    private const string GenUserDomain = "@demo.scholarpath.local";

    private static readonly string[] GenFirstNames =
    [
        "Omar", "Ahmed", "Mohamed", "Youssef", "Khaled", "Tarek", "Karim", "Hassan",
        "Mostafa", "Amir", "Ziad", "Nader", "Sami", "Adel", "Fadi", "Rami", "Hadi",
        "Bilal", "Sara", "Lina", "Hana", "Nour", "Mariam", "Salma", "Yasmin", "Dina",
        "Rana", "Aya", "Farida", "Malak", "Habiba", "Reem", "Layla", "Nadia", "Aisha",
        "Fatima", "Zainab", "James", "Emma", "Daniel", "Sophia", "Liam", "Olivia", "Noah",
    ];

    private static readonly string[] GenLastNames =
    [
        "Mostafa", "Khalil", "Haddad", "Nabil", "Mansour", "Farouk", "Saleh", "Abdullah",
        "Carter", "Hassan", "Ibrahim", "Sayed", "Fahmy", "Othman", "Darwish", "Shahin",
        "Naguib", "Rashed", "Halabi", "Qassem", "Barakat", "Zayed", "Mahfouz", "Younis",
        "Habib", "Sabbagh", "Khoury", "Aziz", "Mourad", "Fawzy", "Gaber", "Hamdan",
        "Selim", "Bishara", "Tawfik", "Lutfi", "Sharaf", "Nasser", "Kamal", "Helmy",
    ];

    private static readonly string[] GenUserCountries =
        ["EG", "JO", "AE", "SA", "MA", "KW", "QA", "LB", "US", "GB", "CA", "DE", "FR"];

    private static readonly (string En, string Ar)[] GenConsultantBios =
    [
        ("Former admissions officer guiding students into top universities — statements of purpose, school selection and interview preparation.",
         "مسؤول قبول سابق يرشد الطلاب نحو أفضل الجامعات — خطابات الغرض واختيار الجامعة والتحضير للمقابلات."),
        ("Scholarship strategist for fully funded Master's and PhD programmes; hundreds of students mentored across the region.",
         "خبير استراتيجيات منح للبرامج الممولة بالكامل للماجستير والدكتوراه؛ أرشد مئات الطلاب في المنطقة."),
        ("Study-abroad coach focused on STEM applicants — CV review, recommendation strategy and funding applications.",
         "مدرّب للدراسة بالخارج متخصص في طلاب التخصصات العلمية — مراجعة السيرة الذاتية واستراتيجية التوصيات وطلبات التمويل."),
        ("PhD holder supporting research-proposal writing and PostDoc / fellowship applications for North America and Europe.",
         "حاصل على الدكتوراه يدعم كتابة المقترحات البحثية وطلبات ما بعد الدكتوراه والزمالات لأمريكا الشمالية وأوروبا."),
        ("Career mentor helping undergraduates plan competitive scholarship applications from the very first step.",
         "مرشد مهني يساعد طلاب البكالوريوس على التخطيط لطلبات منح تنافسية منذ الخطوة الأولى."),
        ("Admissions consultant specialising in UK and European universities, with a strong record of funded offers.",
         "مستشار قبول متخصص في الجامعات البريطانية والأوروبية، بسجل قوي من العروض المموّلة."),
        ("Education advisor focused on Gulf and North-American scholarship opportunities for Arabic-speaking students.",
         "مستشار تعليمي يركّز على فرص المنح في الخليج وأمريكا الشمالية للطلاب الناطقين بالعربية."),
        ("Bilingual mentor supporting students through every stage of the application — search, writing, interview and decision.",
         "مرشد ثنائي اللغة يدعم الطلاب في كل مراحل التقديم — البحث والكتابة والمقابلة واتخاذ القرار."),
    ];

    private static readonly string[] GenExpertiseSets =
    [
        """["Statement of Purpose","Interview Prep","University Selection"]""",
        """["Fully Funded Scholarships","PhD Applications","UK Admissions"]""",
        """["CV Review","STEM Applications","Funding Strategy"]""",
        """["Research Proposals","PostDoc Applications","Fellowships"]""",
        """["Scholarship Search","Application Review","Personal Statements"]""",
        """["University Selection","Visa Guidance","Funding Strategy"]""",
    ];

    /// <summary>
    /// Tops the user roster up to its per-role targets. Idempotent and
    /// resumable: each role counts the generated rows it already has (by the
    /// demo email domain) and creates only the shortfall, so an interrupted
    /// run is safely completed on the next startup.
    /// </summary>
    private static async Task SeedGeneratedUsersAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        CancellationToken ct)
    {
        const int targetStudents = 1700;
        const int targetConsultants = 250;
        const int targetCompanies = 60;

        var students = await db.Users.CountAsync(
            u => u.ActiveRole == "Student" && u.Email!.EndsWith(GenUserDomain), ct).ConfigureAwait(false);
        var consultants = await db.Users.CountAsync(
            u => u.ActiveRole == "Consultant" && u.Email!.EndsWith(GenUserDomain), ct).ConfigureAwait(false);
        var companies = await db.Users.CountAsync(
            u => u.ActiveRole == "ScholarshipProvider" && u.Email!.EndsWith(GenUserDomain), ct).ConfigureAwait(false);

        if (students >= targetStudents && consultants >= targetConsultants && companies >= targetCompanies)
        {
            return;
        }

        var rng = new Random(70123);
        var profiles = new List<UserProfile>();

        var made = 0;
        made += await GenerateRoleUsersAsync(db, userManager, "Student", "student", "Student123!",
            students, targetStudents, rng, profiles, logger, ct).ConfigureAwait(false);
        made += await GenerateRoleUsersAsync(db, userManager, "Consultant", "consultant", "Consult123!",
            consultants, targetConsultants, rng, profiles, logger, ct).ConfigureAwait(false);
        made += await GenerateRoleUsersAsync(db, userManager, "ScholarshipProvider", "company", "ScholarshipProvider123!",
            companies, targetCompanies, rng, profiles, logger, ct).ConfigureAwait(false);

        if (profiles.Count > 0)
        {
            db.UserProfiles.AddRange(profiles);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        logger.LogInformation("Seeded {N} generated users (+{P} profiles)", made, profiles.Count);
    }

    private static async Task<int> GenerateRoleUsersAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        string role, string slug, string password,
        int existing, int target,
        Random rng, List<UserProfile> profiles,
        ILogger logger, CancellationToken ct)
    {
        var created = 0;
        for (var i = existing; i < target && !ct.IsCancellationRequested; i++)
        {
            var first = GenFirstNames[rng.Next(GenFirstNames.Length)];
            var last = GenLastNames[rng.Next(GenLastNames.Length)];
            var email = $"{slug}-{i + 1}{GenUserDomain}";

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = first,
                LastName = last,
                AccountStatus = AccountStatus.Active,
                IsOnboardingComplete = true,
                ActiveRole = role,
                // Students are generated as Egyptian, the population the student profile
                // statistics below describe; the draw is kept so the sequence is unchanged.
                CountryOfResidence = GenUserCountries[rng.Next(GenUserCountries.Length)] is var drawn
                    && role == "Student" ? "EG" : drawn,
                PreferredLanguage = rng.Next(2) == 0 ? "ar" : "en",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-rng.Next(1, 400)),
            };

            var result = await userManager.CreateAsync(user, password).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                continue;
            }

            await userManager.AddToRoleAsync(user, role).ConfigureAwait(false);
            profiles.Add(BuildGeneratedProfile(user, role, rng));
            created++;
        }

        logger.LogInformation("Generated {N} {Role} users", created, role);
        return created;
    }

    private static UserProfile BuildGeneratedProfile(ApplicationUser u, string role, Random rng)
    {
        var now = DateTimeOffset.UtcNow;
        var p = new UserProfile
        {
            UserId = u.Id,
            Timezone = "Africa/Cairo",
            Nationality = u.CountryOfResidence,
            CreatedAt = now.AddDays(-rng.Next(5, 380)),
            ProfileCompletenessPercent = rng.Next(70, 100),
        };

        switch (role)
        {
            case "Consultant":
                var genBio = GenConsultantBios[rng.Next(GenConsultantBios.Length)];
                p.Biography = genBio.En;
                p.BiographyAr = genBio.Ar;
                p.SessionFeeUsd = 15m + rng.Next(0, 18) * 5m;
                p.SessionDurationMinutes = new[] { 30, 45, 60 }[rng.Next(3)];
                p.ExpertiseTagsJson = GenExpertiseSets[rng.Next(GenExpertiseSets.Length)];
                p.LanguagesJson = rng.Next(2) == 0 ? """["en","ar"]""" : """["en","ar","fr"]""";
                p.ConsultantVerifiedAt = now.AddDays(-rng.Next(20, 320));
                break;

            case "ScholarshipProvider":
                p.Biography = "An organisation funding scholarships and reviewing student applications on ScholarPath.";
                p.OrganizationLegalName = $"{u.FirstName} {u.LastName} Education";
                p.OrganizationVerificationStatus = "Verified";
                p.OrganizationVerifiedAt = now.AddDays(-rng.Next(30, 320));
                break;

            default: // Student
                // One draw from the shared generator, as before, so every value
                // generated after it is unchanged in kind.
                var major = PickStudentMajor(rng.NextDouble());
                AcademicLevel[] levels =
                    [AcademicLevel.HighSchool, AcademicLevel.Undergrad, AcademicLevel.Masters, AcademicLevel.PhD];
                p.AcademicLevel = levels[rng.Next(levels.Length)];
                p.FieldOfStudy = major;
                p.Gpa = 2.5m + rng.Next(0, 16) * 0.1m;
                p.GpaScale = "4.0";
                p.Biography = $"A {major} student seeking international scholarship opportunities and mentorship.";
                // Stated in the canonical vocabulary the listings use, so a preference
                // can actually meet a listing's field rather than miss it on wording.
                p.PreferredFieldsJson = JsonArray(FieldTaxonomy.FieldsOf(major));
                p.PreferredCountriesJson = JsonArray(GenDestinations(u.Email ?? u.Id.ToString()));
                break;
        }

        return p;
    }

    // Host countries of internationally mobile students from Egypt, UNESCO
    // Institute for Statistics, indicator 26436 ("Inbound internationally mobile
    // students from Africa: Students from Egypt"), latest year per host from 2020
    // on (2023-2025); the 20 largest hosts, 94.8 percent of the total.
    private static readonly (string Country, int Students)[] EgyptOutboundHosts =
    [
        ("RU", 15408), ("TR", 9595), ("DE", 7896), ("JO", 4945), ("US", 4069),
        ("GB", 3538), ("SA", 3509), ("FR", 2312), ("CA", 2085), ("MY", 1958),
        ("QA", 1926), ("IT", 1831), ("GE", 1053), ("HU", 781), ("AT", 546),
        ("ES", 426), ("AU", 419), ("OM", 413), ("PL", 390), ("BH", 363),
    ];

    // Share of tertiary graduates in Egypt by ISCED-F broad field, UNESCO
    // Institute for Statistics, indicators FOSGP.5T8.F110-F900 (2025), split
    // evenly across the generated majors in each field. Services (0.3 percent)
    // has no generated major and is left out.
    private static readonly (double Share, string[] Majors)[] EgyptGraduateFields =
    [
        (14.02, ["Education"]),
        (4.88, ["Fine Arts", "Literature", "Graphic Design"]),
        (11.89, ["Psychology", "International Relations", "Economics"]),
        (26.50, ["Business Administration", "Finance", "Marketing", "Accounting", "Entrepreneurship", "Law"]),
        (3.42, ["Mathematics", "Physics", "Environmental Science", "Biotechnology"]),
        (2.60, ["Computer Science", "Software Engineering", "Artificial Intelligence", "Data Science", "Cybersecurity"]),
        (8.03, ["Electrical Engineering", "Mechanical Engineering", "Civil Engineering", "Renewable Energy", "Architecture"]),
        (6.75, ["Agriculture"]),
        (17.13, ["Medicine", "Pharmacy", "Public Health", "Nursing", "Dentistry"]),
    ];

    private static string PickStudentMajor(double u)
    {
        var total = EgyptGraduateFields.Sum(f => f.Share);
        var x = u * total;
        foreach (var (share, majors) in EgyptGraduateFields)
        {
            if (x < share) return majors[(int)(x / share * majors.Length)];
            x -= share;
        }
        return EgyptGraduateFields[^1].Majors[^1];
    }

    /// <summary>
    /// One to three study destinations, as onboarding asks every student for,
    /// drawn in proportion to where students from Egypt study (above). Drawn from
    /// a generator keyed on the email rather than the shared one.
    /// </summary>
    private static List<string> GenDestinations(string key)
    {
        var hash = 2166136261u;
        foreach (var ch in key) hash = (hash ^ ch) * 16777619u;
        var rng = new Random((int)(hash & 0x7FFFFFFF));
        var total = EgyptOutboundHosts.Sum(h => h.Students);
        var picked = new List<string>();
        var count = rng.Next(1, 4);
        while (picked.Count < count)
        {
            var x = rng.Next(total);
            foreach (var (country, students) in EgyptOutboundHosts)
            {
                if (x < students) { if (!picked.Contains(country)) picked.Add(country); break; }
                x -= students;
            }
        }
        return picked;
    }
}
