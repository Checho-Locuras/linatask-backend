using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs.Chat;
using LinaTask.Domain.Enums;
using LinaTask.Domain.Models.Chat;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Application.Services
{
    public class ChatService : IChatService
    {
        private readonly LinaTaskDbContext _context;

        public ChatService(LinaTaskDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────
        // LISTAR CONVERSACIONES
        // ─────────────────────────────────────────────
        public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(Guid userId)
        {
            var conversations = await _context.Conversations
                .Where(c => c.UserOneId == userId || c.UserTwoId == userId)
                .Include(c => c.UserOne)
                .Include(c => c.UserTwo)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .AsNoTracking()
                .ToListAsync();

            var conversationIds = conversations.Select(c => c.Id).ToList();

            var unreadCounts = await _context.Messages
                .Where(m => conversationIds.Contains(m.ConversationId)
                            && m.SenderId != userId
                            && !m.IsRead)
                .GroupBy(m => m.ConversationId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var result = conversations.Select(c =>
            {
                var otherUser = c.UserOneId == userId ? c.UserTwo : c.UserOne;
                var lastMessage = c.Messages.FirstOrDefault();

                return new ConversationDto
                {
                    Id = c.Id,
                    OtherUser = new UserSummaryDto
                    {
                        Id = otherUser.Id,
                        Name = otherUser.Name,
                        Role = otherUser.UserRoles.FirstOrDefault()?.Role.Name ?? "",
                        IsOnline = false // Se maneja en SignalR (Api)
                    },
                    LastMessage = lastMessage == null ? null : MapMessage(lastMessage),
                    UnreadCount = unreadCounts.ContainsKey(c.Id) ? unreadCounts[c.Id] : 0,
                    UpdatedAt = lastMessage?.CreatedAt ?? c.CreatedAt
                };
            })
            .OrderByDescending(c => c.UpdatedAt);

            return result;
        }

        // ─────────────────────────────────────────────
        // OBTENER O CREAR CONVERSACIÓN
        // ─────────────────────────────────────────────
        public async Task<ConversationDto> GetOrCreateConversationAsync(Guid userOneId, Guid userTwoId)
        {
            var existing = await _context.Conversations
                .Include(c => c.UserOne)
                .Include(c => c.UserTwo)
                .FirstOrDefaultAsync(c =>
                    (c.UserOneId == userOneId && c.UserTwoId == userTwoId) ||
                    (c.UserOneId == userTwoId && c.UserTwoId == userOneId));

            if (existing != null)
                return MapConversation(existing, userOneId);

            var conversation = new Conversation
            {
                UserOneId = userOneId,
                UserTwoId = userTwoId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            await _context.Entry(conversation).Reference(c => c.UserOne).LoadAsync();
            await _context.Entry(conversation).Reference(c => c.UserTwo).LoadAsync();

            return MapConversation(conversation, userOneId);
        }

        // ─────────────────────────────────────────────
        // HISTORIAL DE MENSAJES
        // ─────────────────────────────────────────────
        public async Task<IEnumerable<MessageDto>> GetMessagesAsync(
            Guid conversationId, Guid userId, int page = 1, int pageSize = 50)
        {
            var belongs = await _context.Conversations
                .AnyAsync(c => c.Id == conversationId &&
                              (c.UserOneId == userId || c.UserTwoId == userId));

            if (!belongs)
                throw new UnauthorizedAccessException("No tienes acceso a esta conversación");

            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return messages
                .Select(MapMessage)
                .Reverse();
        }

        // ─────────────────────────────────────────────
        // GUARDAR MENSAJE
        // ─────────────────────────────────────────────
        public async Task<MessageDto> SaveMessageAsync(Guid senderId, SendMessageDto dto)
        {
            var belongs = await _context.Conversations
                .AnyAsync(c => c.Id == dto.ConversationId &&
                              (c.UserOneId == senderId || c.UserTwoId == senderId));

            if (!belongs)
                throw new UnauthorizedAccessException("No tienes acceso a esta conversación");

            var message = new Message
            {
                ConversationId = dto.ConversationId,
                SenderId = senderId,
                Content = dto.Content,
                MessageType = Enum.Parse<MessageType>(dto.MessageType, true),
                FileUrl = dto.FileUrl,
                FileName = dto.FileName,
                FileSize = dto.FileSize,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);

            await _context.Conversations
                .Where(c => c.Id == dto.ConversationId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.UpdatedAt, DateTime.UtcNow));

            await _context.SaveChangesAsync();

            await _context.Entry(message).Reference(m => m.Sender).LoadAsync();

            return MapMessage(message);
        }

        // ─────────────────────────────────────────────
        // MARCAR COMO LEÍDOS
        // ─────────────────────────────────────────────
        public async Task MarkMessagesAsReadAsync(Guid conversationId, Guid userId)
        {
            await _context.Messages
                .Where(m => m.ConversationId == conversationId
                         && m.SenderId != userId
                         && !m.IsRead)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.IsRead, true)
                    .SetProperty(m => m.ReadAt, DateTime.UtcNow));
        }

        public async Task<Guid> GetOtherUserIdAsync(Guid conversationId, Guid userId)
        {
            var conversation = await _context.Conversations
                .Where(c => c.Id == conversationId)
                .Select(c => new { c.UserOneId, c.UserTwoId })
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Conversación no encontrada");

            return conversation.UserOneId == userId
                ? conversation.UserTwoId
                : conversation.UserOneId;
        }

        public async Task<IEnumerable<Guid>> GetContactIdsAsync(Guid userId)
        {
            return await _context.Conversations
                .Where(c => c.UserOneId == userId || c.UserTwoId == userId)
                .Select(c => c.UserOneId == userId ? c.UserTwoId : c.UserOneId)
                .Distinct()
                .ToListAsync();
        }

        // ─────────────────────────────────────────────
        // MAPPERS
        // ─────────────────────────────────────────────

        private static MessageDto MapMessage(Message m) => new()
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            SenderName = m.Sender?.Name ?? "",
            Content = m.Content,
            MessageType = m.MessageType.ToString().ToLower(),
            FileUrl = m.FileUrl,
            FileName = m.FileName,
            FileSize = m.FileSize,
            IsRead = m.IsRead,
            ReadAt = m.ReadAt,
            CreatedAt = m.CreatedAt
        };

        private static ConversationDto MapConversation(Conversation c, Guid currentUserId)
        {
            var otherUser = c.UserOneId == currentUserId ? c.UserTwo : c.UserOne;

            return new ConversationDto
            {
                Id = c.Id,
                OtherUser = new UserSummaryDto
                {
                    Id = otherUser.Id,
                    Name = otherUser.Name,
                    Role = otherUser.UserRoles.FirstOrDefault()?.Role.Name ?? "",
                    IsOnline = false
                },
                UnreadCount = 0,
                UpdatedAt = c.UpdatedAt
            };
        }
    }
}
