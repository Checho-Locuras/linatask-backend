using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public Guid? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public string? ActionUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Resumen liviano para el badge de la campana</summary>
    public class NotificationSummaryDto
    {
        public int UnreadCount { get; set; }
        public List<NotificationDto> Recent { get; set; } = new();
    }

    /// <summary>Crea una notificación desde el backend / otros servicios</summary>
    public class CreateNotificationDto
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info";
        public string Category { get; set; } = "general";
        public Guid? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public string? ActionUrl { get; set; }
    }

    /// <summary>Parámetros de paginación / filtrado para GET /notifications</summary>
    public class NotificationQueryDto
    {
        public bool? IsRead { get; set; }        // null = todas, true = leídas, false = no leídas
        public string? Category { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PagedNotificationsDto
    {
        public List<NotificationDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
