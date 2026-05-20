using FluentValidation;
using MediatR;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Analytics.Queries.GetPowerBiEmbedToken;

/// <summary>
/// Supported Power BI report types. Each value maps to a report GUID in
/// <c>PowerBiOptions.ReportIds</c> and a role-access rule enforced in the handler.
/// </summary>
public static class PowerBiReportType
{
    public const string ExecutiveDashboard        = "ExecutiveDashboard";
    public const string StudentSuccessDashboard   = "StudentSuccessDashboard";
    public const string FinancialDashboard        = "FinancialDashboard";
    public const string ConsultantSelfAnalytics   = "ConsultantSelfAnalytics";
    public const string StudentSelfAnalytics      = "StudentSelfAnalytics";

    private static readonly IReadOnlySet<string> AdminOnly = new HashSet<string>(StringComparer.Ordinal)
    {
        ExecutiveDashboard, StudentSuccessDashboard, FinancialDashboard,
    };

    /// <summary>
    /// Checks whether <paramref name="activeRole"/> is allowed to fetch an
    /// embed token for <paramref name="reportType"/>.
    /// </summary>
    public static bool IsRoleAllowed(string reportType, string activeRole) =>
        activeRole is "Admin" or "SuperAdmin"
        || (reportType == ConsultantSelfAnalytics && activeRole == "Consultant")
        || (reportType == StudentSelfAnalytics    && activeRole == "Student");
}

/// <summary>
/// Returns a short-lived Power BI embed token for the requested report,
/// scoped to the caller's identity (RLS). Returns <c>IsConfigured = false</c>
/// when the Power BI workspace is not yet provisioned (PB-015 T-014).
/// </summary>
public sealed record GetPowerBiEmbedTokenQuery(string ReportType) : IRequest<EmbedTokenDto>;

public sealed class GetPowerBiEmbedTokenQueryValidator : AbstractValidator<GetPowerBiEmbedTokenQuery>
{
    private static readonly string[] ValidTypes =
    [
        PowerBiReportType.ExecutiveDashboard,
        PowerBiReportType.StudentSuccessDashboard,
        PowerBiReportType.FinancialDashboard,
        PowerBiReportType.ConsultantSelfAnalytics,
        PowerBiReportType.StudentSelfAnalytics,
    ];

    public GetPowerBiEmbedTokenQueryValidator()
    {
        RuleFor(q => q.ReportType)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t, StringComparer.Ordinal))
            .WithMessage("Unknown report type. Must be one of: " + string.Join(", ", ValidTypes));
    }
}

public sealed class GetPowerBiEmbedTokenQueryHandler(
    IPowerBiService powerBi,
    ICurrentUserService currentUser)
    : IRequestHandler<GetPowerBiEmbedTokenQuery, EmbedTokenDto>
{
    public async Task<EmbedTokenDto> Handle(GetPowerBiEmbedTokenQuery request, CancellationToken ct)
    {
        var userId      = currentUser.UserId      ?? throw new ForbiddenAccessException();
        var email       = currentUser.Email       ?? throw new ForbiddenAccessException();
        var activeRole  = currentUser.ActiveRole  ?? throw new ForbiddenAccessException();

        if (!PowerBiReportType.IsRoleAllowed(request.ReportType, activeRole))
            throw new ForbiddenAccessException(
                $"Role '{activeRole}' is not allowed to access report '{request.ReportType}'.");

        return await powerBi.GetEmbedTokenAsync(
            request.ReportType, userId, email, activeRole, ct);
    }
}
