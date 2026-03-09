using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LinaTask.Api.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? Context.User?.FindFirst("sub")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Cada usuario se une a su propio grupo (userId como nombre)
                // Así podemos hacer _hub.Clients.Group(userId).SendAsync(...)
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                _logger.LogInformation("Usuario {UserId} conectado a NotificationHub", userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? Context.User?.FindFirst("sub")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                _logger.LogInformation("Usuario {UserId} desconectado de NotificationHub", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
