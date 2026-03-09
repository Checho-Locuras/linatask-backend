using LinaTask.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LinaTask.Infrastructure.Services
{
    /// <summary>
    /// Worker que corre cada minuto y envía notificaciones de recordatorio
    /// 15 minutos antes del inicio de cada sesión confirmada.
    /// </summary>
    public class SessionReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SessionReminderService> _logger;

        // Intervalo de verificación — cada 60 segundos
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(60);

        // Ventana de recordatorio: entre 14 y 16 minutos antes
        // (evita que si el worker corre exactamente en el minuto 15 se pierda)
        private static readonly TimeSpan ReminderWindowStart = TimeSpan.FromMinutes(14);
        private static readonly TimeSpan ReminderWindowEnd = TimeSpan.FromMinutes(16);

        public SessionReminderService(
            IServiceScopeFactory scopeFactory,
            ILogger<SessionReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SessionReminderService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SessionReminderService.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task CheckAndSendRemindersAsync()
        {
            // Usamos scope porque DbContext y otros servicios son Scoped
            using var scope = _scopeFactory.CreateScope();

            var sessionRepo = scope.ServiceProvider
                .GetRequiredService<ITutoringSessionRepository>();
            var notificationService = scope.ServiceProvider
                .GetRequiredService<INotificationService>();

            var now = DateTime.UtcNow;
            var from = now.Add(ReminderWindowStart);  // 14 min adelante
            var to = now.Add(ReminderWindowEnd);    // 16 min adelante

            var sessions = (await sessionRepo
                .GetSessionsNeedingReminderAsync(from, to))
                .ToList();

            if (sessions.Count == 0) return;

            _logger.LogInformation(
                "Sending reminders for {Count} session(s).", sessions.Count);

            foreach (var session in sessions)
            {
                var subjectName = session.Subject?.Name ?? "tu sesión";
                var studentName = session.Student?.Name ?? "Estudiante";
                var teacherName = session.Teacher?.Name ?? "Docente";
                var minutesUntil = (int)(session.StartTime - now).TotalMinutes;

                try
                {
                    // ── Notificar al estudiante ──
                    await notificationService.NotifySessionReminderAsync(
                        userId: session.StudentId,
                        otherName: teacherName,
                        sessionId: session.Id,
                        subjectName: subjectName,
                        sessionDate: session.StartTime,
                        minutesBefore: minutesUntil
                    );

                    // ── Notificar al docente ──
                    await notificationService.NotifySessionReminderAsync(
                        userId: session.TeacherId,
                        otherName: studentName,
                        sessionId: session.Id,
                        subjectName: subjectName,
                        sessionDate: session.StartTime,
                        minutesBefore: minutesUntil
                    );

                    // ── Marcar como enviado ──
                    session.ReminderSent = true;
                    await sessionRepo.UpdateAsync(session);

                    _logger.LogInformation(
                        "Reminder sent for session {SessionId} at {StartTime}.",
                        session.Id, session.StartTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send reminder for session {SessionId}.", session.Id);
                    // No marcamos ReminderSent para que reintente en el próximo ciclo
                }
            }
        }
    }
}