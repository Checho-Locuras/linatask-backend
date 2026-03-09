using LinaTask.Application.DTOs;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Enums;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LinaTask.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly INotificationPusher _pusher; 
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository repo,
            INotificationPusher pusher,
            ILogger<NotificationService> logger,
            IEmailService emailService,         // ← nuevo
            IUserRepository userRepository
            )
        {
            _repo = repo;
            _pusher = pusher;
            _logger = logger;
            _emailService = emailService;
            _userRepository = userRepository;
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
                ActionUrl = dto.ActionUrl,
                ActionsJson = dto.Actions is { Count: > 0 }
                    ? JsonSerializer.Serialize(dto.Actions)
                    : null
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
                ActionUrl = "/teacher/sessions",
                Actions = new List<NotificationActionDto>
                {
                    new() {
                        Label = "✅ Aceptar",
                        ActionType = "accept_session",
                        Payload = sessionId.ToString(),
                        Style = "primary"
                    },
                    new() {
                        Label = "✕ Rechazar",
                        ActionType = "reject_session",
                        Payload = sessionId.ToString(),
                        Style = "danger"
                    }
                }
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
                Actions = new List<NotificationActionDto>
                {
                    new() {
                        Label = "📅 Ver sesión",
                        ActionType = "navigate",
                        Url = "/student/sessions",
                        Style = "primary"
                    }
                }
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
            CreatedAt = n.CreatedAt,
            // ← nuevo
            Actions = string.IsNullOrEmpty(n.ActionsJson)
        ? null
        : JsonSerializer.Deserialize<List<NotificationActionDto>>(
            n.ActionsJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        };

        public async Task NotifySessionReminderAsync(Guid userId, string otherName, Guid sessionId, string subjectName, DateTime sessionDate, int minutesBefore)
        {
            var message = $"Tu sesión de {subjectName} con {otherName} " +
                          $"comienza en {minutesBefore} minutos. ¡Prepárate!";

            await CreateAsync(new CreateNotificationDto
            {
                UserId = userId,
                Title = "⏰ Tu sesión comienza pronto",
                Message = message,
                Type = NotificationType.Info,
                Category = NotificationCategory.SessionReminder,
                ReferenceId = sessionId,
                ReferenceType = "Session",
                ActionUrl = $"/classroom/{sessionId}"
            });

            // También por email
            await SendEmailIfAvailableAsync(userId, "session_reminder", new()
            {
                ["UserName"] = "Estudiante/Docente",  // se resuelve dentro con GetById
                ["OtherName"] = otherName,
                ["SubjectName"] = subjectName,
                ["MinutesBefore"] = minutesBefore.ToString(),
                ["SessionTime"] = sessionDate.ToLocalTime().ToString("HH:mm"),
                ["SessionDate"] = sessionDate.ToLocalTime().ToString("dd/MM/yyyy"),
                ["ActionUrl"] = $"https://tudominio.com/classroom/{sessionId}"
            });
        }

        private async Task SendEmailIfAvailableAsync(Guid userId, string templateKey, Dictionary<string, string> variables)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user?.Email is null) return;
                await _emailService.SendFromTemplateAsync(user.Email, templateKey, variables);
            }
            catch (Exception ex)
            {
                // El email falla silenciosamente — la notificación push ya se envió
                _logger.LogWarning(ex, "Failed to send email for user {UserId}.", userId);
            }
        }
    }
}