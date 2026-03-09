// LinaTask.Api/Hubs/ChatHub.cs
//
// ARQUITECTURA:
//   ChatHub (Api)  →  ITutoringSessionService (Application)
//                  →  IChatService            (Application)
//
// HmsVideoService NO se inyecta aquí. El hub llama a
// ITutoringSessionService.GetOrCreateVideoRoomAsync(), que
// internamente usa IHmsVideoService. El hub solo orquesta
// eventos SignalR, nunca habla directo con 100ms.

using LinaTask.Application.DTOs;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs.Chat;
using LinaTask.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace LinaTask.Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        // userId → connectionIds (soporte multi-pestaña)
        private static readonly ConcurrentDictionary<string, HashSet<string>> _onlineUsers = new();

        private readonly IChatService _chatService;
        private readonly ITutoringSessionService _sessionService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IChatService chatService,
            ITutoringSessionService sessionService,
            ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _sessionService = sessionService;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────
        // CONEXIÓN / DESCONEXIÓN
        // ─────────────────────────────────────────────────

        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();

            _onlineUsers.AddOrUpdate(
                userId.ToString(),
                _ => new HashSet<string> { Context.ConnectionId },
                (_, conns) => { conns.Add(Context.ConnectionId); return conns; }
            );

            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            await NotifyContactsStatusAsync(userId.ToString(), isOnline: true);

            _logger.LogInformation("User {UserId} connected ({ConnectionId})", userId, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();

            if (_onlineUsers.TryGetValue(userId.ToString(), out var conns))
            {
                conns.Remove(Context.ConnectionId);
                if (conns.Count == 0)
                {
                    _onlineUsers.TryRemove(userId.ToString(), out _);
                    await NotifyContactsStatusAsync(userId.ToString(), isOnline: false);
                }
            }

            _logger.LogInformation("User {UserId} disconnected ({ConnectionId})", userId, Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        // ─────────────────────────────────────────────────
        // CHAT
        // ─────────────────────────────────────────────────

        public async Task SendMessage(SendMessageDto dto)
        {
            try
            {
                var senderId = GetCurrentUserId();
                var message = await _chatService.SaveMessageAsync(senderId, dto);
                var recipientId = await _chatService.GetOtherUserIdAsync(dto.ConversationId, senderId);

                await Clients.Group($"user_{recipientId}").SendAsync("ReceiveMessage", message);
                await Clients.Group($"user_{senderId}").SendAsync("ReceiveMessage", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                throw new HubException("Error sending message");
            }
        }

        public async Task StartTyping(Guid conversationId)
        {
            var senderId = GetCurrentUserId();
            var recipientId = await _chatService.GetOtherUserIdAsync(conversationId, senderId);
            await Clients.Group($"user_{recipientId}")
                .SendAsync("UserTyping", new { conversationId, userId = senderId });
        }

        public async Task StopTyping(Guid conversationId)
        {
            var senderId = GetCurrentUserId();
            var recipientId = await _chatService.GetOtherUserIdAsync(conversationId, senderId);
            await Clients.Group($"user_{recipientId}")
                .SendAsync("UserStoppedTyping", new { conversationId, userId = senderId });
        }

        public async Task MarkAsRead(Guid conversationId)
        {
            var userId = GetCurrentUserId();
            await _chatService.MarkMessagesAsReadAsync(conversationId, userId);
            var otherId = await _chatService.GetOtherUserIdAsync(conversationId, userId);
            await Clients.Group($"user_{otherId}")
                .SendAsync("MessagesRead", new { conversationId, readBy = userId });
        }

        // ─────────────────────────────────────────────────
        // SESIONES — ciclo de vida
        // ─────────────────────────────────────────────────

        /// <summary>
        /// El docente acepta la solicitud. Estado: Scheduled → Ready.
        /// GetOrCreateVideoRoomAsync crea la room en 100ms internamente.
        /// </summary>
        public async Task AcceptSession(Guid sessionId)
        {
            var userId = GetCurrentUserId();
            try
            {
                var session = await GetSessionOrThrow(sessionId);

                if (session.TeacherId != userId)
                    throw new HubException("Only the teacher can accept sessions");

                // El servicio crea la room en 100ms y cambia el estado a Ready
                await _sessionService.GetOrCreateVideoRoomAsync(sessionId, userId);

                var updated = await _sessionService.GetSessionByIdAsync(sessionId, userId);

                await Clients.Group($"user_{session.StudentId}")
                    .SendAsync("SessionAccepted", updated);
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting session {SessionId}", sessionId);
                throw new HubException("Error accepting session");
            }
        }

        /// <summary>
        /// Cualquier participante cancela. Estado: Scheduled|Ready → Cancelled.
        /// </summary>
        public async Task CancelSession(Guid sessionId)
        {
            var userId = GetCurrentUserId();
            try
            {
                var session = await GetSessionOrThrow(sessionId);

                if (session.StudentId != userId && session.TeacherId != userId)
                    throw new HubException("Not a participant of this session");

                var updated = await _sessionService.UpdateSessionAsync(sessionId, new UpdateTutoringSessionDto
                {
                    Status = SessionStatus.Cancelled
                });

                await Clients.Group($"user_{updated.StudentId}").SendAsync("SessionCancelled", updated);
                await Clients.Group($"user_{updated.TeacherId}").SendAsync("SessionCancelled", updated);
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling session {SessionId}", sessionId);
                throw new HubException("Error cancelling session");
            }
        }

        /// <summary>
        /// Un participante entró a la sala. Si era el primero: Ready → InProgress.
        /// </summary>
        public async Task NotifyJoinedRoom(Guid sessionId)
        {
            var userId = GetCurrentUserId();
            try
            {
                var session = await GetSessionOrThrow(sessionId);

                if (session.StudentId != userId && session.TeacherId != userId)
                    throw new HubException("Not a participant of this session");

                if (session.Status == SessionStatus.Ready)
                {
                    await _sessionService.UpdateSessionAsync(sessionId, new UpdateTutoringSessionDto
                    {
                        Status = SessionStatus.InProgress
                    });
                }

                var otherId = session.StudentId == userId ? session.TeacherId : session.StudentId;
                await Clients.Group($"user_{otherId}")
                    .SendAsync("ParticipantJoinedRoom", new { sessionId, userId });
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotifyJoinedRoom for session {SessionId}", sessionId);
                throw new HubException("Error notifying join");
            }
        }

        /// <summary>
        /// El docente finaliza. Estado: InProgress → Completed.
        /// La room de 100ms se deshabilita dentro del servicio.
        /// </summary>
        public async Task EndSession(Guid sessionId)
        {
            var userId = GetCurrentUserId();
            try
            {
                var session = await GetSessionOrThrow(sessionId);

                if (session.TeacherId != userId)
                    throw new HubException("Only the teacher can end the session");

                var updated = await _sessionService.UpdateSessionAsync(sessionId, new UpdateTutoringSessionDto
                {
                    Status = SessionStatus.Completed
                });

                // promptRating = true solo para el estudiante
                await Clients.Group($"user_{updated.StudentId}")
                    .SendAsync("SessionEnded", new { sessionId, promptRating = true });

                await Clients.Group($"user_{updated.TeacherId}")
                    .SendAsync("SessionEnded", new { sessionId, promptRating = false });
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending session {SessionId}", sessionId);
                throw new HubException("Error ending session");
            }
        }

        // ─────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────

        public static bool IsUserOnline(string userId) => _onlineUsers.ContainsKey(userId);

        private Guid GetCurrentUserId()
        {
            if (string.IsNullOrEmpty(Context.UserIdentifier))
                throw new HubException("User not authenticated");
            return Guid.Parse(Context.UserIdentifier);
        }

        private async Task<TutoringSessionDto> GetSessionOrThrow(Guid sessionId)
        {
            var session = await _sessionService.GetSessionByIdAsync(sessionId);
            return session ?? throw new HubException("Session not found");
        }

        private async Task NotifyContactsStatusAsync(string userId, bool isOnline)
        {
            var contactIds = await _chatService.GetContactIdsAsync(Guid.Parse(userId));
            foreach (var contactId in contactIds)
            {
                await Clients.Group($"user_{contactId}")
                    .SendAsync("UserStatusChanged", new { userId, isOnline });
            }
        }
    }
}