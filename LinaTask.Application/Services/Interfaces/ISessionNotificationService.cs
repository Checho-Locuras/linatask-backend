using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services.Interfaces
{
    public interface ISessionNotificationService
    {
        Task NotifyNewSessionRequestAsync(Guid teacherId, object payload);
        Task NotifySessionUpdatedAsync(Guid studentId, Guid teacherId, object payload);
        Task NotifySessionRoomReadyAsync(Guid userId, object payload);
        Task NotifySessionRatedAsync(Guid teacherId, object payload);
    }
}
