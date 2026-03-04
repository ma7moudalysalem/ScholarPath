using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ScholarPath.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("ScholarPath Request: {Name} started", requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "ScholarPath Request: {Name} completed in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "ScholarPath Request: {Name} failed after {ElapsedMilliseconds}ms with error: {Error}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }
}
