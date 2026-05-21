using System.Net;
using System.Text.Json;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Domain.Exceptions;

namespace ScholarPath.API.Middleware;

public sealed class ExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlerMiddleware> logger,
    IHostEnvironment env)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (ValidationException vex)
        {
            await WriteProblemAsync(
                context,
                HttpStatusCode.UnprocessableEntity,
                "Validation failed",
                detail: vex.Message,
                errors: vex.Errors).ConfigureAwait(false);
        }
        catch (BookingDomainException bdex)
        {
            await WriteProblemAsync(
                context,
                HttpStatusCode.UnprocessableEntity,
                "Booking rule violated",
                bdex.Message).ConfigureAwait(false);
        }
        catch (NotFoundException nex)
        {
            await WriteProblemAsync(
                context,
                HttpStatusCode.NotFound,
                "Not found",
                nex.Message).ConfigureAwait(false);
        }
        catch (ConflictException cex)
        {
            await WriteProblemAsync(
                context,
                HttpStatusCode.Conflict,
                "Conflict",
                cex.Message).ConfigureAwait(false);
        }
        catch (ForbiddenAccessException fex)
        {
            await WriteProblemAsync(
                context,
                HttpStatusCode.Forbidden,
                "Forbidden",
                fex.Message).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException uaex)
        {
            // Several CQRS handlers throw the BCL UnauthorizedAccessException for
            // role / ownership checks. Without this branch it falls through to the
            // catch-all and surfaces as a 500 instead of a 403. Treat it as the
            // domain-level Forbidden case — the user IS authenticated, the action
            // is just not permitted for them.
            await WriteProblemAsync(
                context,
                HttpStatusCode.Forbidden,
                "Forbidden",
                uaex.Message).ConfigureAwait(false);
        }
        catch (ServiceUnavailableException suex)
        {
            // A dependency (e.g. the AI embedding provider) was down or
            // misconfigured — log the cause but return an actionable 503,
            // not an opaque 500.
            logger.LogError(suex, "A dependency was unavailable at {Path}", context.Request.Path);
            await WriteProblemAsync(
                context,
                HttpStatusCode.ServiceUnavailable,
                "Service unavailable",
                suex.Message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception at {Path}", context.Request.Path);
            var detail = env.IsDevelopment() ? ex.ToString() : "An error occurred. Please try again.";

            await WriteProblemAsync(
                context,
                HttpStatusCode.InternalServerError,
                "Internal server error",
                detail).ConfigureAwait(false);
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext ctx,
        HttpStatusCode status,
        string title,
        string? detail = null,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/problem+json";

        var payload = new Dictionary<string, object?>
        {
            ["title"] = title,
            ["status"] = (int)status,
            ["detail"] = detail,
            ["traceId"] = ctx.TraceIdentifier,
        };

        if (errors is not null && errors.Count > 0)
        {
            payload["errors"] = errors;
        }

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });

        await ctx.Response.WriteAsync(json).ConfigureAwait(false);
    }
}
