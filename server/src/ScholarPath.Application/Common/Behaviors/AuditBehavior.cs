using System.Text.RegularExpressions;
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
            var summary = BuildSummary(attr.SummaryTemplate, targetId, response, request);

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

    private static readonly Regex PlaceholderRegex = new(@"\{(\w+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Substitutes every {Token} in the template with a real value: {TargetId}
    /// uses the resolved target id, any other {Property} is looked up by name on
    /// the response (preferred) then the request. Previously only {TargetId} was
    /// replaced, so summaries like "AI chatbot turn ({SessionId})" leaked the
    /// raw placeholder into the audit log.
    /// </summary>
    private static string? BuildSummary(string? template, Guid? targetId, object? response, object request)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        return PlaceholderRegex.Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            if (string.Equals(name, "TargetId", StringComparison.Ordinal))
            {
                return targetId?.ToString() ?? "-";
            }

            return ResolvePropertyValue(response, request, name) ?? "-";
        });
    }

    /// <summary>Reads a property by name from the response (preferred) or request, as a string.</summary>
    private static string? ResolvePropertyValue(object? response, object request, string propertyName)
    {
        if (response is not null)
        {
            var v = response.GetType().GetProperty(propertyName)?.GetValue(response);
            if (v is not null) return v.ToString();
        }

        var r = request.GetType().GetProperty(propertyName)?.GetValue(request);
        return r?.ToString();
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
