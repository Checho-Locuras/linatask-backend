using LinaTask.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDto> CreateAsync(CreateNotificationDto dto);
        Task<PagedNotificationsDto> GetByUserAsync(Guid userId, NotificationQueryDto query);
        Task<NotificationSummaryDto> GetSummaryAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid id, Guid userId);
        Task<int> MarkAllAsReadAsync(Guid userId);
        Task<bool> DeleteAsync(Guid id, Guid userId);
        Task<int> DeleteAllReadAsync(Guid userId);

        // ── Helpers de dominio: llamar desde otros servicios ─────────
        Task NotifySessionBookedAsync(Guid teacherId, Guid studentId, string studentName,
                                      Guid sessionId, string subjectName, DateTime sessionDate);
        Task NotifySessionConfirmedAsync(Guid studentId, string teacherName,
                                         Guid sessionId, string subjectName, DateTime sessionDate);
        Task NotifySessionCancelledAsync(Guid recipientId, string cancelledByName,
                                          Guid sessionId, string subjectName, DateTime sessionDate);
        Task NotifySessionRejectedAsync(Guid studentId, string teacherName,
                                         Guid sessionId, string subjectName);
        Task NotifyNewMessageAsync(Guid recipientId, string senderName, Guid conversationId);
        Task NotifyPaymentReceivedAsync(Guid teacherId, string studentName,
                                         Guid paymentId, decimal amount);
    }
}
