using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.Dashboard;
using ProjectNamePlaceholder.Application.Dashboard.Dtos;
using ProjectNamePlaceholder.Application.Security;

namespace ProjectNamePlaceholder.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = Permissions.ReportsRead)]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>User/role totals plus API activity from the audit log for the last <paramref name="days"/> days.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get([FromQuery] int days = 7, CancellationToken cancellationToken = default)
    {
        if (days is < DashboardService.MinDays or > DashboardService.MaxDays)
        {
            return BadRequest(ApiResponse.FailureResponse(
                $"days must be between {DashboardService.MinDays} and {DashboardService.MaxDays}.", StatusCodes.Status400BadRequest));
        }

        var dashboard = await _dashboardService.GetDashboardAsync(days, cancellationToken);
        return Ok(ApiResponse<DashboardDto>.SuccessResponse(dashboard, "Dashboard retrieved successfully"));
    }
}
