using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace LinaTask.Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        // Mapa en memoria: userId → connectionId(s)
        // Un usuario puede tener múltiples conexiones (varias pestañas)
        private static readonly ConcurrentDictionary<string, HashSet<string>> _onlineUsers = new();

        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
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
                (_, connections) => { connections.Add(Context.ConnectionId); return connections; }
            );

            // Unir al grupo personal para recibir mensajes directos
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Notificar a contactos que el usuario está online
            await NotifyContactsStatusAsync(userId.ToString(), isOnline: true);

            _logger.LogInformation("User {UserId} connected ({ConnectionId})", userId, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();

            if (_onlineUsers.TryGetValue(userId.ToString(), out var connections))
            {
                connections.Remove(Context.ConnectionId);
                if (connections.Count == 0)
                {
                    _onlineUsers.TryRemove(userId.ToString(), out _);
                    // Solo notificar offline cuando cierra TODAS las pestañas
                    await NotifyContactsStatusAsync(userId.ToString(), isOnline: false);
                }
            }

            _logger.LogInformation("User {UserId} disconnected ({ConnectionId})", userId.ToString(), Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        // ─────────────────────────────────────────────────
        // ENVIAR MENSAJE
        // ─────────────────────────────────────────────────

        public async Task SendMessage(SendMessageDto dto)
        {
            try
            {
                var senderId = GetCurrentUserId();

                // Guardar en BD
                var message = await _chatService.SaveMessageAsync(senderId, dto);

                // Enviar al destinatario (si está conectado, le llega instantáneo)
                var recipientId = await _chatService.GetOtherUserIdAsync(dto.ConversationId, senderId);
                await Clients.Group($"user_{recipientId}").SendAsync("ReceiveMessage", message);

                // Confirmar al emisor (para sincronizar otras pestañas abiertas)
                await Clients.Group($"user_{senderId}").SendAsync("ReceiveMessage", message);
            }catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
                throw;
            }
            
        }

        // ─────────────────────────────────────────────────
        // TYPING INDICATOR
        // ─────────────────────────────────────────────────

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

        // ─────────────────────────────────────────────────
        // MARCAR MENSAJES COMO LEÍDOS
        // ─────────────────────────────────────────────────

        public async Task MarkAsRead(Guid conversationId)
        {
            var userId = GetCurrentUserId();
            await _chatService.MarkMessagesAsReadAsync(conversationId, userId);

            // Notificar al otro usuario que sus mensajes fueron leídos
            var otherId = await _chatService.GetOtherUserIdAsync(conversationId, userId);
            await Clients.Group($"user_{otherId}")
                .SendAsync("MessagesRead", new { conversationId, readBy = userId });
        }

        // ─────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────

        public static bool IsUserOnline(string userId) => _onlineUsers.ContainsKey(userId);

        private Guid GetCurrentUserId()
        {
            if (string.IsNullOrEmpty(Context.UserIdentifier))
                throw new HubException("Usuario no autenticado");

            return Guid.Parse(Context.UserIdentifier);
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
