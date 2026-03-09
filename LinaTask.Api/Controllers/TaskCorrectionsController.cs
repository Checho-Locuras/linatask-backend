using LinaTask.Api.Attributes;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/marketplace/corrections")]
    [Authorize]
    public class TaskCorrectionsController : ControllerBase
    {
        private readonly ITaskCorrectionService _correctionService;
        private readonly ILogger<TaskCorrectionsController> _logger;

        public TaskCorrectionsController(
            ITaskCorrectionService correctionService,
            ILogger<TaskCorrectionsController> logger)
        {
            _correctionService = correctionService;
            _logger = logger;
        }

        private Guid? CurrentUserId()
        {
            var claim = User.FindFirst("sub")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }

        [HttpGet("by-task/{taskId:guid}")]
        public async Task<ActionResult<IEnumerable<TaskCorrectionRequestDto>>> GetByTask(Guid taskId)
        {
            try { return Ok(await _correctionService.GetByTaskIdAsync(taskId)); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        [HttpPost]
        [PermissionAuthorize("MARKETPLACE.CORRECTION")]
        public async Task<ActionResult<TaskCorrectionRequestDto>> Create([FromBody] CreateCorrectionRequestDto dto)
        {
            try
            {
                var userId = CurrentUserId();
                if (!userId.HasValue) return Unauthorized();
                return Ok(await _correctionService.CreateAsync(dto, userId.Value));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        [HttpPost("{id:guid}/resolve")]
        [PermissionAuthorize("MARKETPLACE.CORRECTION")]
        public async Task<ActionResult<TaskCorrectionRequestDto>> Resolve(Guid id)
        {
            try
            {
                var userId = CurrentUserId();
                if (!userId.HasValue) return Unauthorized();
                return Ok(await _correctionService.ResolveAsync(id, userId.Value));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }
    }
}
