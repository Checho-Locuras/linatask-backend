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
    [Route("api/marketplace/offers")]
    [Authorize]
    public class TaskOffersController : ControllerBase
    {
        private readonly ITaskOfferService _offerService;
        private readonly ILogger<TaskOffersController> _logger;

        public TaskOffersController(ITaskOfferService offerService, ILogger<TaskOffersController> logger)
        {
            _offerService = offerService;
            _logger = logger;
        }

        private Guid? CurrentUserId()
        {
            var claim = User.FindFirst("sub")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }

        [HttpGet("by-task/{taskId:guid}")]
        public async Task<ActionResult<IEnumerable<TaskOfferDto>>> GetByTask(Guid taskId)
        {
            try { return Ok(await _offerService.GetByTaskIdAsync(taskId)); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        [HttpGet("by-teacher/{teacherId:guid}")]
        public async Task<ActionResult<IEnumerable<TaskOfferDto>>> GetByTeacher(Guid teacherId)
        {
            try { return Ok(await _offerService.GetByTeacherIdAsync(teacherId)); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TaskOfferDto>> GetById(Guid id)
        {
            try
            {
                var offer = await _offerService.GetByIdAsync(id);
                return offer is null ? NotFound() : Ok(offer);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        [HttpPost]
        [PermissionAuthorize("MARKETPLACE.OFFER")]
        public async Task<ActionResult<TaskOfferDto>> Create([FromBody] CreateTaskOfferDto dto)
        {
            try
            {
                var offer = await _offerService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = offer.Id }, offer);
            }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error creating offer"); return StatusCode(500, "Internal server error"); }
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<TaskOfferDto>> Update(Guid id, [FromBody] UpdateTaskOfferDto dto)
        {
            try
            {
                var userId = CurrentUserId();
                if (!userId.HasValue) return Unauthorized();
                return Ok(await _offerService.UpdateAsync(id, dto, userId.Value));
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        // Estudiante selecciona una oferta
        [HttpPost("select/{taskId:guid}")]
        [PermissionAuthorize("MARKETPLACE.SELECT_OFFER")]
        public async Task<ActionResult<MarketplaceTaskDto>> SelectOffer(Guid taskId, [FromBody] SelectOfferDto dto)
        {
            try
            {
                var userId = CurrentUserId();
                if (!userId.HasValue) return Unauthorized();
                return Ok(await _offerService.SelectOfferAsync(taskId, dto, userId.Value));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error selecting offer"); return StatusCode(500, "Internal server error"); }
        }

        // Docente retira su oferta
        [HttpPost("{id:guid}/withdraw")]
        public async Task<IActionResult> Withdraw(Guid id)
        {
            try
            {
                var userId = CurrentUserId();
                if (!userId.HasValue) return Unauthorized();
                await _offerService.WithdrawAsync(id, userId.Value);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }
    }
}
