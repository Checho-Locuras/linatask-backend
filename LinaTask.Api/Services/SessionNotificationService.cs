using LinaTask.Application.Services.Interfaces;
using LinaTask.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace LinaTask.Api.Services
{
    public class SessionNotificationService : ISessionNotificationService
    {
        private readonly IHubContext<ChatHub> _hub;

        public SessionNotificationService(IHubContext<ChatHub> hub)
        {
            _hub = hub;
        }

        public Task NotifyNewSessionRequestAsync(Guid teacherId, object payload) =>
            _hub.Clients.Group($"user_{teacherId}").SendAsync("NewSessionRequest", payload);

        public Task NotifySessionUpdatedAsync(Guid studentId, Guid teacherId, object payload) =>
            Task.WhenAll(
                _hub.Clients.Group($"user_{studentId}").SendAsync("SessionUpdated", payload),
                _hub.Clients.Group($"user_{teacherId}").SendAsync("SessionUpdated", payload)
            );

        public Task NotifySessionRoomReadyAsync(Guid userId, object payload) =>
            _hub.Clients.Group($"user_{userId}").SendAsync("SessionRoomReady", payload);

        public Task NotifySessionRatedAsync(Guid teacherId, object payload) =>
            _hub.Clients.Group($"user_{teacherId}").SendAsync("SessionRated", payload);
    }
}