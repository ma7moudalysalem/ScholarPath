using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Dashboard.Queries.GetDashboardSummary;
using ScholarPath.Domain.Entities;

namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/dashboard")]
[Authorize]
public class DashboardController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return UnauthorizedResult("errors.auth.unauthorized");

        var result = await Mediator.Send(new GetDashboardSummaryQuery(user.Id), ct);
        return Ok(result);
    }
}
