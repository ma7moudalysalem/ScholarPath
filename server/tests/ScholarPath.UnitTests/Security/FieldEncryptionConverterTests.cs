using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.UnitTests.Security;

/// <summary>
/// Confirms the EF Core <c>EncryptedStringConverter</c> wiring: the configured
/// PII columns (<see cref="UserProfile.Biography"/>,
/// <see cref="ApplicationTracker.PersonalNotes"/>) are stored as ciphertext in
/// the database, yet handlers reading through EF see plaintext transparently.
/// </summary>
public sealed class FieldEncryptionConverterTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly AesGcmFieldEncryptionService _encryption;

    public FieldEncryptionConverterTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        var keyProvider = new LocalFieldEncryptionKeyProvider(
            Options.Create(new FieldEncryptionOptions
            {
                DevKey = "vY2z2EgwRy2+Ls92notyOeyo5HuMEodhzhytZm81KFg=",
            }));
        _encryption = new AesGcmFieldEncryptionService(keyProvider);

        // The encryption-aware context creates the schema; converters do not
        // affect DDL, only the values written/read.
        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    private ApplicationDbContext NewContext() =>
        new(_options, mediator: null, encryption: _encryption);

    /// <summary>Seeds a minimal user and returns its id, satisfying the UserProfile FK.</summary>
    private static async Task<Guid> SeedUserAsync(ApplicationDbContext db)
    {
        var id = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            Email = $"u{id:N}@test.local",
            NormalizedEmail = $"U{id:N}@TEST.LOCAL",
            UserName = $"u{id:N}@test.local",
            NormalizedUserName = $"U{id:N}@TEST.LOCAL",
            FirstName = "Test",
            LastName = "User",
        });
        await db.SaveChangesAsync();
        return id;
    }

    /// <summary>Seeds a minimal scholarship and returns its id, satisfying the ApplicationTracker FK.</summary>
    private static async Task<Guid> SeedScholarshipAsync(ApplicationDbContext db)
    {
        var id = Guid.NewGuid();
        db.Scholarships.Add(new Scholarship
        {
            Id = id,
            TitleEn = "Test Scholarship",
            TitleAr = "منحة اختبار",
            DescriptionEn = "Description",
            DescriptionAr = "وصف",
            Slug = $"test-{id:N}",
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
        });
        await db.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task UserProfile_Biography_is_ciphertext_in_the_database_but_plaintext_through_EF()
    {
        const string plaintext = "A genuinely sensitive personal biography.";
        var profileId = Guid.NewGuid();

        await using (var db = NewContext())
        {
            var userId = await SeedUserAsync(db);
            db.UserProfiles.Add(new UserProfile
            {
                Id = profileId,
                UserId = userId,
                Biography = plaintext,
            });
            await db.SaveChangesAsync();
        }

        // Raw column value, bypassing EF — it must be an encrypted envelope.
        var raw = ReadSingleRowColumn("SELECT \"Biography\" FROM \"UserProfiles\"");
        raw.Should().NotBeNull();
        raw.Should().StartWith("enc:v1:");
        raw.Should().NotContain("biography", "the plaintext must not be readable at rest");

        // …but a fresh EF read transparently decrypts it back to plaintext.
        await using (var db = NewContext())
        {
            var loaded = await db.UserProfiles.SingleAsync(p => p.Id == profileId);
            loaded.Biography.Should().Be(plaintext);
        }
    }

    [Fact]
    public async Task ApplicationTracker_PersonalNotes_is_ciphertext_in_the_database_but_plaintext_through_EF()
    {
        const string plaintext = "Private reminder: chase the recommendation letter.";
        var appId = Guid.NewGuid();

        await using (var db = NewContext())
        {
            var studentId = await SeedUserAsync(db);
            var scholarshipId = await SeedScholarshipAsync(db);
            db.Applications.Add(new ApplicationTracker
            {
                Id = appId,
                StudentId = studentId,
                ScholarshipId = scholarshipId,
                Mode = ApplicationMode.InApp,
                Status = ApplicationStatus.Draft,
                PersonalNotes = plaintext,
            });
            await db.SaveChangesAsync();
        }

        var raw = ReadSingleRowColumn("SELECT \"PersonalNotes\" FROM \"Applications\"");
        raw.Should().NotBeNull();
        raw.Should().StartWith("enc:v1:");

        await using (var db = NewContext())
        {
            var loaded = await db.Applications.SingleAsync(a => a.Id == appId);
            loaded.PersonalNotes.Should().Be(plaintext);
        }
    }

    [Fact]
    public async Task A_null_PII_value_round_trips_as_null()
    {
        var profileId = Guid.NewGuid();

        await using (var db = NewContext())
        {
            var userId = await SeedUserAsync(db);
            db.UserProfiles.Add(new UserProfile
            {
                Id = profileId,
                UserId = userId,
                Biography = null,
            });
            await db.SaveChangesAsync();
        }

        ReadSingleRowColumn("SELECT \"Biography\" FROM \"UserProfiles\"").Should().BeNull();

        await using (var db = NewContext())
        {
            var loaded = await db.UserProfiles.SingleAsync(p => p.Id == profileId);
            loaded.Biography.Should().BeNull();
        }
    }

    /// <summary>
    /// Reads the column of the single row produced by <paramref name="sql"/>,
    /// directly via ADO.NET — bypassing EF and its value converters so the test
    /// observes exactly what is persisted at rest. Each test's table of interest
    /// (<c>UserProfiles</c> / <c>Applications</c>) holds exactly one row.
    /// <paramref name="sql"/> is a fixed test-owned literal, never user input.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Security", "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "sql is always a fixed test-owned string literal, never user input.")]
    private string? ReadSingleRowColumn(string sql)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        var result = cmd.ExecuteScalar();
        return result is null or DBNull ? null : (string)result;
    }

    public void Dispose() => _connection.Dispose();
}
