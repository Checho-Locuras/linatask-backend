using LinaTask.Api.Attributes;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Enums;
using LinaTask.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IMarketplacePaymentService _paymentService;
        private readonly IMercadoPagoService _mpService;
        private readonly IConfiguration _config;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IMarketplacePaymentService paymentService,
            IMercadoPagoService mpService,
            IConfiguration config,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _mpService = mpService;
            _config = config;
            _logger = logger;
        }

        // ── Iniciar pago ─────────────────────────────────────────────
        [HttpPost("initiate")]
        [PermissionAuthorize("MARKETPLACE.PAY")]
        public async Task<ActionResult<InitiatePaymentResponseDto>> Initiate([FromBody] InitiatePaymentRequestDto dto)
        {
            var studentId = GetCurrentUserId();
            var studentEmail = GetCurrentUserEmail();
            var studentName = GetCurrentUserName();

            // 1. Crear registro de pago en BD (estado Pending)
            var payment = await _paymentService.InitiatePaymentAsync(dto, studentId);

            // 2. Crear preferencia en MercadoPago
            var referenceId = (dto.TaskId ?? dto.SessionId)
                ?? throw new InvalidOperationException("TaskId or SessionId required");

            var preference = await _mpService.CreatePreferenceAsync(
                new CreatePaymentPreferenceDto(
                    dto.Context == PaymentContextType.Session ? "Sesión de tutoría" : "Tarea académica",
                    payment.Amount,
                    studentEmail,
                    studentName,
                    referenceId.ToString(),
                    dto.Context.ToString().ToLower(),
                    _config["App:FrontendUrl"]
                        ?? throw new InvalidOperationException("App:FrontendUrl not configured"),
                    _config["App:BackendUrl"]
                        ?? throw new InvalidOperationException("App:BackendUrl not configured")
                )
            );

            return Ok(new InitiatePaymentResponseDto
            {
                PaymentId = payment.Id,
                PreferenceId = preference.PreferenceId,
                PublicKey = preference.PublicKey,
                Amount = payment.Amount,
                PlatformFee = payment.PlatformFee,
                TeacherAmount = payment.TeacherAmount
            });
        }

        // ── Confirmar pago ───────────────────────────────────────────
        [HttpPost("confirm")]
        [PermissionAuthorize("MARKETPLACE.PAY")]
        public async Task<ActionResult> Confirm([FromBody] ConfirmPaymentDto dto)
        {
            // Verificar con MercadoPago que realmente fue aprobado
            MercadoPagoPaymentInfo mpInfo;
            try
            {
                mpInfo = await _mpService.GetPaymentInfoAsync(dto.ExternalPaymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consulting MercadoPago for payment {Id}", dto.ExternalPaymentId);
                return StatusCode(502, new { message = "No se pudo verificar el pago con MercadoPago" });
            }

            if (mpInfo.Status != "approved")
                return BadRequest(new { message = $"Pago no aprobado: {mpInfo.StatusDetail}" });

            if (dto.TaskId.HasValue)
                await _paymentService.ConfirmPaymentHeldAsync(dto.TaskId.Value, dto.ExternalPaymentId);
            else if (dto.SessionId.HasValue)
                await _paymentService.ConfirmSessionPaymentAsync(dto.SessionId.Value, dto.ExternalPaymentId);
            else
                return BadRequest(new { message = "TaskId or SessionId required" });

            return Ok(new { message = "Pago confirmado y fondos retenidos" });
        }

        // ── Webhook MercadoPago ──────────────────────────────────────
        // MercadoPago envía notificaciones POST aquí.
        // Debe responder 200 rápido — la lógica pesada va a background o fire-and-forget.
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook([FromBody] JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("type", out var typeProp))
                    return Ok();

                var type = typeProp.GetString();
                if (type != "payment")
                    return Ok();

                if (!body.TryGetProperty("data", out var dataProp) ||
                    !dataProp.TryGetProperty("id", out var idProp))
                    return Ok();

                var paymentId = idProp.GetString()!;

                // Consultar estado real en MercadoPago
                var mpInfo = await _mpService.GetPaymentInfoAsync(paymentId);

                if (mpInfo.Status == "approved")
                {
                    await _paymentService.HandleWebhookApprovalAsync(paymentId);
                    _logger.LogInformation("Webhook: approved payment {Id} processed", paymentId);
                }
                else
                {
                    _logger.LogInformation("Webhook: payment {Id} status {Status} — no action", paymentId, mpInfo.Status);
                }
            }
            catch (Exception ex)
            {
                // Siempre 200 para que MP no reintente indefinidamente con el mismo error
                _logger.LogError(ex, "Error processing MercadoPago webhook");
            }

            return Ok();
        }

        // ── Helpers ──────────────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? throw new UnauthorizedAccessException("User ID not found in token");
            return Guid.Parse(claim);
        }

        private string GetCurrentUserEmail() =>
            User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue("email")
            ?? string.Empty;

        private string GetCurrentUserName() =>
            User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("name")
            ?? "Usuario";
    }
}