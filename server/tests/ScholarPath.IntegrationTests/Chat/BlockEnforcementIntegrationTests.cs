using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using Xunit;

namespace ScholarPath.IntegrationTests.Chat;

/// <summary>
/// PB-007 T-010 — Block-enforcement integration tests.
///
/// Verifies that the chat pipeline rejects messages through the real HTTP
/// stack when a block exists — not just at the command-handler unit level.
/// </summary>
public sealed class BlockEnforcementIntegrationTests : IntegrationTestBase
{
    public BlockEnforcementIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task<(Guid senderId, Guid recipientId)> SeedUsersAsync()
    {
        var senderId    = Guid.NewGuid();
        var recipientId = Guid.NewGuid();

        await ExecuteScopeAsync(async sp =>
        {
            var db = GetDb(sp);

            db.Users.Add(new ApplicationUser
            {
                Id = senderId,
                UserName          = $"sender.{senderId:N}@scholarpath.local",
                NormalizedUserName = $"sender.{senderId:N}@scholarpath.local".ToUpperInvariant(),
                Email             = $"sender.{senderId:N}@scholarpath.local",
                NormalizedEmail   = $"sender.{senderId:N}@scholarpath.local".ToUpperInvariant(),
                EmailConfirmed    = true,
                FirstName         = "Sender",
                LastName          = "User",
                AccountStatus     = AccountStatus.Active,
                ActiveRole        = "Student",
            });

            db.Users.Add(new ApplicationUser
            {
                Id = recipientId,
                UserName          = $"recipient.{recipientId:N}@scholarpath.local",
                NormalizedUserName = $"recipient.{recipientId:N}@scholarpath.local".ToUpperInvariant(),
                Email             = $"recipient.{recipientId:N}@scholarpath.local",
                NormalizedEmail   = $"recipient.{recipientId:N}@scholarpath.local".ToUpperInvariant(),
                EmailConfirmed    = true,
                FirstName         = "Recipient",
                LastName          = "User",
                AccountStatus     = AccountStatus.Active,
                ActiveRole        = "Student",
            });

            await db.SaveChangesAsync();
        });

        return (senderId, recipientId);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// When there is NO block between the two users the message endpoint should
    /// succeed (200 OK). This proves the test infrastructure is wired correctly
    /// before the negative path.
    /// </summary>
    [Fact]
    public async Task SendMessage_NoBlock_Returns200()
    {
        var (senderId, recipientId) = await SeedUsersAsync();

        await ExecuteScopeAsync(sp =>
        {
            GetCurrentUser(sp).SetUser(
                senderId,
                $"sender.{senderId:N}@scholarpath.local",
                "Student");
            return Task.CompletedTask;
        });

        var response = await Client.PostAsJsonAsync("/api/chat/messages", new
        {
            recipientId,
            body = "Hello!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// When the recipient has blocked the sender, the endpoint must reject the
    /// message with 409 Conflict. This is the PB-007 T-010 acceptance criterion:
    /// block enforcement is verified end-to-end through the HTTP stack.
    /// </summary>
    [Fact]
    public async Task SendMessage_RecipientBlockedSender_Returns409()
    {
        var (senderId, recipientId) = await SeedUsersAsync();

        // Seed: recipient blocks sender
        await ExecuteScopeAsync(async sp =>
        {
            var db = GetDb(sp);
            db.UserBlocks.Add(new UserBlock
            {
                BlockerId     = recipientId,
                BlockedUserId = senderId,
            });
            await db.SaveChangesAsync();
        });

        // Act: sender tries to send a message
        await ExecuteScopeAsync(sp =>
        {
            GetCurrentUser(sp).SetUser(
                senderId,
                $"sender.{senderId:N}@scholarpath.local",
                "Student");
            return Task.CompletedTask;
        });

        var response = await Client.PostAsJsonAsync("/api/chat/messages", new
        {
            recipientId,
            body = "Hello — this should be blocked!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            because: "a message to a user who has blocked the sender must be rejected");
    }

    /// <summary>
    /// When the sender has blocked the recipient, the endpoint must also reject
    /// the message. The block is symmetric — neither party can message the other.
    /// </summary>
    [Fact]
    public async Task SendMessage_SenderBlockedRecipient_Returns409()
    {
        var (senderId, recipientId) = await SeedUsersAsync();

        // Seed: sender blocks recipient
        await ExecuteScopeAsync(async sp =>
        {
            var db = GetDb(sp);
            db.UserBlocks.Add(new UserBlock
            {
                BlockerId     = senderId,
                BlockedUserId = recipientId,
            });
            await db.SaveChangesAsync();
        });

        // Act: sender tries to message the blocked recipient
        await ExecuteScopeAsync(sp =>
        {
            GetCurrentUser(sp).SetUser(
                senderId,
                $"sender.{senderId:N}@scholarpath.local",
                "Student");
            return Task.CompletedTask;
        });

        var response = await Client.PostAsJsonAsync("/api/chat/messages", new
        {
            recipientId,
            body = "Hello — should also be blocked!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            because: "a message to a user the sender has blocked must also be rejected");
    }

    /// <summary>
    /// After a block is removed the blocked sender should be able to send again.
    /// Verifies that block enforcement is not sticky after an unblock.
    /// </summary>
    [Fact]
    public async Task SendMessage_AfterUnblock_Returns200()
    {
        var (senderId, recipientId) = await SeedUsersAsync();

        // Seed and then remove a block
        await ExecuteScopeAsync(async sp =>
        {
            var db = GetDb(sp);
            var block = new UserBlock
            {
                BlockerId     = recipientId,
                BlockedUserId = senderId,
            };
            db.UserBlocks.Add(block);
            await db.SaveChangesAsync();

            db.UserBlocks.Remove(block);
            await db.SaveChangesAsync();
        });

        // Act: sender messages recipient — should succeed now
        await ExecuteScopeAsync(sp =>
        {
            GetCurrentUser(sp).SetUser(
                senderId,
                $"sender.{senderId:N}@scholarpath.local",
                "Student");
            return Task.CompletedTask;
        });

        var response = await Client.PostAsJsonAsync("/api/chat/messages", new
        {
            recipientId,
            body = "Back in touch!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "messaging should succeed once the block is removed");
    }
}
