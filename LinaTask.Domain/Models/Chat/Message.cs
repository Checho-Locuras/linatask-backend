using LinaTask.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Models.Chat
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public string? Content { get; set; }
        public MessageType MessageType { get; set; } = MessageType.Text;
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public Conversation Conversation { get; set; } = null!;
        public User Sender { get; set; } = null!;
    }

}
