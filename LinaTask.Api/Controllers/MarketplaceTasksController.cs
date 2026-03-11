using LinaTask.Api.Attributes;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Enums;
using LinaTask.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskStatus = LinaTask.Domain.Enums.TaskStatus;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/marketplace/tasks")]
    [Authorize]
    public class MarketplaceTasksController : ControllerBase
    {
        private readonly IMarketplaceTaskService _taskService;
        private readonly ILogger<MarketplaceTasksController> _logger;

        public MarketplaceTasksController(
            IMarketplaceTaskService taskService,
            ILogger<MarketplaceTasksController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        private Guid? CurrentUserId()
        {
            var claim = User.FindFirst("sub")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }

        // ── GET all / open ─────────────────────────────────────
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MarketplaceTaskDto>>> GetAll([FromQuery] bool onlyOpen = false)
        {
            try
            {
                return Ok(await _taskService.GetAllAsync(onlyOpen));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting marketplace tasks");
                return StatusCode(500, "Internal server error");
            }
        }

        // ── GET urgent ─────────────────────────────────────────
        [HttpGet("urgent")]
        public async Task<ActionResult<IEnumerable<MarketplaceTaskDto>>> GetUrgent()
        {
            try { return Ok(await _taskService.GetUrgentAsync()); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        // ── GET by status ──────────────────────────────────────
        [HttpGet("by-status/{status}")]
        public async Task<ActionResult<IEnumerable<MarketplaceTaskDto>>> GetByStatus(string status)
        {
            try
            {
                if (!Enum.TryParse<TaskStatus>(status, ignoreCase: true, out var parsed))
                    return BadRequest($"Invalid status '{status}'. Valid: {string.Join(", ", Enum.GetNames<TaskStatus>())}");
                return Ok(await _taskService.GetByStatusAsync(parsed));
            }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        // ── GET by student ─────────────────────────────────────
        [HttpGet("by-student/{studentId:guid}")]
        public async Task<ActionResult<IEnumerable<MarketplaceTaskDto>>> GetByStudent(Guid studentId)
        {
            try { return Ok(await _taskService.GetByStudentIdAsync(studentId)); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        // ── GET by teacher ─────────────────────────────────────
        [HttpGet("by-teacher/{teacherId:guid}")]
        public async Task<ActionResult<IEnumerable<MarketplaceTaskDto>>> GetByTeacher(Guid teacherId)
        {
            try { return Ok(await _taskService.GetByTeacherIdAsync(teacherId)); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        // ── GET by id ──────────────────────────────────────────
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<MarketplaceTaskDto>> GetById(Guid id)
        {
            try
            {
                var task = await _taskService.GetByIdAsync(id);
                return task is null ? NotFound($"Task {id} not found") : Ok(task);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        // ── GET suggested price ────────────────────────────────
        [HttpGet("suggested-price")]
        public async Task<ActionResult<SuggestedPriceDto>> GetSuggestedPrice(
            [FromQuery] WorkType workType,
            [FromQuery] AcademicLevel academicLevel,
            [FromQuery] bool isUrgent = false,
            [FromQuery] DateTime? deadline = null)
        {
            try
            {
                var dl = deadline ?? DateTime.UtcNow.AddDays(7);
                return Ok(await _taskService.GetSuggestedPriceAsync(workType, academicLevel, isUrgent, dl));
            }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        // ── GET stats ──────────────────────────────────────────
        [HttpGet("stats")]
        public async Task<ActionResult<MarketplaceStatsDto>> GetStats([FromQuery] Guid? userId = null)
        {
            try { return Ok(await _taskService.GetStatsAsync(userId)); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        // ── POST create ────────────────────────────────────────
        [HttpPost]
        [PermissionAuthorize("MARKETPLACE.CREATE")]
        public async Task<ActionResult<MarketplaceTaskDto>> Create([FromBody] CreateMarketplaceTaskDto dto)
        {
            try
            {
                var userId = CurrentUserId();
                if (!userId.HasValue) return Unauthorized();

                var task = await _taskService.CreateAsync(dto, userId.Value);
                return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
            }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error creating marketplace task"); return StatusCode(500, "Internal server error"); }
        }

        // ── PUT update ─────────────────────────────────────────
        [HttpPut("{id:guid}")]
        [PermissionAuthorize("MARKETPLACE.EDIT")]
        public async Task<ActionResult<MarketplaceTaskDto>> Update(Guid id, [FromBody] UpdateMarketplaceTaskDto dto)
        {
            try
            {
                var userId = CurrentUserId();
                if (!userId.HasValue) return Unauthorized();
                return Ok(await _taskService.UpdateAsync(id, dto, userId.Value));
            }
            catch (KeyNotFoundException) { return NotFound($"Task {id} not found"); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        // ── DELETE ─────────────────────────────────────────────
        [HttpDelete("{id:guid}")]
        [PermissionAuthorize("MARKETPLACE.DELETE")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userId = CurrentUserId();
                if (!userId.HasValue) return Unauthorized();
                var deleted = await _taskService.DeleteAsync(id, userId.Value);
                return deleted ? NoContent() : NotFound($"Task {id} not found");
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        // Método para realizar la entrega de la tarea
        [HttpPut("{id:guid}/deliver")]
        [PermissionAuthorize("MARKETPLACE.DELIVER")]
        public async Task<ActionResult<MarketplaceTaskDto>> Deliver(
            Guid id, [FromForm] IFormFile file)
        {
            try
            {
                var userId = CurrentUserId();
                if (!userId.HasValue) return Unauthorized();
                return Ok(await _taskService.DeliverAsync(id, file, userId.Value));
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error delivering task"); return StatusCode(500, "Internal server error"); }
        }
    }
}
