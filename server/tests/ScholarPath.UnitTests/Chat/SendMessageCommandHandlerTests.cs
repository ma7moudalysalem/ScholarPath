using MediatR;
using NSubstitute;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Application.Chat.Commands.SendMessage;

namespace ScholarPath.UnitTests.Chat;

public class SendMessageCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IChatRealtimeNotifier _chatNotifier = Substitute.For<IChatRealtimeNotifier>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly SendMessageCommandHandler _handler;

    public SendMessageCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _handler = new SendMessageCommandHandler(_db, _currentUser, _chatNotifier, _publisher);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Handle_BlockedUser_ThrowsConflictException()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        _currentUser.UserId.Returns(currentUserId);

        var conversation = new ChatConversation
        {
            Id = conversationId,
            ParticipantOneId = currentUserId,
            ParticipantTwoId = recipientId,
        };

        var block = new UserBlock
        {
            BlockerId = recipientId,
            BlockedUserId = currentUserId
        };

        _db.Conversations.Add(conversation);
        _db.UserBlocks.Add(block);
        await _db.SaveChangesAsync();

        var command = new SendMessageCommand(recipientId, "Hello");

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
