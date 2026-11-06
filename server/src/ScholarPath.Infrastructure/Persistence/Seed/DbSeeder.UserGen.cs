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
                // A student's nationality follows the origins of internationally
                // mobile Arab students (StudentOrigins below); the shared draw is kept
                // so the sequence of every other generated value is unchanged.
                CountryOfResidence = GenUserCountries[rng.Next(GenUserCountries.Length)] is var drawn
                    && role == "Student" ? StudentOrigin(email).Country : drawn,
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
                var origin = StudentOrigin(u.Email ?? u.Id.ToString());
                var major = PickStudentMajor(rng.NextDouble(), origin);
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
                p.PreferredCountriesJson = JsonArray(GenDestinations(u.Email ?? u.Id.ToString(), origin));
                break;
        }

        return p;
    }

    // Internationally mobile tertiary students from twelve Arab countries, UNESCO
    // Institute for Statistics: for each origin, the total abroad and the 20
    // largest host countries ("Inbound internationally mobile students from
    // Africa/Asia: Students from <origin>", latest year per host from 2020 on),
    // and the share of the origin's tertiary graduates by ISCED-F broad field
    // (FOSGP.5T8.F110-F900, latest year). Arab origins without recent data for
    // both are left out.
    private sealed record Origin(string Country, int Students, double[] FieldShares, (string Country, int Students)[] Hosts);

    private static readonly Origin[] StudentOrigins =
    [
        new("EG", 66939, [14.02, 4.88, 11.89, 26.50, 3.42, 2.60, 8.03, 6.75, 17.13], // fields 2025
            [("RU", 15408), ("TR", 9595), ("DE", 7896), ("JO", 4945), ("US", 4069), ("GB", 3538), ("SA", 3509), ("FR", 2312), ("CA", 2085), ("MY", 1958), ("QA", 1926), ("IT", 1831), ("GE", 1053), ("HU", 781), ("AT", 546), ("ES", 426), ("AU", 419), ("OM", 413), ("PL", 390), ("BH", 363)]),
        new("MA", 73635, [9.80, 11.74, 10.71, 28.19, 7.08, 9.71, 12.96, 0.66, 7.21], // fields 2024
            [("FR", 33889), ("DE", 5554), ("ES", 5151), ("TR", 4958), ("CA", 3750), ("RU", 3712), ("UA", 2642), ("IT", 2085), ("BE", 1971), ("GB", 1628), ("US", 1458), ("RO", 1395), ("SN", 1059), ("CH", 824), ("PL", 509), ("HU", 416), ("SA", 368), ("TN", 298), ("FI", 232), ("QA", 188)]),
        new("SA", 43806, [2.42, 12.85, 2.94, 38.09, 7.78, 13.00, 10.75, 0.24, 6.53], // fields 2024
            [("US", 16337), ("GB", 8928), ("AU", 2931), ("JO", 2370), ("MY", 2205), ("BH", 1879), ("CA", 1215), ("DE", 1025), ("QA", 1016), ("IR", 866), ("IN", 682), ("IE", 653), ("EG", 567), ("FR", 519), ("TR", 351), ("ES", 323), ("IT", 244), ("PL", 180), ("RU", 162), ("OM", 119)]),
        new("DZ", 40827, [1.88, 14.05, 16.72, 29.11, 11.04, 4.74, 15.12, 1.53, 2.89], // fields 2024
            [("FR", 27022), ("CA", 2355), ("TR", 2196), ("RU", 1558), ("GB", 933), ("ES", 807), ("DE", 668), ("PL", 552), ("SA", 536), ("BE", 507), ("RO", 384), ("IT", 364), ("TN", 338), ("MY", 327), ("US", 296), ("QA", 246), ("HU", 215), ("PT", 206), ("IE", 141), ("CH", 131)]),
        new("JO", 30988, [11.27, 16.48, 3.29, 18.92, 3.49, 8.25, 10.33, 0.95, 25.94], // fields 2024
            [("TR", 7266), ("SA", 2771), ("US", 2502), ("GE", 2487), ("DE", 2126), ("QA", 1876), ("GB", 1791), ("RU", 1605), ("MY", 1402), ("HU", 1053), ("CA", 891), ("EG", 607), ("ES", 417), ("RO", 365), ("AU", 325), ("BH", 318), ("KZ", 280), ("AM", 241), ("DZ", 231), ("OM", 213)]),
        new("TN", 30772, [4.68, 12.91, 5.50, 26.40, 6.42, 14.86, 16.64, 0.96, 10.54], // fields 2023
            [("FR", 11602), ("DE", 7254), ("IT", 2040), ("CA", 1416), ("TR", 1263), ("RU", 1084), ("RO", 672), ("US", 608), ("CH", 597), ("HU", 492), ("MA", 437), ("ES", 339), ("BE", 308), ("QA", 249), ("PT", 225), ("GB", 209), ("UA", 207), ("SA", 195), ("PL", 158), ("MY", 152)]),
        new("LB", 30226, [4.80, 12.94, 6.28, 26.50, 7.77, 8.85, 13.95, 2.32, 13.05], // fields 2023
            [("FR", 9866), ("IR", 2166), ("DE", 2133), ("US", 1861), ("TR", 1743), ("IT", 1738), ("CA", 1500), ("GB", 1407), ("RU", 1005), ("GE", 981), ("CH", 555), ("BY", 505), ("BE", 472), ("SA", 390), ("AU", 370), ("RO", 370), ("ES", 361), ("QA", 298), ("CY", 280), ("UA", 254)]),
        new("PS", 29458, [16.13, 10.16, 5.44, 28.23, 2.93, 6.46, 8.78, 0.49, 20.09], // fields 2023
            [("JO", 8728), ("TR", 5705), ("SA", 4433), ("QA", 1463), ("DE", 1462), ("DZ", 1111), ("EG", 1109), ("MY", 823), ("RU", 664), ("US", 451), ("TN", 379), ("RO", 299), ("MA", 255), ("HU", 245), ("GB", 232), ("CU", 228), ("FR", 188), ("IT", 152), ("GR", 137), ("CA", 132)]),
        new("OM", 10937, [21.29, 6.13, 1.68, 28.92, 3.65, 9.21, 17.45, 0.49, 6.63], // fields 2024
            [("JO", 2401), ("GB", 2269), ("US", 2141), ("MY", 967), ("IN", 589), ("IE", 351), ("CA", 264), ("QA", 262), ("AU", 249), ("MA", 211), ("BH", 200), ("DE", 178), ("SA", 132), ("NZ", 74), ("CY", 72), ("FR", 61), ("TN", 52), ("IR", 46), ("TR", 42), ("HU", 41)]),
        new("MR", 8310, [9.76, 22.27, 14.25, 12.99, 27.28, 5.14, 2.13, 2.75, 1.78], // fields 2020
            [("MA", 1647), ("SN", 1633), ("TN", 1143), ("FR", 840), ("TR", 803), ("DZ", 708), ("SA", 408), ("RW", 314), ("RU", 115), ("DE", 102), ("QA", 95), ("CA", 72), ("EG", 72), ("MY", 68), ("GB", 50), ("US", 43), ("CI", 40), ("ES", 27), ("GN", 14), ("IR", 14)]),
        new("QA", 6343, [7.90, 7.99, 14.91, 37.65, 2.61, 6.12, 13.39, 0.00, 8.42], // fields 2024
            [("GB", 2591), ("IN", 750), ("JO", 689), ("US", 434), ("MY", 320), ("FR", 224), ("DE", 191), ("AU", 162), ("MA", 125), ("ES", 110), ("CA", 87), ("IE", 67), ("PL", 65), ("BG", 59), ("IT", 46), ("OM", 45), ("CH", 41), ("CY", 39), ("SA", 39), ("TR", 30)]),
        new("BH", 4692, [7.87, 10.89, 4.65, 42.89, 2.48, 5.60, 10.40, 0.16, 10.59], // fields 2024
            [("GB", 1386), ("IR", 527), ("JO", 432), ("US", 393), ("SA", 370), ("QA", 307), ("IN", 301), ("DE", 108), ("MY", 103), ("AU", 101), ("OM", 75), ("CA", 69), ("EG", 69), ("TR", 64), ("IE", 47), ("RU", 45), ("FR", 43), ("MA", 37), ("ES", 22), ("IT", 21)]),
    ];

    // The generated majors in each ISCED-F broad field, in the order of FieldShares.
    // Services (F1000) has no generated major and is left out.
    private static readonly string[][] MajorsByField =
    [
        ["Education"],
        ["Fine Arts", "Literature", "Graphic Design"],
        ["Psychology", "International Relations", "Economics"],
        ["Business Administration", "Finance", "Marketing", "Accounting", "Entrepreneurship", "Law"],
        ["Mathematics", "Physics", "Environmental Science", "Biotechnology"],
        ["Computer Science", "Software Engineering", "Artificial Intelligence", "Data Science", "Cybersecurity"],
        ["Electrical Engineering", "Mechanical Engineering", "Civil Engineering", "Renewable Energy", "Architecture"],
        ["Agriculture"],
        ["Medicine", "Pharmacy", "Public Health", "Nursing", "Dentistry"],
    ];

    // A generator keyed on the email, so a student's origin, destinations and the
    // shared sequence are independent of each other.
    private static Random KeyedRandom(string key, uint salt)
    {
        var hash = 2166136261u ^ salt;
        foreach (var ch in key) hash = (hash ^ ch) * 16777619u;
        return new Random((int)(hash & 0x7FFFFFFF));
    }

    private static Origin StudentOrigin(string key)
    {
        var x = KeyedRandom(key, 0x9e3779b9).Next(StudentOrigins.Sum(o => o.Students));
        foreach (var o in StudentOrigins)
        {
            if (x < o.Students) return o;
            x -= o.Students;
        }
        return StudentOrigins[^1];
    }

    private static string PickStudentMajor(double u, Origin origin)
    {
        var x = u * origin.FieldShares.Sum();
        for (var f = 0; f < MajorsByField.Length; f++)
        {
            var share = origin.FieldShares[f];
            if (x < share) return MajorsByField[f][(int)(x / share * MajorsByField[f].Length)];
            x -= share;
        }
        return MajorsByField[^1][^1];
    }

    /// <summary>
    /// One to three study destinations, as onboarding asks every student for,
    /// drawn in proportion to where students from the student's own country study.
    /// </summary>
    private static List<string> GenDestinations(string key, Origin origin)
    {
        var rng = KeyedRandom(key, 0);
        var total = origin.Hosts.Sum(h => h.Students);
        var picked = new List<string>();
        var count = Math.Min(rng.Next(1, 4), origin.Hosts.Length);
        while (picked.Count < count)
        {
            var x = rng.Next(total);
            foreach (var (country, students) in origin.Hosts)
            {
                if (x < students) { if (!picked.Contains(country)) picked.Add(country); break; }
                x -= students;
            }
        }
        return picked;
    }
}
