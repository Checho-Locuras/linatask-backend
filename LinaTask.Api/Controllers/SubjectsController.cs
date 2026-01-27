using LinaTask.Application.DTOs;
using LinaTask.Application.Services;
using LinaTask.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubjectsController : ControllerBase
    {
        private readonly ISubjectService _subjectService;
        private readonly ILogger<SubjectsController> _logger;

        public SubjectsController(ISubjectService subjectService, ILogger<SubjectsController> logger)
        {
            _subjectService = subjectService;
            _logger = logger;
        }

        [HttpGet("getAllSubjects")]
        public async Task<ActionResult<IEnumerable<SubjectDto>>> GetAllSubjects()
        {
            try
            {
                var subjects = await _subjectService.GetAllSubjectsAsync();
                return Ok(subjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener materias");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getActiveSubjects")]
        public async Task<ActionResult<IEnumerable<SubjectDto>>> GetActiveSubjects()
        {
            try
            {
                var subjects = await _subjectService.GetActiveSubjectsAsync();
                return Ok(subjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener materias activas");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getSubjectsByCategory/{category}")]
        public async Task<ActionResult<IEnumerable<SubjectDto>>> GetSubjectsByCategory(string category)
        {
            try
            {
                var subjects = await _subjectService.GetSubjectsByCategoryAsync(category);
                return Ok(subjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener materias por categoría");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getSubjectById/{id:guid}")]
        public async Task<ActionResult<SubjectDto>> GetSubjectById(Guid id)
        {
            try
            {
                var subject = await _subjectService.GetSubjectByIdAsync(id);
                if (subject == null)
                    return NotFound($"Materia con ID {id} no encontrada");

                return Ok(subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener materia {SubjectId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost("createSubject")]
        public async Task<ActionResult<SubjectDto>> CreateSubject([FromBody] CreateSubjectDto createSubjectDto)
        {
            try
            {
                var subject = await _subjectService.CreateSubjectAsync(createSubjectDto);
                return CreatedAtAction(nameof(GetSubjectById), new { id = subject.Id }, subject);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear materia");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("updateSubject/{id:guid}")]
        public async Task<ActionResult<SubjectDto>> UpdateSubject(Guid id, [FromBody] UpdateSubjectDto updateSubjectDto)
        {
            try
            {
                var subject = await _subjectService.UpdateSubjectAsync(id, updateSubjectDto);
                return Ok(subject);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Materia con ID {id} no encontrada");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar materia {SubjectId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpDelete("deleteSubject/{id:guid}")]
        public async Task<IActionResult> DeleteSubject(Guid id)
        {
            try
            {
                var deleted = await _subjectService.DeleteSubjectAsync(id);
                if (!deleted)
                    return NotFound($"Materia con ID {id} no encontrada");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar materia {SubjectId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}