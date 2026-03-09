using LinaTask.Api.Attributes;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/marketplace/ratings")]
    [Authorize]
    public class MarketplaceRatingsController : ControllerBase
    {
        private readonly IMarketplaceRatingService _ratingService;
        private readonly ILogger<MarketplaceRatingsController> _logger;

        public MarketplaceRatingsController(
            IMarketplaceRatingService ratingService,
            ILogger<MarketplaceRatingsController> logger)
        {
            _ratingService = ratingService;
            _logger = logger;
        }

        private Guid? CurrentUserId()
        {
            var claim = User.FindFirst("sub")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }

        [HttpGet("by-task/{taskId:guid}")]
        public async Task<ActionResult<IEnumerable<MarketplaceRatingDto>>> GetByTask(Guid taskId)
        {
            try { return Ok(await _ratingService.GetByTaskIdAsync(taskId)); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        [HttpGet("by-user/{userId:guid}")]
        public async Task<ActionResult<IEnumerable<MarketplaceRatingDto>>> GetByUser(Guid userId)
        {
            try { return Ok(await _ratingService.GetByUserAsync(userId)); }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        [HttpPost]
        [PermissionAuthorize("MARKETPLACE.RATE")]
        public async Task<ActionResult<MarketplaceRatingDto>> Create([FromBody] CreateMarketplaceRatingDto dto)
        {
            try
            {
                var userId = CurrentUserId();
                if (!userId.HasValue) return Unauthorized();
                return Ok(await _ratingService.CreateAsync(dto, userId.Value));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error creating rating"); return StatusCode(500, "Internal server error"); }
        }
    }
}
