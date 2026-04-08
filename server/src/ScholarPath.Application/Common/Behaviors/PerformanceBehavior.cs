using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ScholarPath.Application.Common.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await next(cancellationToken).ConfigureAwait(false);
        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            logger.LogWarning(
                "Long-running request: {RequestName} took {ElapsedMilliseconds}ms",
                requestName, sw.ElapsedMilliseconds);
        }

        return response;
    }
}
