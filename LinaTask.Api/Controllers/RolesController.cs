using LinaTask.Api.Attributes;
using LinaTask.Application.Services;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<RolesController> _logger;

        public RolesController(IRoleService roleService, ILogger<RolesController> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
        {
            try
            {
                var roles = await _roleService.GetAllRolesAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los roles");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<RoleDto>> GetRoleById(Guid id)
        {
            try
            {
                var role = await _roleService.GetRoleByIdAsync(id);
                if (role == null)
                    return NotFound($"Rol con ID {id} no encontrado");

                return Ok(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener rol por ID: {RoleId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("by-name/{name}")]
        public async Task<ActionResult<RoleDto>> GetRoleByName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest("El nombre del rol es requerido");

                var role = await _roleService.GetRoleByNameAsync(name);
                if (role == null)
                    return NotFound($"Rol con nombre '{name}' no encontrado");

                return Ok(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener rol por nombre: {RoleName}", name);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost]
        public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleDto createRoleDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var role = await _roleService.CreateRoleAsync(createRoleDto);
                return CreatedAtAction(nameof(GetRoleById), new { id = role.Id }, role);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear rol");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<RoleDto>> UpdateRole(Guid id, [FromBody] UpdateRoleDto updateRoleDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var role = await _roleService.UpdateRoleAsync(id, updateRoleDto);
                return Ok(role);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar rol: {RoleId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            try
            {
                var deleted = await _roleService.DeleteRoleAsync(id);
                if (!deleted)
                    return NotFound($"Rol con ID {id} no encontrado");

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar rol: {RoleId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("exists/{id:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> RoleExists(Guid id)
        {
            try
            {
                var exists = await _roleService.RoleExistsAsync(id);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia del rol: {RoleId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("exists-by-name/{name}")]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> RoleExistsByName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest("El nombre del rol es requerido");

                var exists = await _roleService.RoleExistsByNameAsync(name);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia del rol por nombre: {RoleName}", name);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}