using NSubstitute;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Infrastructure.Hubs;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Application.Chat.Commands.SendMessage;

namespace ScholarPath.UnitTests.Chat;

public class SendMessageCommandHandlerTests
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IHubContext<ChatHub> _hubContext = Substitute.For<IHubContext<ChatHub>>();
    private readonly SendMessageCommandHandler _handler;

    public SendMessageCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _handler = new SendMessageCommandHandler(_db, _currentUser, _hubContext);
    }

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
            Messages = new List<ChatMessage>()
        };

        var block = new UserBlock
        {
            BlockerId = recipientId,
            BlockedUserId = currentUserId
        };

        _db.Conversations.Add(conversation);
        _db.UserBlocks.Add(block);
        await _db.SaveChangesAsync();

        var command = new SendMessageCommand(conversationId, "Hello");

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
