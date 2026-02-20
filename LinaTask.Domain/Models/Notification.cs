using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Models
{
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = NotificationType.Info;
        public string Category { get; set; } = NotificationCategory.General;
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public Guid? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public string? ActionUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class NotificationType
    {
        public const string Info = "info";
        public const string Success = "success";
        public const string Warning = "warning";
        public const string Error = "error";
    }

    public static class NotificationCategory
    {
        public const string SessionBooked = "session_booked";
        public const string SessionConfirmed = "session_confirmed";
        public const string SessionCancelled = "session_cancelled";
        public const string SessionRejected = "session_rejected";
        public const string SessionReminder = "session_reminder";
        public const string Payment = "payment";
        public const string Message = "message";
        public const string General = "general";
    }
}
