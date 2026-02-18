using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.DTOs.Chat
{
    public class ConversationDto
    {
        public Guid Id { get; set; }
        public UserSummaryDto OtherUser { get; set; } = null!;
        public MessageDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
