using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Admin.Commands.ApproveOnboarding;
using ScholarPath.Application.Admin.Commands.ChangeUserRole;
using ScholarPath.Application.Admin.Commands.ReviewUpgradeRequest;
using ScholarPath.Application.Admin.Commands.SendBroadcast;
using ScholarPath.Application.Admin.Commands.SetUserStatus;
using ScholarPath.Application.Admin.Commands.SoftDeleteUser;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Admin.Queries.GetAnalyticsOverview;
using ScholarPath.Application.Admin.Queries.GetApplicationFunnel;
using ScholarPath.Application.Admin.Queries.GetOnboardingQueue;
using ScholarPath.Application.Admin.Queries.GetUpgradeQueue;
using ScholarPath.Application.Admin.Queries.GetUserDetail;
using ScholarPath.Application.Admin.Queries.GetUserGrowth;
using ScholarPath.Application.Admin.Queries.SearchUsers;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Ai.Queries.GetAiUsageSummary;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Audit.Queries.GetAuditLog;
using ScholarPath.Domain.Enums;

namespace ScholarPath.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin")]
[Produces("application/json")]
public sealed class AdminController(IMediator mediator) : ControllerBase
{
    // ─── Users ────────────────────────────────────────────────────────────

    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<AdminUserRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? search,
        [FromQuery] AccountStatus? status,
        [FromQuery] string? role,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var q = new SearchUsersQuery(search, status, role, includeDeleted, page, pageSize);
        var result = await mediator.Send(q, ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("users/{userId:guid}")]
    [ProducesResponseType(typeof(AdminUserDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserDetail(Guid userId, CancellationToken ct)
    {
        var user = await mediator.Send(new GetUserDetailQuery(userId), ct).ConfigureAwait(false);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("users/{userId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetUserStatus(
        Guid userId,
        [FromBody] SetUserStatusBody body,
        CancellationToken ct)
    {
        await mediator.Send(new SetUserStatusCommand(userId, body.Status, body.Reason), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDeleteUser(
        Guid userId,
        [FromQuery] string? reason,
        CancellationToken ct)
    {
        await mediator.Send(new SoftDeleteUserCommand(userId, reason), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeUserRole(
        Guid userId,
        [FromBody] ChangeRoleBody body,
        CancellationToken ct)
    {
        await mediator.Send(new ChangeUserRoleCommand(userId, body.Role, body.Operation), ct).ConfigureAwait(false);
        return NoContent();
    }

    // ─── Onboarding + upgrade queues ──────────────────────────────────────

    [HttpGet("onboarding-queue")]
    [ProducesResponseType(typeof(PagedResult<OnboardingRequestRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOnboardingQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetOnboardingQueueQuery(page, pageSize), ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("onboarding-queue/{userId:guid}/review")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReviewOnboarding(
        Guid userId,
        [FromBody] ReviewDecisionBody body,
        CancellationToken ct)
    {
        var decision = body.Approve ? OnboardingDecision.Approve : OnboardingDecision.Reject;
        await mediator.Send(new ReviewOnboardingCommand(userId, decision, body.Notes), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("upgrade-queue")]
    [ProducesResponseType(typeof(PagedResult<UpgradeRequestRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpgradeQueue(
        [FromQuery] UpgradeRequestStatus? status = UpgradeRequestStatus.Pending,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetUpgradeQueueQuery(status, page, pageSize), ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("upgrade-queue/{requestId:guid}/review")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReviewUpgrade(
        Guid requestId,
        [FromBody] ReviewDecisionBody body,
        CancellationToken ct)
    {
        var decision = body.Approve ? UpgradeDecision.Approve : UpgradeDecision.Reject;
        await mediator.Send(new ReviewUpgradeRequestCommand(requestId, decision, body.Notes), ct).ConfigureAwait(false);
        return NoContent();
    }

    // ─── Analytics ────────────────────────────────────────────────────────

    [HttpGet("analytics/overview")]
    [ProducesResponseType(typeof(AnalyticsOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AnalyticsOverview(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAnalyticsOverviewQuery(), ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("analytics/user-growth")]
    [ProducesResponseType(typeof(IReadOnlyList<GrowthPoint>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UserGrowth([FromQuery] int days = 30, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetUserGrowthQuery(days), ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("analytics/application-funnel")]
    [ProducesResponseType(typeof(IReadOnlyList<ApplicationStatusPoint>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplicationFunnel(CancellationToken ct)
    {
        var result = await mediator.Send(new GetApplicationFunnelQuery(), ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// AI economy dashboard payload — cost, volume, latency, and recommendation
    /// CTR. Window is clamped server-side to {7, 30, 90} days (PB-017 FR-248..FR-252).
    /// </summary>
    [HttpGet("analytics/ai-usage")]
    [ProducesResponseType(typeof(AiUsageSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AiUsage(
        [FromQuery] int windowDays = 30,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAiUsageSummaryQuery(windowDays), ct).ConfigureAwait(false);
        return Ok(result);
    }

    // ─── Audit log ────────────────────────────────────────────────────────

    [HttpGet("audit-log")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] AuditAction? action = null,
        [FromQuery] string? targetType = null,
        [FromQuery] Guid? actorUserId = null,
        [FromQuery] Guid? targetId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var q = new GetAuditLogQuery(page, pageSize, action, targetType, actorUserId, targetId, from, to, search);
        var result = await mediator.Send(q, ct).ConfigureAwait(false);
        return Ok(result);
    }

    // ─── Broadcast ────────────────────────────────────────────────────────

    [HttpPost("broadcasts")]
    [ProducesResponseType(typeof(BroadcastResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendBroadcast([FromBody] SendBroadcastCommand command, CancellationToken ct)
    {
        var count = await mediator.Send(command, ct).ConfigureAwait(false);
        return Ok(new BroadcastResultDto(count));
    }
}

// ─── Request/response DTOs kept local to the controller ───────────────────
public sealed record SetUserStatusBody(AccountStatus Status, string? Reason);
public sealed record ChangeRoleBody(string Role, RoleOp Operation);
public sealed record ReviewDecisionBody(bool Approve, string? Notes);
public sealed record BroadcastResultDto(int RecipientCount);
