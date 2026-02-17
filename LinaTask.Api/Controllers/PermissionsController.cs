using LinaTask.Api.Attributes;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(
            IPermissionRepository permissionRepository,
            ILogger<PermissionsController> logger)
        {
            _permissionRepository = permissionRepository;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los permisos disponibles
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Permission>>> GetAllPermissions()
        {
            try
            {
                var permissions = await _permissionRepository.GetAllPermissionsAsync();
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene permisos de un rol específico
        /// </summary>
        [HttpGet("by-role/{roleId:guid}")]
        public async Task<ActionResult<IEnumerable<Permission>>> GetPermissionsByRole(Guid roleId)
        {
            try
            {
                var permissions = await _permissionRepository.GetPermissionsByRoleIdAsync(roleId);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos del rol");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene permisos de un usuario
        /// </summary>
        [HttpGet("by-user/{userId:guid}")]
        public async Task<ActionResult<IEnumerable<Permission>>> GetPermissionsByUser(Guid userId)
        {
            try
            {
                var permissions = await _permissionRepository.GetPermissionsByUserIdAsync(userId);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos del usuario");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Asigna permisos a un rol
        /// </summary>
        [HttpPost("assign-to-role/{roleId:guid}")]
        public async Task<IActionResult> AssignPermissionsToRole(Guid roleId, [FromBody] List<Guid> permissionIds)
        {
            try
            {
                await _permissionRepository.AssignPermissionsToRoleAsync(roleId, permissionIds);
                return Ok(new { message = "Permisos asignados correctamente" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar permisos");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Remueve permisos de un rol
        /// </summary>
        [HttpDelete("remove-from-role/{roleId:guid}")]
        public async Task<IActionResult> RemovePermissionsFromRole(Guid roleId, [FromBody] List<Guid> permissionIds)
        {
            try
            {
                await _permissionRepository.RemovePermissionsFromRoleAsync(roleId, permissionIds);
                return Ok(new { message = "Permisos removidos correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al remover permisos");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Reemplaza completamente los permisos de un rol
        /// </summary>
        [HttpPut("replace-role-permissions/{roleId:guid}")]
        public async Task<IActionResult> ReplaceRolePermissions(
            Guid roleId,
            [FromBody] List<Guid> permissionIds)
        {
            try
            {
                await _permissionRepository.ReplaceRolePermissionsAsync(roleId, permissionIds);
                return Ok(new { message = "Permisos actualizados correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reemplazar permisos del rol");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene permisos asociados a un menú específico
        /// </summary>
        [HttpGet("by-menu/{menuId:guid}")]
        public async Task<ActionResult<IEnumerable<Permission>>> GetPermissionsByMenu(Guid menuId)
        {
            try
            {
                var permissions = await _permissionRepository.GetPermissionsByMenuIdAsync(menuId);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos del menú");
                return StatusCode(500, "Error interno del servidor");
            }
        }


    }
}