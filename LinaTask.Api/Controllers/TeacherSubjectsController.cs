using LinaTask.Application.DTOs;
using LinaTask.Application.Services;
using LinaTask.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherSubjectsController : ControllerBase
    {
        private readonly ITeacherSubjectService _teacherSubjectService;
        private readonly ILogger<TeacherSubjectsController> _logger;

        public TeacherSubjectsController(
            ITeacherSubjectService teacherSubjectService,
            ILogger<TeacherSubjectsController> logger)
        {
            _teacherSubjectService = teacherSubjectService;
            _logger = logger;
        }

        [HttpGet("getAllTeacherSubjects")]
        public async Task<ActionResult<IEnumerable<TeacherSubjectDto>>> GetAllTeacherSubjects()
        {
            try
            {
                var teacherSubjects = await _teacherSubjectService.GetAllTeacherSubjectsAsync();
                return Ok(teacherSubjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener relaciones profesor-materia");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getTeacherSubjectById/{id:guid}")]
        public async Task<ActionResult<TeacherSubjectDto>> GetTeacherSubjectById(Guid id)
        {
            try
            {
                var teacherSubject = await _teacherSubjectService.GetByIdAsync(id);
                if (teacherSubject == null)
                    return NotFound($"Relación con ID {id} no encontrada");

                return Ok(teacherSubject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener relación {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getTeacherSubjectsByTeacherId/{teacherId:guid}")]
        public async Task<ActionResult<IEnumerable<TeacherSubjectDto>>> GetTeacherSubjectsByTeacherId(Guid teacherId)
        {
            try
            {
                var teacherSubjects = await _teacherSubjectService.GetByTeacherAsync(teacherId);
                return Ok(teacherSubjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener materias del profesor");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getTeacherSubjectsBySubjectId/{subjectId:guid}")]
        public async Task<ActionResult<IEnumerable<TeacherSubjectDto>>> GetTeacherSubjectsBySubjectId(Guid subjectId)
        {
            try
            {
                var teacherSubjects = await _teacherSubjectService.GetBySubjectAsync(subjectId);
                return Ok(teacherSubjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener profesores de la materia");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost("createTeacherSubject")]
        public async Task<ActionResult<TeacherSubjectDto>> CreateTeacherSubject([FromBody] CreateTeacherSubjectDto createDto)
        {
            try
            {
                var teacherSubject = await _teacherSubjectService.CreateTeacherSubjectAsync(createDto);
                return CreatedAtAction(nameof(GetTeacherSubjectById), new { id = teacherSubject.Id }, teacherSubject);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear relación profesor-materia");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("updateTeacherSubject/{id:guid}")]
        public async Task<ActionResult<TeacherSubjectDto>> UpdateTeacherSubject(Guid id, [FromBody] UpdateTeacherSubjectDto updateDto)
        {
            try
            {
                var teacherSubject = await _teacherSubjectService.UpdateTeacherSubjectAsync(id, updateDto);
                return Ok(teacherSubject);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Relación con ID {id} no encontrada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar relación {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpDelete("deleteTeacherSubject/{id:guid}")]
        public async Task<IActionResult> DeleteTeacherSubject(Guid id)
        {
            try
            {
                var deleted = await _teacherSubjectService.DeleteTeacherSubjectAsync(id);
                if (!deleted)
                    return NotFound($"Relación con ID {id} no encontrada");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar relación {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}