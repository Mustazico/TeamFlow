using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Application.Activity;
using TeamFlow.Application.Dashboard;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/activity")]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activity;

    public ActivityController(IActivityService activity) => _activity = activity;

    [HttpGet("recent")]
    public async Task<IActionResult> Recent([FromQuery] int take = 30, CancellationToken ct = default) =>
        Ok(await _activity.RecentForMeAsync(Math.Clamp(take, 1, 100), ct));

    [HttpGet("by-project/{projectId:guid}")]
    public async Task<IActionResult> ByProject(Guid projectId, [FromQuery] int take = 50, CancellationToken ct = default) =>
        Ok(await _activity.ForProjectAsync(projectId, Math.Clamp(take, 1, 200), ct));
}

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet("overview")]
    public async Task<IActionResult> Overview([FromQuery] int days = 14, CancellationToken ct = default) =>
        Ok(await _dashboard.GetOverviewAsync(days, ct));
}
