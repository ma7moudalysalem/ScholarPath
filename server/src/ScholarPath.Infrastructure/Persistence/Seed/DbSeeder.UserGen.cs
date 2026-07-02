using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

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
                CountryOfResidence = GenUserCountries[rng.Next(GenUserCountries.Length)],
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
                var field = GenFields[rng.Next(GenFields.Length)];
                AcademicLevel[] levels =
                    [AcademicLevel.HighSchool, AcademicLevel.Undergrad, AcademicLevel.Masters, AcademicLevel.PhD];
                p.AcademicLevel = levels[rng.Next(levels.Length)];
                p.FieldOfStudy = field.En;
                p.Gpa = 2.5m + rng.Next(0, 16) * 0.1m;
                p.GpaScale = "4.0";
                p.Biography = $"A {field.En} student seeking international scholarship opportunities and mentorship.";
                p.PreferredFieldsJson = $"[\"{field.En}\"]";
                break;
        }

        return p;
    }
}
