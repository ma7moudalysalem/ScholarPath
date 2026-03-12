using MediatR;

public record LinkProviderCommand(string Provider, string IdToken, string UserId) : IRequest<Unit>;
