using MediatR;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Common.Behaviors;

/// <summary>
/// Writes an AuditLog entry after a successful handler run, for commands
/// decorated with [Auditable]. No-op for requests without the attribute.
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse>(
    IAuditService audit,
    ILogger<AuditBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken).ConfigureAwait(false);

        var attr = (AuditableAttribute?)Attribute.GetCustomAttribute(typeof(TRequest), typeof(AuditableAttribute));
        if (attr is null)
        {
            return response;
        }

        if (attr.SkipOnNull && response is null)
        {
            return response;
        }

        try
        {
            var targetId = ResolveTargetId(response, request, attr.TargetIdProperty);
            var summary = attr.SummaryTemplate?.Replace("{TargetId}", targetId?.ToString() ?? "-", StringComparison.Ordinal);

            await audit.WriteAsync(
                attr.Action,
                attr.TargetType,
                targetId,
                beforeJson: null,
                afterJson: null,
                summary,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // never let audit failures bubble up and undo a successful command
            logger.LogWarning(ex, "Audit write failed for {RequestName}", typeof(TRequest).Name);
        }

        return response;
    }

    private static Guid? ResolveTargetId(object? response, object request, string propertyName)
    {
        // try response first
        if (response is not null)
        {
            var v = response.GetType().GetProperty(propertyName)?.GetValue(response);
            if (v is Guid g) return g;
            if (Guid.TryParse(v?.ToString(), out var parsed)) return parsed;
        }

        // fall back to request
        var r = request.GetType().GetProperty(propertyName)?.GetValue(request);
        if (r is Guid rg) return rg;
        if (Guid.TryParse(r?.ToString(), out var rp)) return rp;

        return null;
    }
}
