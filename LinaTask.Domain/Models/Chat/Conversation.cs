using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Models.Chat
{
    public class Conversation
    {
        public Guid Id { get; set; }
        public Guid UserOneId { get; set; }
        public Guid UserTwoId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public User UserOne { get; set; } = null!;
        public User UserTwo { get; set; } = null!;
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
