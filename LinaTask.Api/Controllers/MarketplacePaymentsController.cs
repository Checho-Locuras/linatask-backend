using LinaTask.Api.Attributes;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/marketplace/payments")]
    [Authorize]
    public class MarketplacePaymentsController : ControllerBase
    {
        private readonly IMarketplacePaymentService _paymentService;
        private readonly ILogger<MarketplacePaymentsController> _logger;

        public MarketplacePaymentsController(
            IMarketplacePaymentService paymentService,
            ILogger<MarketplacePaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        private Guid? CurrentUserId()
        {
            var claim = User.FindFirst("sub")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }

        [HttpGet("by-task/{taskId:guid}")]
        public async Task<ActionResult<MarketplacePaymentDto>> GetByTask(Guid taskId)
        {
            try
            {
                var payment = await _paymentService.GetByTaskIdAsync(taskId);
                return payment is null ? NotFound() : Ok(payment);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error"); return StatusCode(500, "Internal server error"); }
        }

        // Iniciar pago (estudiante)
        [HttpPost("initiate")]
        [PermissionAuthorize("MARKETPLACE.PAY")]
        public async Task<ActionResult<MarketplacePaymentDto>> Initiate([FromBody] InitiatePaymentDto dto)
        {
            try
            {
                var userId = CurrentUserId();
                if (!userId.HasValue) return Unauthorized();
                return Ok(await _paymentService.InitiatePaymentAsync(dto, userId.Value));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error initiating payment"); return StatusCode(500, "Internal server error"); }
        }

        // Confirmar retención (webhook de pasarela de pago)
        [HttpPost("confirm-held/{taskId:guid}")]
        public async Task<ActionResult<MarketplacePaymentDto>> ConfirmHeld(Guid taskId)
        {
            try { return Ok(await _paymentService.ConfirmPaymentHeldAsync(taskId)); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error confirming payment"); return StatusCode(500, "Internal server error"); }
        }

        // Aprobar y liberar pago (estudiante)
        [HttpPost("release/{taskId:guid}")]
        [PermissionAuthorize("MARKETPLACE.PAY")]
        public async Task<ActionResult<MarketplacePaymentDto>> Release(Guid taskId)
        {
            try
            {
                var userId = CurrentUserId();
                if (!userId.HasValue) return Unauthorized();
                return Ok(await _paymentService.ReleasePaymentAsync(taskId, userId.Value));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error releasing payment"); return StatusCode(500, "Internal server error"); }
        }

        // Reembolso (admin)
        [HttpPost("refund/{taskId:guid}")]
        [PermissionAuthorize("MARKETPLACE.REFUND")]
        public async Task<ActionResult<MarketplacePaymentDto>> Refund(Guid taskId)
        {
            try { return Ok(await _paymentService.RefundPaymentAsync(taskId)); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error refunding payment"); return StatusCode(500, "Internal server error"); }
        }
    }
}
