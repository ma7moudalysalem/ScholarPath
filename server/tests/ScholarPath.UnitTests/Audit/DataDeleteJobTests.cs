using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Jobs;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Audit;

/// <summary>
/// GDPR Art. 17 — verifies the data-delete job removes or irreversibly
/// anonymises ALL personal data across every related table, keeps
/// financial records, audits the erasure, and is safe to re-run.
/// </summary>
public class DataDeleteJobTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);

    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IDateTimeService Clock()
    {
        var c = Substitute.For<IDateTimeService>();
        c.UtcNow.Returns(Now);
        return c;
    }

    private static DataDeleteJob Sut(ApplicationDbContext db) =>
        new(db, Clock(), NullLogger<DataDeleteJob>.Instance);

    /// <summary>Seeds a fully-populated user with PII spread across every table, plus a due delete request.</summary>
    private static async Task<Guid> SeedFullUserWithDueRequest(ApplicationDbContext db)
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "real.person@example.com",
            UserName = "real.person@example.com",
            NormalizedEmail = "REAL.PERSON@EXAMPLE.COM",
            FirstName = "Real",
            LastName = "Person",
            PhoneNumber = "+201234567890",
            ProfileImageUrl = "https://blob/avatar.png",
            PasswordHash = "HASHED-SECRET",
            AccountStatus = AccountStatus.Active,
            CreatedAt = Now.AddYears(-1),
        };
        db.Users.Add(user);

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Biography = "personal biography",
            Nationality = "Egyptian",
            LinkedInUrl = "https://linkedin.com/in/real",
        };
        db.UserProfiles.Add(profile);
        db.EducationEntries.Add(new EducationEntry
        {
            UserProfileId = profile.Id,
            InstitutionName = "Cairo University",
            Degree = "BSc",
            FieldOfStudy = "CS",
            Description = "graduated top of class",
        });

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = "tok",
            ExpiresAt = Now.AddDays(7),
            IpAddress = "203.0.113.7",
            UserAgent = "Firefox",
        });
        db.LoginAttempts.Add(new LoginAttempt
        {
            UserId = userId,
            Email = "real.person@example.com",
            Succeeded = true,
            IpAddress = "203.0.113.7",
            UserAgent = "Firefox",
        });

        db.Messages.Add(new ChatMessage
        {
            ConversationId = Guid.NewGuid(),
            SenderId = userId,
            Body = "private chat message",
        });
        db.ForumPosts.Add(new ForumPost
        {
            AuthorId = userId,
            Title = "my thread",
            BodyMarkdown = "private forum content",
        });
        db.AiInteractions.Add(new AiInteraction
        {
            UserId = userId,
            Feature = AiFeature.Chatbot,
            Provider = AiProvider.Stub,
            PromptText = "private prompt with PII",
            ResponseText = "private response",
            MetadataJson = "{\"x\":1}",
        });
        db.ScholarshipProviderReviews.Add(new ScholarshipProviderReview
        {
            StudentId = userId,
            ScholarshipProviderId = Guid.NewGuid(),
            ApplicationTrackerId = Guid.NewGuid(),
            Rating = 4,
            Comment = "personal opinion text",
        });
        db.Applications.Add(new ApplicationTracker
        {
            StudentId = userId,
            ScholarshipId = Guid.NewGuid(),
            PersonalNotes = "private notes",
            FormDataJson = "{\"answer\":\"sensitive\"}",
        });

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            Action = AuditAction.Login,
            TargetType = "User",
            TargetId = userId,
            IpAddress = "203.0.113.7",
            UserAgent = "Firefox",
        });

        db.UserDataRequests.Add(new UserDataRequest
        {
            UserId = userId,
            Type = UserDataRequestType.Delete,
            Status = UserDataRequestStatus.Pending,
            RequestedAt = Now.AddDays(-31),
            ScheduledProcessAt = Now.AddDays(-1), // cooling-off elapsed
        });

        await db.SaveChangesAsync();
        return userId;
    }

    [Fact]
    public async Task Delete_anonymises_the_user_row()
    {
        using var db = CreateDb();
        var userId = await SeedFullUserWithDueRequest(db);

        await Sut(db).RunAsync(default);

        var user = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId);
        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().Be(Now);
        user.DeletedByUserId.Should().BeNull("the system job is the actor, not the user");
        user.FirstName.Should().Be("Deleted");
        user.LastName.Should().Be("User");
        user.Email.Should().NotContain("real.person");
        user.PhoneNumber.Should().BeNull();
        user.ProfileImageUrl.Should().BeNull();
        user.PasswordHash.Should().BeNull("credentials must not survive erasure");
    }

    [Fact]
    public async Task Delete_anonymises_PII_in_all_related_tables()
    {
        using var db = CreateDb();
        var userId = await SeedFullUserWithDueRequest(db);

        await Sut(db).RunAsync(default);

        // Profile + education
        var profile = await db.UserProfiles.IgnoreQueryFilters().FirstAsync(p => p.UserId == userId);
        profile.Biography.Should().BeNull();
        profile.Nationality.Should().BeNull();
        profile.LinkedInUrl.Should().BeNull();
        var edu = await db.EducationEntries.IgnoreQueryFilters().FirstAsync();
        edu.InstitutionName.Should().Be("[removed]");
        edu.Description.Should().BeNull();

        // Chat
        var msg = await db.Messages.IgnoreQueryFilters().FirstAsync(m => m.SenderId == userId);
        msg.Body.Should().NotContain("private chat message");
        msg.IsDeleted.Should().BeTrue();

        // Forum
        var post = await db.ForumPosts.IgnoreQueryFilters().FirstAsync(p => p.AuthorId == userId);
        post.BodyMarkdown.Should().NotContain("private forum content");
        post.Title.Should().Be("[removed]");
        post.IsDeleted.Should().BeTrue();

        // AI interactions
        var ai = await db.AiInteractions.IgnoreQueryFilters().FirstAsync(a => a.UserId == userId);
        ai.PromptText.Should().NotContain("private prompt");
        ai.ResponseText.Should().NotContain("private response");
        ai.MetadataJson.Should().BeNull();

        // Reviews
        var review = await db.ScholarshipProviderReviews.IgnoreQueryFilters().FirstAsync(r => r.StudentId == userId);
        review.Comment.Should().BeNull();

        // Application tracker
        var app = await db.Applications.IgnoreQueryFilters().FirstAsync(a => a.StudentId == userId);
        app.PersonalNotes.Should().BeNull();
        app.FormDataJson.Should().BeNull();

        // Login attempts + audit log network identifiers
        var login = await db.LoginAttempts.IgnoreQueryFilters().FirstAsync(l => l.UserId == userId);
        login.Email.Should().Be("[removed]");
        login.IpAddress.Should().BeNull();
        var audit = await db.AuditLogs.FirstAsync(a => a.ActorUserId == userId && a.Action == AuditAction.Login);
        audit.IpAddress.Should().BeNull();
        audit.UserAgent.Should().BeNull();

        // Refresh tokens revoked + IP stripped
        var token = await db.RefreshTokens.IgnoreQueryFilters().FirstAsync(t => t.UserId == userId);
        token.IsRevoked.Should().BeTrue();
        token.IpAddress.Should().BeNull();
    }

    [Fact]
    public async Task Delete_marks_request_completed_and_writes_audit_log()
    {
        using var db = CreateDb();
        var userId = await SeedFullUserWithDueRequest(db);
        var reqId = (await db.UserDataRequests.FirstAsync(r => r.UserId == userId)).Id;

        await Sut(db).RunAsync(default);

        var req = await db.UserDataRequests.FindAsync(reqId);
        req!.Status.Should().Be(UserDataRequestStatus.Completed);
        req.CompletedAt.Should().Be(Now);

        var audit = db.AuditLogs.SingleOrDefault(
            a => a.TargetId == reqId && a.Action == AuditAction.Delete);
        audit.Should().NotBeNull("the erasure must itself be audited");
        audit!.ActorUserId.Should().BeNull();
    }

    [Fact]
    public async Task Delete_does_not_process_requests_still_in_cooling_off()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            Email = "x@example.com",
            UserName = "x@example.com",
            FirstName = "Still",
            LastName = "Here",
            CreatedAt = Now,
        });
        db.UserDataRequests.Add(new UserDataRequest
        {
            UserId = userId,
            Type = UserDataRequestType.Delete,
            Status = UserDataRequestStatus.Pending,
            RequestedAt = Now,
            ScheduledProcessAt = Now.AddDays(20), // still within cooling-off
        });
        await db.SaveChangesAsync();

        await Sut(db).RunAsync(default);

        var user = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId);
        user.IsDeleted.Should().BeFalse();
        user.FirstName.Should().Be("Still");
    }

    [Fact]
    public async Task Delete_is_idempotent_when_rerun()
    {
        using var db = CreateDb();
        var userId = await SeedFullUserWithDueRequest(db);

        await Sut(db).RunAsync(default);
        // Second sweep — the request is now Completed, must not be picked up again.
        await Sut(db).RunAsync(default);

        var requests = db.UserDataRequests.Where(r => r.UserId == userId).ToList();
        requests.Should().ContainSingle();
        requests[0].Status.Should().Be(UserDataRequestStatus.Completed);

        // Erasure result is stable.
        var user = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId);
        user.FirstName.Should().Be("Deleted");
        user.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_handles_missing_user_and_still_completes_request()
    {
        using var db = CreateDb();
        // Delete request referencing a user that does not exist.
        var req = new UserDataRequest
        {
            UserId = Guid.NewGuid(),
            Type = UserDataRequestType.Delete,
            Status = UserDataRequestStatus.Pending,
            RequestedAt = Now.AddDays(-31),
            ScheduledProcessAt = Now.AddDays(-1),
        };
        db.UserDataRequests.Add(req);
        await db.SaveChangesAsync();

        await Sut(db).RunAsync(default);

        var saved = await db.UserDataRequests.FindAsync(req.Id);
        saved!.Status.Should().Be(UserDataRequestStatus.Completed);
    }

    [Fact]
    public async Task Delete_keeps_financial_payment_records()
    {
        using var db = CreateDb();
        var userId = await SeedFullUserWithDueRequest(db);
        var payment = new Payment
        {
            Type = PaymentType.ConsultantBooking,
            Status = PaymentStatus.Captured,
            AmountCents = 5000,
            Currency = "USD",
            PayerUserId = userId,
            IdempotencyKey = $"key_{Guid.NewGuid():N}",
            CapturedAt = Now.AddDays(-5),
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        await Sut(db).RunAsync(default);

        // The transaction must survive erasure (legal retention); it carries no
        // name/email — only the now-anonymised user GUID.
        var kept = await db.Payments.FindAsync(payment.Id);
        kept.Should().NotBeNull();
        kept!.AmountCents.Should().Be(5000);
        kept.PayerUserId.Should().Be(userId);
    }
}
