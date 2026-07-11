using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.Menu;
using ProjectNamePlaceholder.Application.Security;

namespace ProjectNamePlaceholder.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController(IMenuService menuService) : ControllerBase
    {

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<dynamic>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMenu()
        {
            var menu = await menuService.GetMenuForUserAsync(User);
            return Ok(ApiResponse<dynamic>.SuccessResponse(menu, "Menu retrieved successfully"));
        }

        [HttpGet("all")]
        [Authorize(Policy = Permissions.MenusRead)]
        [ProducesResponseType(typeof(ApiResponse<List<MenuDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var menus = await menuService.GetAllAsync();
            return Ok(ApiResponse<List<MenuDto>>.SuccessResponse(menus, "Menus retrieved"));
        }

        [HttpGet("{id:long}")]
        [Authorize(Policy = Permissions.MenusRead)]
        [ProducesResponseType(typeof(ApiResponse<MenuDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById([FromRoute] long id)
        {
            var menu = await menuService.GetByIdAsync(id);
            if (menu == null) return NotFound(ApiResponse.FailureResponse("Menu not found", 404));
            return Ok(ApiResponse<MenuDto>.SuccessResponse(menu, "Menu retrieved"));
        }

        [HttpPost]
        [Authorize(Policy = Permissions.MenusCreate)]
        [ProducesResponseType(typeof(ApiResponse<MenuDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CreateMenuRequestDto request)
        {
            var created = await menuService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<MenuDto>.SuccessResponse(created, "Menu created", 201));
        }

        [HttpPut("{id:long}")]
        [Authorize(Policy = Permissions.MenusUpdate)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Update([FromRoute] long id, [FromBody] UpdateMenuRequestDto request)
        {
            try
            {
                await menuService.UpdateAsync(id, request);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse.FailureResponse("Menu not found", 404));
            }
        }

        [HttpDelete("{id:long}")]
        [Authorize(Policy = Permissions.MenusDelete)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete([FromRoute] long id)
        {
            try
            {
                await menuService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse.FailureResponse("Menu not found", 404));
            }
        }
    }
}
