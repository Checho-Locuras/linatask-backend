using LinaTask.Domain.DTOs.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services.Interfaces
{
    public interface IChatService
    {
        Task<IEnumerable<ConversationDto>> GetConversationsAsync(Guid userId);
        Task<ConversationDto> GetOrCreateConversationAsync(Guid userOneId, Guid userTwoId);
        Task<IEnumerable<MessageDto>> GetMessagesAsync(Guid conversationId, Guid userId, int page, int pageSize);
        Task<MessageDto> SaveMessageAsync(Guid senderId, SendMessageDto dto);
        Task MarkMessagesAsReadAsync(Guid conversationId, Guid userId);
        Task<Guid> GetOtherUserIdAsync(Guid conversationId, Guid userId);
        Task<IEnumerable<Guid>> GetContactIdsAsync(Guid userId);
    }
}
