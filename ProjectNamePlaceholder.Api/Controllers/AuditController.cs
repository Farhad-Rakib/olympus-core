using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.Audit.Dtos;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Security;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditLogRepository _repository;
    private readonly IAuditLogService _auditLogService;

    public AuditController(IAuditLogRepository repository, IAuditLogService auditLogService)
    {
        _repository = repository;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.AuditRead)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] long? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] bool? success = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _repository.QueryAsync(userId, action, from, to, success, page, pageSize, cancellationToken);

        var dtos = items.Select(x => new AuditLogDto(
            x.Id,
            x.UserId,
            x.UserName,
            x.Action,
            x.Path,
            x.Method,
            x.Data,
            x.RequestBody,
            x.ResponseBody,
            x.StatusCode,
            x.UserAgent,
            x.Headers,
            x.Success,
            x.Timestamp,
            x.Ip)).ToList();

        var result = new { Items = dtos, Total = total, Page = page, PageSize = pageSize };
        return Ok(ApiResponse<object>.SuccessResponse(result, "Audit logs retrieved"));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.AuditCreate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateAuditRequest request, CancellationToken cancellationToken)
    {
        // Allow front-end to submit client-side audit events
        await _auditLogService.CreateAsync(request.UserId, request.Action, request.Data, cancellationToken);
        return Created(string.Empty, ApiResponse.SuccessResponse("Audit created", StatusCodes.Status201Created));
    }
}
