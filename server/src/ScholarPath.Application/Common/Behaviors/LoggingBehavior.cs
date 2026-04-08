using MediatR;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = currentUser.UserId?.ToString() ?? "anonymous";

        logger.LogInformation(
            "Handling {RequestName} for user {UserId}",
            requestName, userId);

        var response = await next(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Handled  {RequestName} for user {UserId}",
            requestName, userId);

        return response;
    }
}
