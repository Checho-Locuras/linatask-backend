using LinaTask.Domain.DTOs;
using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Interfaces
{
    public interface INotificationRepository
    {
        Task<Notification?> GetByIdAsync(Guid id);
        Task<PagedNotificationsDto> GetByUserAsync(Guid userId, NotificationQueryDto query);
        Task<NotificationSummaryDto> GetSummaryAsync(Guid userId);
        Task<Notification> CreateAsync(Notification notification);
        Task<bool> MarkAsReadAsync(Guid id, Guid userId);
        Task<int> MarkAllAsReadAsync(Guid userId);
        Task<bool> DeleteAsync(Guid id, Guid userId);
        Task<int> DeleteAllReadAsync(Guid userId);
    }
}
