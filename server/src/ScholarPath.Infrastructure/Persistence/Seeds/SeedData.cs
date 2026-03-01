using Microsoft.EntityFrameworkCore;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Seeds;

public static class SeedData
{
    // ── Fixed GUIDs ──────────────────────────────────────────────────────

    // Users
    private static readonly Guid AdminId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid StudentId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid ConsultantId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid CompanyId = Guid.Parse("10000000-0000-0000-0000-000000000004");

    // Categories
    private static readonly Guid CatStem = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid CatArts = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid CatBusiness = Guid.Parse("20000000-0000-0000-0000-000000000003");
    private static readonly Guid CatMedicine = Guid.Parse("20000000-0000-0000-0000-000000000004");
    private static readonly Guid CatEngineering = Guid.Parse("20000000-0000-0000-0000-000000000005");
    private static readonly Guid CatLaw = Guid.Parse("20000000-0000-0000-0000-000000000006");
    private static readonly Guid CatEducation = Guid.Parse("20000000-0000-0000-0000-000000000007");
    private static readonly Guid CatSocialSciences = Guid.Parse("20000000-0000-0000-0000-000000000008");

    // Scholarships
    private static readonly Guid Scholarship1 = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid Scholarship2 = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid Scholarship3 = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly Guid Scholarship4 = Guid.Parse("30000000-0000-0000-0000-000000000004");
    private static readonly Guid Scholarship5 = Guid.Parse("30000000-0000-0000-0000-000000000005");

    // User Profiles
    private static readonly Guid ProfileStudent = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid ProfileConsultant = Guid.Parse("40000000-0000-0000-0000-000000000002");

    // Resources
    private static readonly Guid Resource1 = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid Resource2 = Guid.Parse("50000000-0000-0000-0000-000000000002");
    private static readonly Guid Resource3 = Guid.Parse("50000000-0000-0000-0000-000000000003");
    private static readonly Guid Resource4 = Guid.Parse("50000000-0000-0000-0000-000000000004");

    // Groups
    private static readonly Guid Group1 = Guid.Parse("60000000-0000-0000-0000-000000000001");
    private static readonly Guid Group2 = Guid.Parse("60000000-0000-0000-0000-000000000002");

    // Notifications
    private static readonly Guid Notification1 = Guid.Parse("70000000-0000-0000-0000-000000000001");
    private static readonly Guid Notification2 = Guid.Parse("70000000-0000-0000-0000-000000000002");

    private static readonly DateTime SeedDate = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    // ── Public Entry Point ───────────────────────────────────────────────

    public static void Apply(ModelBuilder builder)
    {
        SeedUsers(builder);
        SeedCategories(builder);
        SeedUserProfiles(builder);
        SeedScholarships(builder);
        SeedResources(builder);
        SeedGroups(builder);
        SeedNotifications(builder);
    }

    // ── Users ────────────────────────────────────────────────────────────

    // Pre-computed password hashes (static to avoid PendingModelChangesWarning).
    // Passwords: Admin@123456, Student@123456, Consultant@123456, Company@123456
    private const string AdminPasswordHash = "AQAAAAIAAYagAAAAEBzopFBef9EbyXq08be+PDy9bpasNFbYqmiSYXxvLVk3ydlcbIBNF4KeMFkqu9/OTQ==";
    private const string StudentPasswordHash = "AQAAAAIAAYagAAAAENnfSCXEfIvwmiulLqrD69j7vHG3dq+P/5wbiM/k9bbUZWruE2CSXXWvcSqYkj7Niw==";
    private const string ConsultantPasswordHash = "AQAAAAIAAYagAAAAEITdr5Aifj2zBzl4OM+yP6AWgGMc3zPUSFi6HUXZjzY4WXk9/CvMASxZIx8r5cEekg==";
    private const string CompanyPasswordHash = "AQAAAAIAAYagAAAAEPrDShx08aOmDP6z/OLyYXfyF7pvJM1EDbsV9wicesVdlZqTNVN3k/XtUobxDoLQqg==";

    private static void SeedUsers(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>().HasData(
            new ApplicationUser
            {
                Id = AdminId,
                FirstName = "System",
                LastName = "Admin",
                UserName = "admin@scholarpath.com",
                NormalizedUserName = "ADMIN@SCHOLARPATH.COM",
                Email = "admin@scholarpath.com",
                NormalizedEmail = "ADMIN@SCHOLARPATH.COM",
                EmailConfirmed = true,
                PasswordHash = AdminPasswordHash,
                SecurityStamp = "ADMIN-SECURITY-STAMP-00000001",
                ConcurrencyStamp = "ADMIN-CONCURRENCY-STAMP-00001",
                Role = UserRole.Admin,
                AccountStatus = AccountStatus.Active,
                IsOnboardingComplete = true,
                IsActive = true,
                CreatedAt = SeedDate
            },
            new ApplicationUser
            {
                Id = StudentId,
                FirstName = "Ahmed",
                LastName = "Hassan",
                UserName = "ahmed.student@scholarpath.com",
                NormalizedUserName = "AHMED.STUDENT@SCHOLARPATH.COM",
                Email = "ahmed.student@scholarpath.com",
                NormalizedEmail = "AHMED.STUDENT@SCHOLARPATH.COM",
                EmailConfirmed = true,
                PasswordHash = StudentPasswordHash,
                SecurityStamp = "STUDENT-SECURITY-STAMP-0000002",
                ConcurrencyStamp = "STUDENT-CONCURRENCY-STAMP-002",
                Role = UserRole.Student,
                AccountStatus = AccountStatus.Active,
                IsOnboardingComplete = true,
                IsActive = true,
                CreatedAt = SeedDate
            },
            new ApplicationUser
            {
                Id = ConsultantId,
                FirstName = "Sara",
                LastName = "Mohamed",
                UserName = "sara.consultant@scholarpath.com",
                NormalizedUserName = "SARA.CONSULTANT@SCHOLARPATH.COM",
                Email = "sara.consultant@scholarpath.com",
                NormalizedEmail = "SARA.CONSULTANT@SCHOLARPATH.COM",
                EmailConfirmed = true,
                PasswordHash = ConsultantPasswordHash,
                SecurityStamp = "CONSULT-SECURITY-STAMP-000003",
                ConcurrencyStamp = "CONSULT-CONCURRENCY-STAMP-03",
                Role = UserRole.Consultant,
                AccountStatus = AccountStatus.Active,
                IsOnboardingComplete = true,
                IsActive = true,
                CreatedAt = SeedDate
            },
            new ApplicationUser
            {
                Id = CompanyId,
                FirstName = "Omar",
                LastName = "Khalil",
                UserName = "omar.company@scholarpath.com",
                NormalizedUserName = "OMAR.COMPANY@SCHOLARPATH.COM",
                Email = "omar.company@scholarpath.com",
                NormalizedEmail = "OMAR.COMPANY@SCHOLARPATH.COM",
                EmailConfirmed = true,
                PasswordHash = CompanyPasswordHash,
                SecurityStamp = "COMPANY-SECURITY-STAMP-000004",
                ConcurrencyStamp = "COMPANY-CONCURRENCY-STAMP-004",
                Role = UserRole.Company,
                AccountStatus = AccountStatus.Active,
                IsOnboardingComplete = true,
                IsActive = true,
                CreatedAt = SeedDate
            }
        );
    }

    // ── Categories ───────────────────────────────────────────────────────

    private static void SeedCategories(ModelBuilder builder)
    {
        builder.Entity<Category>().HasData(
            new Category
            {
                Id = CatStem,
                Name = "STEM",
                NameAr = "العلوم والتكنولوجيا",
                Description = "Science, Technology, Engineering, and Mathematics scholarships",
                DescriptionAr = "منح العلوم والتكنولوجيا والهندسة والرياضيات",
                CreatedAt = SeedDate
            },
            new Category
            {
                Id = CatArts,
                Name = "Arts & Humanities",
                NameAr = "الفنون والعلوم الإنسانية",
                Description = "Scholarships for arts, literature, history, and philosophy",
                DescriptionAr = "منح الفنون والأدب والتاريخ والفلسفة",
                CreatedAt = SeedDate
            },
            new Category
            {
                Id = CatBusiness,
                Name = "Business & Economics",
                NameAr = "الأعمال والاقتصاد",
                Description = "Business administration, finance, and economics scholarships",
                DescriptionAr = "منح إدارة الأعمال والمالية والاقتصاد",
                CreatedAt = SeedDate
            },
            new Category
            {
                Id = CatMedicine,
                Name = "Medicine & Health Sciences",
                NameAr = "الطب والعلوم الصحية",
                Description = "Medical, nursing, pharmacy, and public health scholarships",
                DescriptionAr = "منح الطب والتمريض والصيدلة والصحة العامة",
                CreatedAt = SeedDate
            },
            new Category
            {
                Id = CatEngineering,
                Name = "Engineering",
                NameAr = "الهندسة",
                Description = "Civil, mechanical, electrical, and software engineering scholarships",
                DescriptionAr = "منح الهندسة المدنية والميكانيكية والكهربائية وهندسة البرمجيات",
                CreatedAt = SeedDate
            },
            new Category
            {
                Id = CatLaw,
                Name = "Law",
                NameAr = "القانون",
                Description = "Legal studies and international law scholarships",
                DescriptionAr = "منح الدراسات القانونية والقانون الدولي",
                CreatedAt = SeedDate
            },
            new Category
            {
                Id = CatEducation,
                Name = "Education",
                NameAr = "التعليم",
                Description = "Teaching, pedagogy, and educational leadership scholarships",
                DescriptionAr = "منح التدريس والتربية والقيادة التعليمية",
                CreatedAt = SeedDate
            },
            new Category
            {
                Id = CatSocialSciences,
                Name = "Social Sciences",
                NameAr = "العلوم الاجتماعية",
                Description = "Psychology, sociology, political science, and anthropology scholarships",
                DescriptionAr = "منح علم النفس والاجتماع والعلوم السياسية والأنثروبولوجيا",
                CreatedAt = SeedDate
            }
        );
    }

    // ── User Profiles ────────────────────────────────────────────────────

    private static void SeedUserProfiles(ModelBuilder builder)
    {
        builder.Entity<UserProfile>().HasData(
            new UserProfile
            {
                Id = ProfileStudent,
                UserId = StudentId,
                FieldOfStudy = "Computer Science",
                GPA = 3.75m,
                Interests = "[\"AI\",\"Machine Learning\",\"Web Development\"]",
                Country = "Egypt",
                TargetCountry = "Germany",
                Bio = "Passionate computer science student seeking international scholarship opportunities.",
                CreatedAt = SeedDate
            },
            new UserProfile
            {
                Id = ProfileConsultant,
                UserId = ConsultantId,
                FieldOfStudy = "International Education",
                Country = "Egypt",
                Bio = "Experienced scholarship consultant with 10+ years helping students secure funding.",
                CreatedAt = SeedDate
            }
        );
    }

    // ── Scholarships ─────────────────────────────────────────────────────

    private static void SeedScholarships(ModelBuilder builder)
    {
        builder.Entity<Scholarship>().HasData(
            new Scholarship
            {
                Id = Scholarship1,
                Title = "DAAD Graduate Scholarship",
                TitleAr = "منحة DAAD للدراسات العليا",
                Description = "The German Academic Exchange Service (DAAD) offers fully funded scholarships for international students pursuing Master's or PhD degrees in Germany. The scholarship covers tuition, monthly stipend, health insurance, and travel costs.",
                DescriptionAr = "تقدم هيئة التبادل الأكاديمي الألمانية (DAAD) منحًا ممولة بالكامل للطلاب الدوليين لمتابعة درجة الماجستير أو الدكتوراه في ألمانيا. تغطي المنحة الرسوم الدراسية والراتب الشهري والتأمين الصحي وتكاليف السفر.",
                Country = "Germany",
                FieldOfStudy = "All Fields",
                FundingType = ScholarshipFundingType.FullyFunded,
                DegreeLevel = DegreeLevel.Masters,
                AwardAmount = 1200m,
                Currency = "EUR",
                Deadline = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
                EligibilityDescription = "Open to graduates from developing countries with excellent academic records. Applicants must hold a Bachelor's degree and have at least 2 years of work experience.",
                RequiredDocuments = "CV, motivation letter, academic transcripts, recommendation letters, language certificate",
                OfficialLink = "https://www.daad.de/en/study-and-research-in-germany/scholarships/",
                IsActive = true,
                MinGPA = 3.0m,
                EligibleCountries = "[\"Egypt\",\"Jordan\",\"Morocco\",\"Tunisia\",\"Lebanon\"]",
                EligibleMajors = "[\"Computer Science\",\"Engineering\",\"Natural Sciences\",\"Mathematics\"]",
                CategoryId = CatStem,
                CreatedAt = SeedDate
            },
            new Scholarship
            {
                Id = Scholarship2,
                Title = "Chevening Scholarship",
                TitleAr = "منحة تشيفنينج",
                Description = "Chevening is the UK government's international awards programme offering fully funded Master's degrees at any UK university. It provides a unique opportunity for future leaders and decision-makers.",
                DescriptionAr = "تشيفنينج هو برنامج المنح الدولي للحكومة البريطانية الذي يقدم درجات ماجستير ممولة بالكامل في أي جامعة بريطانية. يوفر فرصة فريدة للقادة وصناع القرار المستقبليين.",
                Country = "United Kingdom",
                FieldOfStudy = "All Fields",
                FundingType = ScholarshipFundingType.FullyFunded,
                DegreeLevel = DegreeLevel.Masters,
                AwardAmount = 18000m,
                Currency = "GBP",
                Deadline = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                EligibilityDescription = "Applicants must have at least 2 years of work experience, hold an undergraduate degree, and return to their home country for at least 2 years after the scholarship.",
                RequiredDocuments = "Personal statement, reference letters, academic transcripts, English language test",
                OfficialLink = "https://www.chevening.org/",
                IsActive = true,
                MinGPA = 3.0m,
                EligibleCountries = "[\"Egypt\",\"Jordan\",\"Iraq\",\"Saudi Arabia\",\"UAE\"]",
                CategoryId = CatBusiness,
                CreatedAt = SeedDate
            },
            new Scholarship
            {
                Id = Scholarship3,
                Title = "Turkish Government Scholarship (Turkiye Burslari)",
                TitleAr = "المنحة التركية الحكومية",
                Description = "The Turkish Government offers fully funded scholarships for international students at all academic levels. The program includes Turkish language preparation, tuition waiver, monthly stipend, accommodation, and health insurance.",
                DescriptionAr = "تقدم الحكومة التركية منحًا ممولة بالكامل للطلاب الدوليين في جميع المراحل الأكاديمية. يشمل البرنامج إعداد اللغة التركية والإعفاء من الرسوم والراتب الشهري والسكن والتأمين الصحي.",
                Country = "Turkey",
                FieldOfStudy = "All Fields",
                FundingType = ScholarshipFundingType.FullyFunded,
                DegreeLevel = DegreeLevel.Bachelors,
                AwardAmount = 800m,
                Currency = "TRY",
                Deadline = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc),
                EligibilityDescription = "Open to citizens of all countries except Turkey. Undergraduate applicants must be under 21, Master's under 30, and PhD under 35.",
                RequiredDocuments = "National ID, photo, high school diploma, transcripts, language certificate (optional)",
                OfficialLink = "https://www.turkiyeburslari.gov.tr/",
                IsActive = true,
                MaxAge = 21,
                EligibleCountries = "[\"Egypt\",\"Palestine\",\"Syria\",\"Yemen\",\"Somalia\"]",
                CategoryId = CatEducation,
                CreatedAt = SeedDate
            },
            new Scholarship
            {
                Id = Scholarship4,
                Title = "Fulbright Foreign Student Program",
                TitleAr = "برنامج فولبرايت للطلاب الأجانب",
                Description = "The Fulbright Program provides grants for graduate students to study, conduct research, or teach English in the United States. It is one of the most prestigious scholarship programs in the world.",
                DescriptionAr = "يقدم برنامج فولبرايت منحًا لطلاب الدراسات العليا للدراسة أو إجراء البحوث أو تدريس اللغة الإنجليزية في الولايات المتحدة. وهو واحد من أكثر برامج المنح الدراسية المرموقة في العالم.",
                Country = "United States",
                FieldOfStudy = "All Fields",
                FundingType = ScholarshipFundingType.FullyFunded,
                DegreeLevel = DegreeLevel.Masters,
                AwardAmount = 35000m,
                Currency = "USD",
                Deadline = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
                EligibilityDescription = "Candidates must hold a Bachelor's degree, demonstrate English proficiency, and have a strong academic and professional record.",
                RequiredDocuments = "Application form, personal statement, study plan, recommendation letters, TOEFL/IELTS score",
                OfficialLink = "https://foreign.fulbrightonline.org/",
                IsActive = true,
                MinGPA = 3.0m,
                EligibleCountries = "[\"Egypt\",\"Lebanon\",\"Jordan\",\"Morocco\",\"Tunisia\",\"Iraq\"]",
                EligibleMajors = "[\"Public Policy\",\"Engineering\",\"Sciences\",\"Education\",\"Arts\"]",
                CategoryId = CatSocialSciences,
                CreatedAt = SeedDate
            },
            new Scholarship
            {
                Id = Scholarship5,
                Title = "Erasmus Mundus Joint Masters",
                TitleAr = "منح إيراسموس موندوس للماجستير المشترك",
                Description = "Erasmus Mundus Joint Master Degrees are prestigious integrated study programmes delivered by international consortia of higher education institutions. Students study in at least two different European countries.",
                DescriptionAr = "درجات ماجستير إيراسموس موندوس المشتركة هي برامج دراسية متكاملة مرموقة تقدمها اتحادات دولية لمؤسسات التعليم العالي. يدرس الطلاب في دولتين أوروبيتين على الأقل.",
                Country = "European Union",
                FieldOfStudy = "Multiple Fields",
                FundingType = ScholarshipFundingType.FullyFunded,
                DegreeLevel = DegreeLevel.Masters,
                AwardAmount = 25000m,
                Currency = "EUR",
                Deadline = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                EligibilityDescription = "Open to students and scholars worldwide. Applicants must hold a recognized Bachelor's degree. Partner country applicants receive higher funding.",
                RequiredDocuments = "Online application, academic transcripts, CV, motivation letter, language proficiency proof, recommendation letters",
                OfficialLink = "https://erasmus-plus.ec.europa.eu/",
                IsActive = true,
                MinGPA = 3.2m,
                EligibleCountries = "[\"Egypt\",\"Algeria\",\"Libya\",\"Sudan\",\"Mauritania\"]",
                EligibleMajors = "[\"Environmental Science\",\"Public Health\",\"Data Science\",\"Urban Planning\"]",
                CategoryId = CatEngineering,
                CreatedAt = SeedDate
            }
        );
    }

    // ── Resources ────────────────────────────────────────────────────────

    private static void SeedResources(ModelBuilder builder)
    {
        builder.Entity<Resource>().HasData(
            new Resource
            {
                Id = Resource1,
                Title = "How to Write a Winning Scholarship Essay",
                TitleAr = "كيف تكتب مقالة منحة دراسية ناجحة",
                Description = "A comprehensive guide covering structure, tone, and common mistakes to avoid when writing scholarship personal statements.",
                DescriptionAr = "دليل شامل يغطي الهيكل والأسلوب والأخطاء الشائعة التي يجب تجنبها عند كتابة بيانات المنح الشخصية.",
                Url = "https://scholarpath.com/resources/scholarship-essay-guide",
                Type = "Article",
                Category = "Application Tips",
                CreatedAt = SeedDate
            },
            new Resource
            {
                Id = Resource2,
                Title = "IELTS Preparation: Free Study Plan",
                TitleAr = "التحضير لامتحان IELTS: خطة دراسية مجانية",
                Description = "A 30-day structured study plan for IELTS preparation with practice materials and tips for each section.",
                DescriptionAr = "خطة دراسية منظمة لمدة 30 يومًا للتحضير لامتحان IELTS مع مواد تدريبية ونصائح لكل قسم.",
                Url = "https://scholarpath.com/resources/ielts-study-plan",
                Type = "Guide",
                Category = "Test Preparation",
                CreatedAt = SeedDate
            },
            new Resource
            {
                Id = Resource3,
                Title = "Top 10 Scholarship Databases for Arab Students",
                TitleAr = "أفضل 10 قواعد بيانات للمنح الدراسية للطلاب العرب",
                Description = "A curated list of the best online scholarship search engines and databases specifically useful for students from the Arab region.",
                DescriptionAr = "قائمة منسقة لأفضل محركات البحث وقواعد البيانات للمنح الدراسية المفيدة تحديدًا للطلاب من المنطقة العربية.",
                Url = "https://scholarpath.com/resources/scholarship-databases",
                Type = "Article",
                Category = "Scholarship Search",
                CreatedAt = SeedDate
            },
            new Resource
            {
                Id = Resource4,
                Title = "Recommendation Letter Request Template",
                TitleAr = "نموذج طلب خطاب التوصية",
                Description = "Professional email templates and tips for requesting strong recommendation letters from professors and employers.",
                DescriptionAr = "قوالب بريد إلكتروني احترافية ونصائح لطلب خطابات توصية قوية من الأساتذة وأصحاب العمل.",
                Url = "https://scholarpath.com/resources/recommendation-letter-template",
                Type = "Template",
                Category = "Application Tips",
                CreatedAt = SeedDate
            }
        );
    }

    // ── Groups ───────────────────────────────────────────────────────────

    private static void SeedGroups(ModelBuilder builder)
    {
        builder.Entity<Group>().HasData(
            new Group
            {
                Id = Group1,
                Name = "DAAD Scholarship Applicants",
                NameAr = "متقدمو منحة DAAD",
                Description = "A community for students applying to DAAD scholarships. Share tips, experiences, and support each other through the application process.",
                DescriptionAr = "مجتمع للطلاب المتقدمين لمنح DAAD. شاركوا النصائح والخبرات وادعموا بعضكم البعض خلال عملية التقديم.",
                CreatorId = ConsultantId,
                IsPrivate = false,
                MaxMembers = 200,
                CreatedAt = SeedDate
            },
            new Group
            {
                Id = Group2,
                Name = "Study in the UK",
                NameAr = "الدراسة في بريطانيا",
                Description = "Everything about studying in the United Kingdom: university selection, visa process, living costs, and scholarship opportunities.",
                DescriptionAr = "كل ما يتعلق بالدراسة في المملكة المتحدة: اختيار الجامعة وإجراءات التأشيرة وتكاليف المعيشة وفرص المنح الدراسية.",
                CreatorId = AdminId,
                IsPrivate = false,
                MaxMembers = 500,
                CreatedAt = SeedDate
            }
        );

        // Group Members
        builder.Entity<GroupMember>().HasData(
            new GroupMember
            {
                Id = Guid.Parse("61000000-0000-0000-0000-000000000001"),
                GroupId = Group1,
                UserId = ConsultantId,
                Role = GroupRole.Admin,
                CreatedAt = SeedDate
            },
            new GroupMember
            {
                Id = Guid.Parse("61000000-0000-0000-0000-000000000002"),
                GroupId = Group1,
                UserId = StudentId,
                Role = GroupRole.Member,
                CreatedAt = SeedDate
            },
            new GroupMember
            {
                Id = Guid.Parse("61000000-0000-0000-0000-000000000003"),
                GroupId = Group2,
                UserId = AdminId,
                Role = GroupRole.Admin,
                CreatedAt = SeedDate
            },
            new GroupMember
            {
                Id = Guid.Parse("61000000-0000-0000-0000-000000000004"),
                GroupId = Group2,
                UserId = StudentId,
                Role = GroupRole.Member,
                CreatedAt = SeedDate
            }
        );
    }

    // ── Notifications ────────────────────────────────────────────────────

    private static void SeedNotifications(ModelBuilder builder)
    {
        builder.Entity<Notification>().HasData(
            new Notification
            {
                Id = Notification1,
                UserId = StudentId,
                Type = NotificationType.System,
                Title = "Welcome to ScholarPath!",
                TitleAr = "مرحبًا بك في ScholarPath!",
                Message = "Your account has been activated. Start exploring scholarships and join communities to connect with fellow students.",
                MessageAr = "تم تفعيل حسابك. ابدأ في استكشاف المنح الدراسية وانضم إلى المجتمعات للتواصل مع زملائك الطلاب.",
                IsRead = false,
                CreatedAt = SeedDate
            },
            new Notification
            {
                Id = Notification2,
                UserId = StudentId,
                Type = NotificationType.ScholarshipAlert,
                Title = "New Scholarship: DAAD Graduate Scholarship",
                TitleAr = "منحة جديدة: منحة DAAD للدراسات العليا",
                Message = "A new fully-funded scholarship matching your profile has been posted. Check it out before the deadline!",
                MessageAr = "تم نشر منحة ممولة بالكامل جديدة تتوافق مع ملفك الشخصي. تحقق منها قبل الموعد النهائي!",
                IsRead = false,
                RelatedEntityId = Scholarship1,
                RelatedEntityType = "Scholarship",
                CreatedAt = SeedDate
            }
        );
    }
}
