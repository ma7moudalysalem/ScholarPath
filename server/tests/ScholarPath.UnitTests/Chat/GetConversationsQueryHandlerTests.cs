using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Chat.Queries.GetConversations;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Chat;

/// <summary>
/// Unit tests for <see cref="GetConversationsQueryHandler"/>.
/// Covers: conversation list shape, avatar URL projection, presence dot,
/// and blocked-participant flag.
/// </summary>
public sealed class GetConversationsQueryHandlerTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ApplicationUser MakeUser(string firstName = "Test", string? photoUrl = null) => new()
    {
        Id = Guid.NewGuid(),
        Email = $"{Guid.NewGuid():N}@chat-test.local",
        UserName = $"{Guid.NewGuid():N}@chat-test.local",
        FirstName = firstName,
        LastName = "User",
        ProfileImageUrl = photoUrl,
    };

    [Fact]
    public async Task Returns_conversations_with_correct_name_and_avatar()
    {
        await using var db = CreateDb();

        var me = MakeUser("Me");
        var other = MakeUser("Alice", photoUrl: "https://cdn.example.com/alice.jpg");
        var conv = new ChatConversation
        {
            Id = Guid.NewGuid(),
            ParticipantOneId = me.Id,
            ParticipantTwoId = other.Id,
            LastMessageAt = DateTimeOffset.UtcNow,
        };
        db.Users.AddRange(me, other);
        db.Conversations.Add(conv);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(me.Id);
        var presence = Substitute.For<IChatPresenceQuery>();
        presence.IsOnline(other.Id).Returns(true);

        var sut = new GetConversationsQueryHandler(db, currentUser, presence);
        var result = await sut.Handle(new GetConversationsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        var dto = result[0];
        dto.OtherParticipantId.Should().Be(other.Id);
        dto.OtherParticipantName.Should().Contain("Alice");
        dto.OtherParticipantAvatarUrl.Should().Be("https://cdn.example.com/alice.jpg");
        dto.IsOnline.Should().BeTrue();
        dto.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task Null_avatar_when_user_has_no_photo()
    {
        await using var db = CreateDb();

        var me = MakeUser();
        var other = MakeUser(photoUrl: null);
        db.Users.AddRange(me, other);
        db.Conversations.Add(new ChatConversation
        {
            Id = Guid.NewGuid(),
            ParticipantOneId = me.Id,
            ParticipantTwoId = other.Id,
        });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(me.Id);
        var presence = Substitute.For<IChatPresenceQuery>();

        var sut = new GetConversationsQueryHandler(db, currentUser, presence);
        var result = await sut.Handle(new GetConversationsQuery(), CancellationToken.None);

        result[0].OtherParticipantAvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task IsOnline_false_when_other_participant_is_offline()
    {
        await using var db = CreateDb();

        var me = MakeUser();
        var other = MakeUser();
        db.Users.AddRange(me, other);
        db.Conversations.Add(new ChatConversation
        {
            Id = Guid.NewGuid(),
            ParticipantOneId = me.Id,
            ParticipantTwoId = other.Id,
        });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(me.Id);
        var presence = Substitute.For<IChatPresenceQuery>();
        presence.IsOnline(Arg.Any<Guid>()).Returns(false); // offline

        var sut = new GetConversationsQueryHandler(db, currentUser, presence);
        var result = await sut.Handle(new GetConversationsQuery(), CancellationToken.None);

        result[0].IsOnline.Should().BeFalse();
    }

    [Fact]
    public async Task IsBlocked_true_when_current_user_has_blocked_other_participant()
    {
        await using var db = CreateDb();

        var me = MakeUser();
        var other = MakeUser();
        db.Users.AddRange(me, other);
        db.Conversations.Add(new ChatConversation
        {
            Id = Guid.NewGuid(),
            ParticipantOneId = me.Id,
            ParticipantTwoId = other.Id,
        });
        db.UserBlocks.Add(new UserBlock { BlockerId = me.Id, BlockedUserId = other.Id });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(me.Id);
        var presence = Substitute.For<IChatPresenceQuery>();

        var sut = new GetConversationsQueryHandler(db, currentUser, presence);
        var result = await sut.Handle(new GetConversationsQuery(), CancellationToken.None);

        result[0].IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task Returns_empty_list_when_user_has_no_conversations()
    {
        await using var db = CreateDb();
        var me = MakeUser();
        db.Users.Add(me);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(me.Id);
        var presence = Substitute.For<IChatPresenceQuery>();

        var sut = new GetConversationsQueryHandler(db, currentUser, presence);
        var result = await sut.Handle(new GetConversationsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
