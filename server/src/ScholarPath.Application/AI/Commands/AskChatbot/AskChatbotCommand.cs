using MediatR;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Ai.Commands.AskChatbot;

[Auditable(AuditAction.Create, "AiInteraction",
    SummaryTemplate = "AI chatbot turn ({SessionId})")]
public sealed record AskChatbotCommand(
    string Message,
    string? SessionId) : IRequest<ChatAnswerDto>;
