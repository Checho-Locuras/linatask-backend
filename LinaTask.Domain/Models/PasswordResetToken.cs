using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Models
{
    public class PasswordResetToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string DeliveryMethod { get; set; } = string.Empty; // "email" o "sms"
        public string DeliveryDestination { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        // Navegación
        public User User { get; set; } = null!;

        // Métodos de utilidad
        public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
        public bool IsValid() => !IsUsed && !IsExpired();
    }
}
