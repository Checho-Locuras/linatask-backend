using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationsController(INotificationService service)
        {
            _service = service;
        }

        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub")
                     ?? throw new UnauthorizedAccessException());

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
    }
}
