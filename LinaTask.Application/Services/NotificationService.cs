using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LinaTask.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly INotificationPusher _pusher; 
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository repo,
            INotificationPusher pusher,
            ILogger<NotificationService> logger)
        {
            _repo = repo;
            _pusher = pusher;
            _logger = logger;
        }

        // ── CRUD BASE ────────────────────────────────────────────────

        public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
        {
            var entity = new Notification
            {
                UserId = dto.UserId,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,
                Category = dto.Category,
                ReferenceId = dto.ReferenceId,
                ReferenceType = dto.ReferenceType,
                ActionUrl = dto.ActionUrl
            };

            var saved = await _repo.CreateAsync(entity);
            var result = MapToDto(saved);

            // Delegar el envío en tiempo real al pusher — sin saber cómo funciona
            await _pusher.PushAsync(dto.UserId, result);

            return result;
        }

        public Task<PagedNotificationsDto> GetByUserAsync(Guid userId, NotificationQueryDto query)
            => _repo.GetByUserAsync(userId, query);

        public Task<NotificationSummaryDto> GetSummaryAsync(Guid userId)
            => _repo.GetSummaryAsync(userId);

        public Task<bool> MarkAsReadAsync(Guid id, Guid userId)
            => _repo.MarkAsReadAsync(id, userId);

        public Task<int> MarkAllAsReadAsync(Guid userId)
            => _repo.MarkAllAsReadAsync(userId);

        public Task<bool> DeleteAsync(Guid id, Guid userId)
            => _repo.DeleteAsync(id, userId);

        public Task<int> DeleteAllReadAsync(Guid userId)
            => _repo.DeleteAllReadAsync(userId);

        // ── HELPERS DE DOMINIO ───────────────────────────────────────

        public Task NotifySessionBookedAsync(
            Guid teacherId, Guid studentId, string studentName,
            Guid sessionId, string subjectName, DateTime sessionDate)
        {
            var dateLabel = sessionDate.ToString("dd/MM/yyyy HH:mm");
            return CreateAsync(new CreateNotificationDto
            {
                UserId = teacherId,
                Title = "Nueva solicitud de sesión",
                Message = $"{studentName} ha solicitado una sesión de {subjectName} para el {dateLabel}.",
                Type = NotificationType.Info,
                Category = NotificationCategory.SessionBooked,
                ReferenceId = sessionId,
                ReferenceType = "Session",
                ActionUrl = "/teacher/requests"
            });
        }

        public Task NotifySessionConfirmedAsync(
            Guid studentId, string teacherName,
            Guid sessionId, string subjectName, DateTime sessionDate)
        {
            var dateLabel = sessionDate.ToString("dd/MM/yyyy HH:mm");
            return CreateAsync(new CreateNotificationDto
            {
                UserId = studentId,
                Title = "Sesión confirmada ✅",
                Message = $"{teacherName} confirmó tu sesión de {subjectName} para el {dateLabel}.",
                Type = NotificationType.Success,
                Category = NotificationCategory.SessionConfirmed,
                ReferenceId = sessionId,
                ReferenceType = "Session",
                ActionUrl = "/student/sessions"
            });
        }

        public Task NotifySessionCancelledAsync(
            Guid recipientId, string cancelledByName,
            Guid sessionId, string subjectName, DateTime sessionDate)
        {
            var dateLabel = sessionDate.ToString("dd/MM/yyyy HH:mm");
            return CreateAsync(new CreateNotificationDto
            {
                UserId = recipientId,
                Title = "Sesión cancelada",
                Message = $"{cancelledByName} canceló la sesión de {subjectName} del {dateLabel}.",
                Type = NotificationType.Warning,
                Category = NotificationCategory.SessionCancelled,
                ReferenceId = sessionId,
                ReferenceType = "Session",
                ActionUrl = "/student/sessions"
            });
        }

        public Task NotifySessionRejectedAsync(
            Guid studentId, string teacherName,
            Guid sessionId, string subjectName)
        {
            return CreateAsync(new CreateNotificationDto
            {
                UserId = studentId,
                Title = "Solicitud rechazada",
                Message = $"{teacherName} no pudo aceptar tu solicitud de {subjectName}. Puedes buscar otro docente.",
                Type = NotificationType.Error,
                Category = NotificationCategory.SessionRejected,
                ReferenceId = sessionId,
                ReferenceType = "Session",
                ActionUrl = "/student/schedule"
            });
        }

        public Task NotifyNewMessageAsync(Guid recipientId, string senderName, Guid conversationId)
        {
            return CreateAsync(new CreateNotificationDto
            {
                UserId = recipientId,
                Title = "Nuevo mensaje",
                Message = $"{senderName} te envió un mensaje.",
                Type = NotificationType.Info,
                Category = NotificationCategory.Message,
                ReferenceId = conversationId,
                ReferenceType = "Conversation",
                ActionUrl = "/shared/chat"
            });
        }

        public Task NotifyPaymentReceivedAsync(
            Guid teacherId, string studentName, Guid paymentId, decimal amount)
        {
            return CreateAsync(new CreateNotificationDto
            {
                UserId = teacherId,
                Title = "Pago recibido 💰",
                Message = $"{studentName} realizó un pago de ${amount:N0}.",
                Type = NotificationType.Success,
                Category = NotificationCategory.Payment,
                ReferenceId = paymentId,
                ReferenceType = "Payment",
                ActionUrl = "/teacher/earnings"
            });
        }

        // ── MAPPER ───────────────────────────────────────────────────
        private static NotificationDto MapToDto(Notification n) => new()
        {
            Id = n.Id,
            UserId = n.UserId,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            Category = n.Category,
            IsRead = n.IsRead,
            ReadAt = n.ReadAt,
            ReferenceId = n.ReferenceId,
            ReferenceType = n.ReferenceType,
            ActionUrl = n.ActionUrl,
            CreatedAt = n.CreatedAt
        };
    }
}