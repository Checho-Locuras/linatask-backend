using LinaTask.Api.Common;
using LinaTask.Application.DTOs;
using LinaTask.Application.Services;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Enums;
using LinaTask.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : BaseController
    {
        private readonly INotificationService _service;
        private readonly ITutoringSessionService _sessionService;

        public NotificationsController(INotificationService service, ITutoringSessionService sessionService)
        {
            _service = service;
            _sessionService = sessionService;
        }

        // GET api/notifications?isRead=false&page=1&pageSize=20
        [HttpGet]
        public async Task<ActionResult<PagedNotificationsDto>> GetAll(
            [FromQuery] NotificationQueryDto query)
        {
            var result = await _service.GetByUserAsync(CurrentUserId, query);
            return Ok(result);
        }

        // GET api/notifications/summary  ← usado por el badge de la campana
        [HttpGet("summary")]
        public async Task<ActionResult<NotificationSummaryDto>> GetSummary()
        {
            var result = await _service.GetSummaryAsync(CurrentUserId);
            return Ok(result);
        }

        // POST api/notifications  ← uso interno/admin (también llaman los helpers del servicio)
        [HttpPost]
        public async Task<ActionResult<NotificationDto>> Create(
            [FromBody] CreateNotificationDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetAll), result);
        }

        // PATCH api/notifications/{id}/read
        [HttpPatch("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var updated = await _service.MarkAsReadAsync(id, CurrentUserId);
            return updated ? NoContent() : NotFound();
        }

        // PATCH api/notifications/read-all
        [HttpPatch("read-all")]
        public async Task<ActionResult<int>> MarkAllAsRead()
        {
            var count = await _service.MarkAllAsReadAsync(CurrentUserId);
            return Ok(new { updated = count });
        }

        // DELETE api/notifications/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _service.DeleteAsync(id, CurrentUserId);
            return deleted ? NoContent() : NotFound();
        }

        // DELETE api/notifications/read  ← limpia todas las leídas
        [HttpDelete("read")]
        public async Task<ActionResult<int>> DeleteAllRead()
        {
            var count = await _service.DeleteAllReadAsync(CurrentUserId);
            return Ok(new { deleted = count });
        }

        [HttpPost("{id}/actions/{actionType}")]
        [Authorize]
        public async Task<IActionResult> ExecuteAction(
            Guid id,
            string actionType,
            [FromBody] string? payload)
        {
            var userId = CurrentUserId; // tu helper existente

            // 1. Marcar notificación como leída
            await _service.MarkAsReadAsync(id, userId);

            // 2. Ejecutar acción según el tipo
            switch (actionType)
            {
                case "accept_session":
                    {
                        if (!Guid.TryParse(payload, out var sessionId))
                            return BadRequest(new { message = "Payload inválido" });

                        try
                        {
                            await _sessionService.UpdateSessionAsync(sessionId, new UpdateTutoringSessionDto
                            {
                                Status = SessionStatus.Ready
                            });
                            return Ok(new NotificationActionResult
                            {
                                Success = true,
                                Message = "Sesión aceptada correctamente",
                                RedirectUrl = "/teacher/sessions"
                            });
                        }
                        catch (Exception ex)
                        {
                            return BadRequest(new NotificationActionResult
                            {
                                Success = false,
                                Error = ex.Message
                            });
                        }
                    }

                case "reject_session":
                    {
                        if (!Guid.TryParse(payload, out var sessionId))
                            return BadRequest(new { message = "Payload inválido" });

                        try
                        {
                            await _sessionService.UpdateSessionAsync(sessionId, new UpdateTutoringSessionDto
                            {
                                Status = SessionStatus.Cancelled
                            });

                            return Ok(new NotificationActionResult
                            {
                                Success = true,
                                Message = "Sesión rechazada",
                                RedirectUrl = "/teacher/sessions"
                            });
                        }
                        catch (Exception ex)
                        {
                            return BadRequest(new NotificationActionResult
                            {
                                Success = false,
                                Error = ex.Message
                            });
                        }
                    }

                case "navigate":
                    return Ok(new NotificationActionResult
                    {
                        Success = true,
                        RedirectUrl = payload
                    });

                default:
                    return BadRequest(new NotificationActionResult
                    {
                        Success = false,
                        Error = $"Acción desconocida: {actionType}"
                    });
            }
        }
    }
}
