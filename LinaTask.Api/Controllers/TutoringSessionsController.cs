using LinaTask.Application.DTOs;
using LinaTask.Application.Services;
using LinaTask.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        /// <summary>
        /// Obtiene todas las sesiones
        /// </summary>
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

        /// <summary>
        /// Obtiene una sesión por ID
        /// </summary>
        [HttpGet("getTutoringSessionById/{id:guid}")]
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

        /// <summary>
        /// Obtiene sesiones por estudiante
        /// </summary>
        [HttpGet("getTutoringSessionsByStudentId/{studentId:guid}")]
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

        /// <summary>
        /// Obtiene sesiones por profesor
        /// </summary>
        [HttpGet("getTutoringSessionsByTeacherId/{teacherId:guid}")]
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

        /// <summary>
        /// Obtiene sesiones por estado
        /// </summary>
        [HttpGet("getTutoringSessionsByStatus/{status}")]
        public async Task<ActionResult<IEnumerable<TutoringSessionDto>>> GetTutoringSessionsByStatus(string status)
        {
            try
            {
                var sessions = await _sessionService.GetSessionsByStatusAsync(status);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener sesiones por estado");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene sesiones próximas (opcionalmente filtradas por usuario)
        /// </summary>
        [HttpGet("getUpcomingTutoringSessions")]
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

        /// <summary>
        /// Obtiene estadísticas de sesiones
        /// </summary>
        [HttpGet("getTutoringSessionStats")]
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

        /// <summary>
        /// Crea una nueva sesión
        /// </summary>
        [HttpPost("createTutoringSession")]
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

        /// <summary>
        /// Actualiza una sesión
        /// </summary>
        [HttpPut("updateTutoringSession/{id:guid}")]
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

        /// <summary>
        /// Elimina una sesión
        /// </summary>
        [HttpDelete("deleteTutoringSession/{id:guid}")]
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
    }
}