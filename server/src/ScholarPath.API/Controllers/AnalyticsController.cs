using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Analytics.Queries.GetAdminAnalytics;
using ScholarPath.Application.Analytics.Queries.GetAdminRevenue;
using ScholarPath.Application.Analytics.Queries.GetScholarshipProviderInsights;
using ScholarPath.Application.Analytics.Queries.GetConsultantEarningsTrend;
using ScholarPath.Application.Analytics.Queries.GetConsultantKpis;
using ScholarPath.Application.Analytics.Queries.GetPowerBiEmbedToken;
using ScholarPath.Application.Analytics.Queries.GetStudentJourney;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.API.Controllers;

/// <summary>
/// Power BI analytics embed-token endpoint (PB-015 T-014).
///
/// Role access rules (enforced in the query handler):
///   Admin / SuperAdmin → any report type
///   Consultant         → ConsultantSelfAnalytics only
///   Student            → StudentSelfAnalytics only
/// </summary>
[ApiController]
[Authorize]
[Route("api/analytics")]
[Produces("application/json")]
public sealed class AnalyticsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns a short-lived Power BI embed token for the requested report,
    /// scoped to the caller's identity via RLS.  Returns 503 when the Power BI
    /// workspace has not been provisioned yet.
    /// </summary>
    /// <param name="reportType">
    /// One of: ExecutiveDashboard | StudentSuccessDashboard | FinancialDashboard |
    /// ConsultantSelfAnalytics | StudentSelfAnalytics
    /// </param>
    [HttpGet("embed-token")]
    [ProducesResponseType(typeof(EmbedTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetEmbedToken(
        [FromQuery] string reportType,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetPowerBiEmbedTokenQuery(reportType), ct)
            .ConfigureAwait(false);

        if (!result.IsConfigured)
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "Power BI workspace is not provisioned yet." });

        return Ok(result);
    }

    /// <summary>
    /// Returns KPI aggregates for the authenticated consultant from
    /// <c>dbo.vw_consultant_kpis</c>.  Returns all-zeros when the consultant
    /// has no activity yet.
    /// </summary>
    [HttpGet("consultant-kpis")]
    [Authorize(Roles = "Consultant,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ConsultantKpisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConsultantKpis(CancellationToken ct)
    {
        var result = await mediator.Send(new GetConsultantKpisQuery(), ct)
            .ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Returns the authenticated student's journey aggregates from
    /// <c>dbo.vw_student_journey</c>.  Returns all-zeros when the student
    /// has no activity yet.
    /// </summary>
    [HttpGet("student-journey")]
    [Authorize(Roles = "Student,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(StudentJourneyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudentJourney(CancellationToken ct)
    {
        var result = await mediator.Send(new GetStudentJourneyQuery(), ct)
            .ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Returns admin-level platform analytics: funnel, finance, and acceptance
    /// rates by field — sourced from three SQL views and filtered to the last
    /// <paramref name="days"/> calendar days (funnel and finance only).
    /// </summary>
    /// <param name="days">Trailing days window for funnel and finance data (default 30).</param>
    [HttpGet("admin-reporting")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(AdminAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAdminReporting(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAdminAnalyticsQuery(days), ct)
            .ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Admin revenue report — gross / profit-share / payee-net / refunded
    /// totals aggregated from Payments and ScholarshipProviderReviewPayments in the
    /// supplied window, with monthly breakdown, top consultants, and a
    /// month-over-month growth percentage vs the equivalent prior period.
    /// </summary>
    [HttpGet("admin/revenue")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(AdminRevenueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAdminRevenue(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAdminRevenueQuery(from, to), ct)
            .ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Provider application insights — pipeline, country/field breakdown,
    /// top scholarships, and a monthly view→apply→accept funnel for the
    /// supplied company. The caller must own the company (ScholarshipProvider role) or
    /// be an admin. Omit <paramref name="companyId"/> to default to the
    /// caller's own user-id.
    /// </summary>
    [HttpGet("company/insights")]
    [Authorize(Roles = "ScholarshipProvider,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ScholarshipProviderInsightsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetScholarshipProviderInsights(
        [FromQuery] Guid? companyId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetScholarshipProviderInsightsQuery(companyId, from, to), ct)
            .ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Consultant earnings trend — monthly gross/net for the caller, a
    /// 3-month linear projection of next month's net, and an anonymised
    /// percentile vs all consultants on the platform.
    /// </summary>
    [HttpGet("consultant/earnings-trend")]
    [Authorize(Roles = "Consultant,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ConsultantEarningsTrendDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConsultantEarningsTrend(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetConsultantEarningsTrendQuery(from, to), ct)
            .ConfigureAwait(false);
        return Ok(result);
    }
}
