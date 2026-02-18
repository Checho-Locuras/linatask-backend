using LinaTask.Api.Attributes;
using LinaTask.Application.DTOs;
using LinaTask.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeacherAvailabilityController : ControllerBase
    {
        private readonly ITeacherAvailabilityService _service;
        private readonly ILogger<TeacherAvailabilityController> _logger;

        public TeacherAvailabilityController(
            ITeacherAvailabilityService service,
            ILogger<TeacherAvailabilityController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ── GET: disponibilidad de un docente (bloques crudos) ──
        [HttpGet("getByTeacher/{teacherId:guid}")]
        //[PermissionAuthorize("AVAILABILITY.VIEW")]
        public async Task<ActionResult<IEnumerable<TeacherAvailabilityDto>>> GetByTeacher(Guid teacherId)
        {
            try
            {
                var result = await _service.GetByTeacherIdAsync(teacherId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener disponibilidad del docente {TeacherId}", teacherId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // ── GET: disponibilidad semanal con slots y colisiones ──
        [HttpGet("getWeekly/{teacherId:guid}")]
        //[PermissionAuthorize("AVAILABILITY.VIEW")]
        public async Task<ActionResult<TeacherWeeklyAvailabilityDto>> GetWeekly(
            Guid teacherId,
            [FromQuery] string? weekStart = null)
        {
            try
            {
                var parsedDate = weekStart != null
                    ? DateTime.Parse(weekStart)
                    : DateTime.Today;

                var result = await _service.GetWeeklyAvailabilityAsync(teacherId, parsedDate);
                return Ok(result);
            }
            catch (FormatException)
            {
                return BadRequest("Formato de fecha inválido. Use yyyy-MM-dd");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener vista semanal del docente {TeacherId}", teacherId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // ── GET: un bloque por id ──
        [HttpGet("{id:guid}")]
        //[PermissionAuthorize("AVAILABILITY.VIEW")]
        public async Task<ActionResult<TeacherAvailabilityDto>> GetById(Guid id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                if (result == null)
                    return NotFound($"Disponibilidad {id} no encontrada");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener disponibilidad {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // ── POST: crear un bloque ──
        [HttpPost("create/{teacherId:guid}")]
        //[PermissionAuthorize("AVAILABILITY.CREATE")]
        public async Task<ActionResult<TeacherAvailabilityDto>> Create(
            Guid teacherId,
            [FromBody] CreateAvailabilityDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(teacherId, dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear disponibilidad para docente {TeacherId}", teacherId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // ── PUT: actualizar un bloque ──
        [HttpPut("update/{id:guid}")]
        //[PermissionAuthorize("AVAILABILITY.EDIT")]
        public async Task<ActionResult<TeacherAvailabilityDto>> Update(
            Guid id,
            [FromBody] UpdateAvailabilityDto dto)
        {
            try
            {
                var result = await _service.UpdateAsync(id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Disponibilidad {id} no encontrada");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar disponibilidad {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // ── DELETE: eliminar un bloque ──
        [HttpDelete("delete/{id:guid}")]
        //[PermissionAuthorize("AVAILABILITY.DELETE")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var deleted = await _service.DeleteAsync(id);
                if (!deleted)
                    return NotFound($"Disponibilidad {id} no encontrada");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar disponibilidad {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // ── POST: guardar disponibilidad completa (bulk) ──
        /// <summary>
        /// Reemplaza TODA la disponibilidad del docente.
        /// Llamar desde el calendario al presionar "Guardar".
        /// </summary>
        [HttpPost("bulkSave")]
        //[PermissionAuthorize("AVAILABILITY.CREATE")]
        public async Task<ActionResult<IEnumerable<TeacherAvailabilityDto>>> BulkSave(
            [FromBody] BulkSaveAvailabilityDto dto)
        {
            try
            {
                var result = await _service.BulkSaveAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en bulk save de disponibilidad para {TeacherId}", dto.TeacherId);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}