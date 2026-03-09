using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/menus")]
    public class MenusController : ControllerBase
    {
        private readonly IMenuService _service;

        public MenusController(IMenuService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var menus = await _service.GetAllAsync();
            return Ok(menus);
        }

        [HttpGet("visible")]
        public async Task<IActionResult> GetVisible()
        {
            var menus = await _service.GetVisibleMenusAsync();
            return Ok(menus);
        }

        [HttpGet("hierarchy")]
        public async Task<IActionResult> GetHierarchy()
        {
            var menus = await _service.GetMenuHierarchyAsync();
            return Ok(menus);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var menu = await _service.GetByIdAsync(id);
            if (menu == null)
                return NotFound();

            return Ok(menu);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMenuDto dto)
        {
            try
            {
                var menu = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = menu.Id }, menu);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMenuDto dto)
        {
            try
            {
                var menu = await _service.UpdateAsync(id, dto);
                if (menu == null)
                    return NotFound();

                return Ok(menu);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/permissions")]
        public async Task<IActionResult> AssignPermissions(Guid id, [FromBody] List<Guid> permissionIds)
        {
            try
            {
                await _service.AssignPermissionsAsync(id, permissionIds);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("by-role/{roleId}")]
        public async Task<IActionResult> GetByRole(Guid roleId)
        {
            var menus = await _service.GetMenusByRoleAsync(roleId);
            return Ok(menus);
        }

        [HttpGet("by-role-name/{roleName}")]
        public async Task<IActionResult> GetByRoleName(string roleName)
        {
            var menus = await _service.GetMenusByRoleNameAsync(roleName);
            return Ok(menus);
        }
    }
}