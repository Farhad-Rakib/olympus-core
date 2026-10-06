using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.Menu;
using ProjectNamePlaceholder.Application.Security;

namespace ProjectNamePlaceholder.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<dynamic>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenu(CancellationToken cancellationToken)
    {
        var menu = await _menuService.GetMenuForUserAsync(User, cancellationToken);
        return Ok(ApiResponse<dynamic>.SuccessResponse(menu, "Menu retrieved successfully"));
    }

    [HttpGet("all")]
    [Authorize(Policy = Permissions.MenusRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MenuDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var menus = await _menuService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MenuDto>>.SuccessResponse(menus, "Menus retrieved"));
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = Permissions.MenusRead)]
    [ProducesResponseType(typeof(ApiResponse<MenuDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken cancellationToken)
    {
        var menu = await _menuService.GetByIdAsync(id, cancellationToken);
        if (menu == null) return NotFound(ApiResponse.FailureResponse("Menu not found", StatusCodes.Status404NotFound));
        return Ok(ApiResponse<MenuDto>.SuccessResponse(menu, "Menu retrieved"));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.MenusCreate)]
    [ProducesResponseType(typeof(ApiResponse<MenuDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateMenuRequestDto request, CancellationToken cancellationToken)
    {
        var created = await _menuService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<MenuDto>.SuccessResponse(created, "Menu created", StatusCodes.Status201Created));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Permissions.MenusUpdate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update([FromRoute] long id, [FromBody] UpdateMenuRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            await _menuService.UpdateAsync(id, request, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.FailureResponse("Menu not found", StatusCodes.Status404NotFound));
        }
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = Permissions.MenusDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken cancellationToken)
    {
        try
        {
            await _menuService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.FailureResponse("Menu not found", StatusCodes.Status404NotFound));
        }
    }
}
