using LinaTask.Api.Hubs;
using LinaTask.Application.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LinaTask.Api.Services
{
    public class SignalRNotificationPusher : INotificationPusher
    {
        private readonly IHubContext<NotificationHub> _hub;
        private readonly ILogger<SignalRNotificationPusher> _logger;

        public SignalRNotificationPusher(
            IHubContext<NotificationHub> hub,
            ILogger<SignalRNotificationPusher> logger)
        {
            _hub = hub;
            _logger = logger;
        }

        public async Task PushAsync(Guid userId, object payload)
        {
            try
            {
                await _hub.Clients
                    .Group(userId.ToString())
                    .SendAsync("ReceiveNotification", payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo enviar notificación en tiempo real al usuario {UserId}", userId);
                // No propagamos: el guardado en DB ya fue exitoso
            }
        }
    }
}