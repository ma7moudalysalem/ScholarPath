using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Infrastructure.Persistence.Seed;

public static partial class DbSeeder
{
    /// <summary>
    /// Seeds direct-message data: a handful of <see cref="ChatConversation"/>s
    /// between demo users (student↔consultant, student↔company, student↔student),
    /// each with a thread of <see cref="ChatMessage"/>s (a mix of read and
    /// unread), one archived-for-a-participant conversation, and a
    /// <see cref="UserBlock"/> example. Idempotent on
    /// <see cref="ChatConversation"/> being empty. The unique
    /// <c>(ParticipantOneId, ParticipantTwoId)</c> index is respected by using
    /// distinct participant pairs.
    /// </summary>
    private static async Task SeedChatAsync(
        ApplicationDbContext db, DemoUsers users, ILogger logger, CancellationToken ct)
    {
        if (await db.Conversations.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        // --- conversation 1: student ↔ consultant ------------------------
        var convo1 = new ChatConversation
        {
            ParticipantOneId = users.Students[0].Id,
            ParticipantTwoId = users.Consultants[0].Id,
            CreatedAt = now.AddDays(-13),
        };
        // --- conversation 2: student ↔ company ---------------------------
        var convo2 = new ChatConversation
        {
            ParticipantOneId = users.Students[1].Id,
            ParticipantTwoId = users.Companies[0].Id,
            CreatedAt = now.AddDays(-7),
        };
        // --- conversation 3: student ↔ student, archived for participant two
        var convo3 = new ChatConversation
        {
            ParticipantOneId = users.Students[2].Id,
            ParticipantTwoId = users.Students[3].Id,
            IsArchivedForParticipantTwo = true,
            CreatedAt = now.AddDays(-20),
        };

        db.Conversations.AddRange(convo1, convo2, convo3);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // --- messages ----------------------------------------------------
        var convo1Messages = new List<ChatMessage>
        {
            new() { ConversationId = convo1.Id, SenderId = users.Students[0].Id, Body = "Hi! I'd love to book a session about my statement of purpose.", SentAt = now.AddDays(-13), ReadAt = now.AddDays(-13).AddHours(1), CreatedAt = now.AddDays(-13) },
            new() { ConversationId = convo1.Id, SenderId = users.Consultants[0].Id, Body = "Of course — send me your current draft and pick a slot from my calendar.", SentAt = now.AddDays(-13).AddHours(1), ReadAt = now.AddDays(-13).AddHours(2), CreatedAt = now.AddDays(-13).AddHours(1) },
            new() { ConversationId = convo1.Id, SenderId = users.Students[0].Id, Body = "Great, I just booked the Tuesday slot. Thank you!", SentAt = now.AddDays(-12), ReadAt = null, CreatedAt = now.AddDays(-12) },
        };
        var convo2Messages = new List<ChatMessage>
        {
            new() { ConversationId = convo2.Id, SenderId = users.Students[1].Id, Body = "Hello, does the STEM Excellence Award cover travel costs?", SentAt = now.AddDays(-7), ReadAt = now.AddDays(-7).AddHours(3), CreatedAt = now.AddDays(-7) },
            new() { ConversationId = convo2.Id, SenderId = users.Companies[0].Id, Body = "Yes — it covers tuition, a monthly stipend, and one return flight.", SentAt = now.AddDays(-7).AddHours(3), ReadAt = null, CreatedAt = now.AddDays(-7).AddHours(3) },
        };
        var convo3Messages = new List<ChatMessage>
        {
            new() { ConversationId = convo3.Id, SenderId = users.Students[2].Id, Body = "Did you submit your application yet?", SentAt = now.AddDays(-20), ReadAt = now.AddDays(-19), CreatedAt = now.AddDays(-20) },
            new() { ConversationId = convo3.Id, SenderId = users.Students[3].Id, Body = "Not yet — still gathering my recommendation letters.", SentAt = now.AddDays(-19), ReadAt = now.AddDays(-19).AddHours(2), CreatedAt = now.AddDays(-19) },
        };

        var allMessages = convo1Messages.Concat(convo2Messages).Concat(convo3Messages).ToList();
        db.Messages.AddRange(allMessages);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // --- point each conversation at its last message -----------------
        SetLastMessage(convo1, convo1Messages);
        SetLastMessage(convo2, convo2Messages);
        SetLastMessage(convo3, convo3Messages);

        // --- a user block ------------------------------------------------
        db.UserBlocks.Add(new UserBlock
        {
            BlockerId = users.Students[0].Id,
            BlockedUserId = users.Students[4].Id,
            Reason = "Unsolicited messages advertising paid essay services.",
            BlockedAt = now.AddDays(-3),
        });

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation(
            "Seeded chat: {C} conversations, {M} messages, 1 user block",
            3, allMessages.Count);
    }

    private static void SetLastMessage(ChatConversation convo, IReadOnlyList<ChatMessage> messages)
    {
        var last = messages[^1];
        convo.LastMessageAt = last.SentAt;
        convo.LastMessageId = last.Id;
    }
}
