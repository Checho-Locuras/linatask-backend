using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.DTOs.Chat
{
    public class UserSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
    }
}
