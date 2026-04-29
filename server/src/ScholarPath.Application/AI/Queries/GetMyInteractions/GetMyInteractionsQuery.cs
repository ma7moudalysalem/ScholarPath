using MediatR;
using ScholarPath.Application.Ai.DTOs;

namespace ScholarPath.Application.Ai.Queries.GetMyInteractions;

public sealed record GetMyInteractionsQuery(int Limit = 20) : IRequest<IReadOnlyList<AiInteractionRowDto>>;
