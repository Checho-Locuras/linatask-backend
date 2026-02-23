using LinaTask.Api.Attributes;
using LinaTask.Application.DTOs;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TutoringSessionsController : ControllerBase
    {
        private readonly ITutoringSessionService _sessionService;
        private readonly ILogger<TutoringSessionsController> _logger;

        public TutoringSessionsController(
            ITutoringSessionService sessionService,
            ILogger<TutoringSessionsController> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        [HttpGet("getAllTutoringSessions")]
        public async Task<ActionResult<IEnumerable<TutoringSessionDto>>> GetAllTutoringSessions()
        {
            try
            {
                var sessions = await _sessionService.GetAllSessionsAsync();
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener sesiones");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getTutoringSessionById/{id:guid}")]
        [PermissionAuthorize("SESSION.VIEW")]
        public async Task<ActionResult<TutoringSessionDto>> GetTutoringSessionById(Guid id)
        {
            try
            {
                var session = await _sessionService.GetSessionByIdAsync(id);
                if (session == null)
                    return NotFound($"Sesión con ID {id} no encontrada");

                return Ok(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener sesión {SessionId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getTutoringSessionsByStudentId/{studentId:guid}")]
        [PermissionAuthorize("SESSION.VIEW")]
        public async Task<ActionResult<IEnumerable<TutoringSessionDto>>> GetTutoringSessionsByStudentId(Guid studentId)
        {
            try
            {
                var sessions = await _sessionService.GetSessionsByStudentAsync(studentId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener sesiones del estudiante");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getTutoringSessionsByTeacherId/{teacherId:guid}")]
        [PermissionAuthorize("SESSION.VIEW")]
        public async Task<ActionResult<IEnumerable<TutoringSessionDto>>> GetTutoringSessionsByTeacherId(Guid teacherId)
        {
            try
            {
                var sessions = await _sessionService.GetSessionsByTeacherAsync(teacherId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener sesiones del profesor");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getTutoringSessionsByStatus/{status}")]
        [PermissionAuthorize("SESSION.VIEW")]
        public async Task<ActionResult<IEnumerable<TutoringSessionDto>>> GetTutoringSessionsByStatus(string status)
        {
            try
            {
                // Parsear el string a enum, ignorando mayúsculas/minúsculas
                if (!Enum.TryParse<SessionStatus>(status, ignoreCase: true, out var sessionStatus))
                    return BadRequest(new { message = $"Estado inválido: '{status}'. Valores válidos: {string.Join(", ", Enum.GetNames<SessionStatus>())}" });

                var sessions = await _sessionService.GetSessionsByStatusAsync(sessionStatus);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener sesiones por estado");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getUpcomingTutoringSessions")]
        [PermissionAuthorize("SESSION.VIEW")]
        public async Task<ActionResult<IEnumerable<TutoringSessionDto>>> GetUpcomingTutoringSessions([FromQuery] Guid? userId = null)
        {
            try
            {
                var sessions = await _sessionService.GetUpcomingSessionsAsync(userId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener sesiones próximas");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getTutoringSessionStats")]
        [PermissionAuthorize("SESSION.STATS")]
        public async Task<ActionResult<SessionStatsDto>> GetTutoringSessionStats([FromQuery] Guid? userId = null)
        {
            try
            {
                var stats = await _sessionService.GetSessionStatsAsync(userId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost("createTutoringSession")]
        [PermissionAuthorize("SESSION.CREATE")]
        public async Task<ActionResult<TutoringSessionDto>> CreateTutoringSession([FromBody] CreateTutoringSessionDto createDto)
        {
            try
            {
                var session = await _sessionService.CreateSessionAsync(createDto);
                return CreatedAtAction(nameof(GetTutoringSessionById), new { id = session.Id }, session);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear sesión");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("updateTutoringSession/{id:guid}")]
        [PermissionAuthorize("SESSION.EDIT")]
        public async Task<ActionResult<TutoringSessionDto>> UpdateTutoringSession(Guid id, [FromBody] UpdateTutoringSessionDto updateDto)
        {
            try
            {
                var session = await _sessionService.UpdateSessionAsync(id, updateDto);
                return Ok(session);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Sesión con ID {id} no encontrada");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar sesión {SessionId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpDelete("deleteTutoringSession/{id:guid}")]
        [PermissionAuthorize("SESSION.DELETE")]
        public async Task<IActionResult> DeleteTutoringSession(Guid id)
        {
            try
            {
                var deleted = await _sessionService.DeleteSessionAsync(id);
                if (!deleted)
                    return NotFound($"Sesión con ID {id} no encontrada");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar sesión {SessionId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost("{id:guid}/video/join")]
        public async Task<ActionResult<VideoRoomAccessDto>> JoinVideoRoom(Guid id)
        {
            try
            {
                // Obtener el ID del usuario actual desde el token
                var userIdClaim = User.FindFirst("sub")?.Value ??
                                  User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdClaim, out var requestingUserId))
                {
                    return Unauthorized("No se pudo identificar al usuario");
                }

                _logger.LogInformation("Usuario {UserId} solicitando unirse a sala de video {SessionId}", requestingUserId, id);

                var access = await _sessionService.GetOrCreateVideoRoomAsync(id, requestingUserId);
                return Ok(access);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Sesión no encontrada: {SessionId}", id);
                return NotFound($"Sesión con ID {id} no encontrada");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado a sala de video {SessionId} por usuario {UserId}", id, User.FindFirst("sub")?.Value);
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operación inválida para sala de video {SessionId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al unirse a sala de video {SessionId}", id);
                return StatusCode(500, "Error interno del servidor al procesar la solicitud de video");
            }
        }

        // ─────────────────────────────────────────────────
        // Endpoint adicional: Obtener token directamente (si lo necesitas)
        // ─────────────────────────────────────────────────
        [HttpGet("{id:guid}/video/token")]
        public async Task<ActionResult<string>> GetVideoToken(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst("sub")?.Value ??
                                  User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdClaim, out var requestingUserId))
                {
                    return Unauthorized();
                }

                var session = await _sessionService.GetSessionByIdAsync(id, requestingUserId);
                if (session == null)
                    return NotFound();

                if (string.IsNullOrEmpty(session.VideoToken))
                    return NotFound("No hay token de video disponible para esta sesión");

                return Ok(new { token = session.VideoToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener token de video");
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}