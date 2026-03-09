using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services.Interfaces
{
    public interface INotificationPusher
    {
        /// <summary>
        /// Envía una notificación en tiempo real al usuario especificado.
        /// La implementación concreta (SignalR, WebSocket, etc.) vive fuera de Application.
        /// </summary>
        Task PushAsync(Guid userId, object payload);
    }
}
