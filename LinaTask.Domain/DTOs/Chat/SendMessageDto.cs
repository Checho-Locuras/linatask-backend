using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LinaTask.Domain.DTOs.Chat
{
    public class SendMessageDto
    {
        [Required]
        public Guid ConversationId { get; set; }

        public string? Content { get; set; }

        public string MessageType { get; set; } = "text";

        // Para archivos se usa el endpoint de upload separado
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
    }
}
