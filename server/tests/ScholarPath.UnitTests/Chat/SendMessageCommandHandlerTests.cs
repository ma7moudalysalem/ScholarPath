using Moq;
using Moq.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Infrastructure.Hubs;
using ScholarPath.Application.Chat.Commands.SendMessage;

namespace ScholarPath.UnitTests.Chat;

public class SendMessageCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IHubContext<ChatHub>> _hubContextMock;
    private readonly SendMessageCommandHandler _handler;

    public SendMessageCommandHandlerTests()
    {
        _dbMock = new Mock<IApplicationDbContext>();
        _currentUserMock = new Mock<ICurrentUserService>();
        _hubContextMock = new Mock<IHubContext<ChatHub>>();

        _handler = new SendMessageCommandHandler(
            _dbMock.Object,
            _currentUserMock.Object,
            _hubContextMock.Object);
    }

    [Fact]
    public async Task Handle_BlockedUser_ThrowsConflictException()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        _currentUserMock.Setup(c => c.UserId).Returns(currentUserId);

        var conversation = new ChatConversation
        {
            Id = conversationId,
            ParticipantOneId = currentUserId,
            ParticipantTwoId = recipientId
        };

        var block = new UserBlock
        {
            BlockerId = recipientId,
            BlockedUserId = currentUserId
        };

        _dbMock.Setup(db => db.ChatConversations).ReturnsDbSet(new List<ChatConversation> { conversation });
        _dbMock.Setup(db => db.UserBlocks).ReturnsDbSet(new List<UserBlock> { block });
        _dbMock.Setup(db => db.ChatMessages).ReturnsDbSet(new List<ChatMessage>());

        var command = new SendMessageCommand(conversationId, "Hello");

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
