using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Jobs;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Audit;

/// <summary>
/// GDPR Art. 15 — verifies the data-export job collects ALL of the user's
/// personal data across every related table and audits the access event.
/// </summary>
public class DataExportJobTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IDateTimeService Clock()
    {
        var c = Substitute.For<IDateTimeService>();
        c.UtcNow.Returns(new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero));
        return c;
    }

    /// <summary>Blob stub that records the JSON payload it was asked to upload.</summary>
    private sealed class CapturingBlob : IBlobStorageService
    {
        public string? CapturedJson { get; private set; }

        public async Task<string> UploadAsync(Stream content, string fileName, string contentType, string container, CancellationToken ct)
        {
            using var reader = new StreamReader(content, Encoding.UTF8);
            CapturedJson = await reader.ReadToEndAsync(ct);
            return $"https://blob.test/{container}/{fileName}";
        }

        public Task DeleteAsync(string blobUrl, CancellationToken ct) => Task.CompletedTask;
        public Task<Stream> DownloadAsync(string blobUrl, CancellationToken ct) =>
            Task.FromResult<Stream>(new MemoryStream());
    }

    private static ApplicationUser SeedUser(Guid id) => new()
    {
        Id = id,
        Email = "subject@example.com",
        UserName = "subject@example.com",
        FirstName = "Real",
        LastName = "Person",
        AccountStatus = AccountStatus.Active,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Export_includes_personal_data_from_every_related_table()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(SeedUser(userId));

        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = userId, Biography = "my bio" };
        db.UserProfiles.Add(profile);
        db.EducationEntries.Add(new EducationEntry
        {
            UserProfileId = profile.Id,
            InstitutionName = "Cairo University",
            Degree = "BSc",
            FieldOfStudy = "CS",
        });

        var conversationId = Guid.NewGuid();
        db.Messages.Add(new ChatMessage
        {
            ConversationId = conversationId,
            SenderId = userId,
            Body = "secret chat content",
        });
        db.ForumPosts.Add(new ForumPost
        {
            AuthorId = userId,
            BodyMarkdown = "my forum post",
            Title = "post title",
        });
        db.AiInteractions.Add(new AiInteraction
        {
            UserId = userId,
            Feature = AiFeature.Chatbot,
            Provider = AiProvider.Stub,
            PromptText = "my private prompt",
            ResponseText = "the answer",
        });
        db.CompanyReviews.Add(new CompanyReview
        {
            StudentId = userId,
            CompanyId = Guid.NewGuid(),
            ApplicationTrackerId = Guid.NewGuid(),
            Rating = 5,
            Comment = "great company",
        });
        db.Applications.Add(new ApplicationTracker
        {
            StudentId = userId,
            ScholarshipId = Guid.NewGuid(),
            PersonalNotes = "remember to follow up",
        });
        db.UserDataRequests.Add(new UserDataRequest
        {
            UserId = userId,
            Type = UserDataRequestType.Export,
            Status = UserDataRequestStatus.Pending,
            RequestedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var blob = new CapturingBlob();
        var job = new DataExportJob(db, blob, Substitute.For<IEmailService>(), Clock(),
            NullLogger<DataExportJob>.Instance);

        await job.RunAsync(default);

        var json = blob.CapturedJson;
        json.Should().NotBeNull();
        // Every category of personal data must be present in the export blob.
        json.Should().Contain("secret chat content", "chat messages must be exported");
        json.Should().Contain("my forum post", "forum posts must be exported");
        json.Should().Contain("my private prompt", "AI interactions must be exported");
        json.Should().Contain("the answer");
        json.Should().Contain("great company", "company reviews must be exported");
        json.Should().Contain("Cairo University", "education history must be exported");
        json.Should().Contain("remember to follow up", "application personal notes must be exported");
        json.Should().Contain("my bio");
    }

    [Fact]
    public async Task Export_marks_request_completed_with_download_url()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(SeedUser(userId));
        var req = new UserDataRequest
        {
            UserId = userId,
            Type = UserDataRequestType.Export,
            Status = UserDataRequestStatus.Pending,
            RequestedAt = DateTimeOffset.UtcNow,
        };
        db.UserDataRequests.Add(req);
        await db.SaveChangesAsync();

        await new DataExportJob(db, new CapturingBlob(), Substitute.For<IEmailService>(), Clock(),
            NullLogger<DataExportJob>.Instance).RunAsync(default);

        var saved = await db.UserDataRequests.FindAsync(req.Id);
        saved!.Status.Should().Be(UserDataRequestStatus.Completed);
        saved.DownloadUrl.Should().NotBeNullOrEmpty();
        saved.DownloadExpiresAt.Should().NotBeNull();
        saved.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Export_writes_an_audit_log_entry()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(SeedUser(userId));
        var req = new UserDataRequest
        {
            UserId = userId,
            Type = UserDataRequestType.Export,
            Status = UserDataRequestStatus.Pending,
            RequestedAt = DateTimeOffset.UtcNow,
        };
        db.UserDataRequests.Add(req);
        await db.SaveChangesAsync();

        await new DataExportJob(db, new CapturingBlob(), Substitute.For<IEmailService>(), Clock(),
            NullLogger<DataExportJob>.Instance).RunAsync(default);

        var audit = db.AuditLogs.SingleOrDefault(a => a.TargetId == req.Id);
        audit.Should().NotBeNull();
        audit!.TargetType.Should().Be("UserDataRequest");
        audit.ActorUserId.Should().BeNull("the export is performed by a system job");
    }

    [Fact]
    public async Task Export_is_idempotent_skips_already_completed_requests()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(SeedUser(userId));
        db.UserDataRequests.Add(new UserDataRequest
        {
            UserId = userId,
            Type = UserDataRequestType.Export,
            Status = UserDataRequestStatus.Completed,
            RequestedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var blob = new CapturingBlob();
        await new DataExportJob(db, blob, Substitute.For<IEmailService>(), Clock(),
            NullLogger<DataExportJob>.Instance).RunAsync(default);

        blob.CapturedJson.Should().BeNull("a completed request must not be re-processed");
    }

    [Fact]
    public async Task Export_handles_missing_user_without_throwing()
    {
        using var db = CreateDb();
        // No ApplicationUser row — request references a user that does not exist.
        var req = new UserDataRequest
        {
            UserId = Guid.NewGuid(),
            Type = UserDataRequestType.Export,
            Status = UserDataRequestStatus.Pending,
            RequestedAt = DateTimeOffset.UtcNow,
        };
        db.UserDataRequests.Add(req);
        await db.SaveChangesAsync();

        await new DataExportJob(db, new CapturingBlob(), Substitute.For<IEmailService>(), Clock(),
            NullLogger<DataExportJob>.Instance).RunAsync(default);

        var saved = await db.UserDataRequests.FindAsync(req.Id);
        saved!.Status.Should().Be(UserDataRequestStatus.Completed);
    }
}
