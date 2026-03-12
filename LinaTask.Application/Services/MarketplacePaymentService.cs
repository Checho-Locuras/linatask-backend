// Application/Services/MarketplacePaymentService.cs
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Enums;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using Microsoft.Extensions.Logging;
using TaskStatus = LinaTask.Domain.Enums.TaskStatus;

namespace LinaTask.Application.Services
{
    public class MarketplacePaymentService : IMarketplacePaymentService
    {
        private static readonly TimeSpan AutoReleaseDelay = TimeSpan.FromDays(3);

        private readonly IMarketplacePaymentRepository _paymentRepo;
        private readonly IMarketplaceTaskRepository _taskRepo;
        private readonly INotificationService _notificationService;
        private readonly ILogger<MarketplacePaymentService> _logger;
        private readonly ISystemParameterRepository _parameterRepo;

        // Inyectar también el repo de sesiones cuando esté disponible.
        // Si aún no existe, se puede usar null-conditional o inyección opcional.
        // Por ahora declaramos la interfaz — si no existe, coméntala y
        // el bloque "session" del InitiatePayment lanzará NotImplementedException.
        // private readonly ITutoringSessionRepository _sessionRepo;

        public MarketplacePaymentService(
            IMarketplacePaymentRepository paymentRepo,
            IMarketplaceTaskRepository taskRepo,
            INotificationService notificationService,
            ILogger<MarketplacePaymentService> logger,
            ISystemParameterRepository parameterRepo)
        {
            _paymentRepo = paymentRepo;
            _taskRepo = taskRepo;
            _notificationService = notificationService;
            _logger = logger;
            _parameterRepo = parameterRepo;
        }

        // ── GetByTaskId ──────────────────────────────────────────────

        public async Task<MarketplacePaymentDto?> GetByTaskIdAsync(Guid taskId)
        {
            var payment = await _paymentRepo.GetByTaskIdAsync(taskId);
            return payment is null ? null : MapToDto(payment);
        }

        // ── InitiatePayment ──────────────────────────────────────────

        public async Task<MarketplacePaymentDto> InitiatePaymentAsync(
            InitiatePaymentRequestDto dto,
            Guid studentId)
        {
            return dto.Context switch
            {
                PaymentContextType.Task => await InitiateTaskPaymentAsync(dto, studentId),
                PaymentContextType.Session => await InitiateSessionPaymentAsync(dto, studentId),
                _ => throw new InvalidOperationException($"Invalid payment context: {dto.Context}")
            };
        }

        private async Task<MarketplacePaymentDto> InitiateTaskPaymentAsync(
            InitiatePaymentRequestDto dto, Guid studentId)
        {
            if (dto.TaskId is null)
                throw new ArgumentException("TaskId is required for task payments");

            var task = await _taskRepo.GetByIdAsync(dto.TaskId.Value)
                ?? throw new KeyNotFoundException("Task not found");

            if (task.StudentId != studentId)
                throw new UnauthorizedAccessException("Only the task owner can initiate payment");

            if (task.Status != TaskStatus.Assigned)
                throw new InvalidOperationException("Task must be in Assigned status to initiate payment");

            var existing = await _paymentRepo.GetByTaskIdAsync(dto.TaskId.Value);
            if (existing is not null)
                throw new InvalidOperationException("A payment already exists for this task");

            var amount = task.FinalPrice
                ?? throw new InvalidOperationException("Task has no final price set");

            var feePercent = await GetCommissionAsync("task");
            var platformFee = Math.Round(amount * feePercent, 2);

            var payment = new MarketplacePayment
            {
                Id = Guid.NewGuid(),
                TaskId = dto.TaskId,
                PaymentContext = "task",
                StudentId = studentId,
                TeacherId = task.AssignedTeacherId!.Value,
                Amount = amount,
                PlatformFee = platformFee,
                TeacherAmount = amount - platformFee,
                Status = PaymentStatus.Pending,
                PaymentMethod = dto.PaymentMethod,
                AutoReleaseAt = DateTime.UtcNow.Add(AutoReleaseDelay),
                CreatedAt = DateTime.UtcNow
            };

            return MapToDto(await _paymentRepo.CreateAsync(payment));
        }

        private Task<MarketplacePaymentDto> InitiateSessionPaymentAsync(
            InitiatePaymentRequestDto dto, Guid studentId)
        {
            // TODO: implementar cuando el repo de sesiones esté disponible.
            // Patrón idéntico a InitiateTaskPaymentAsync pero usando _sessionRepo.
            throw new NotImplementedException(
                "Session payments will be implemented when ITutoringSessionRepository is injected.");
        }

        // ── ConfirmPaymentHeld ───────────────────────────────────────

        public async Task<MarketplacePaymentDto> ConfirmPaymentHeldAsync(
            Guid taskId, string externalPaymentId)
        {
            var payment = await _paymentRepo.GetByTaskIdAsync(taskId)
                ?? throw new KeyNotFoundException("Payment not found");

            if (payment.Status != PaymentStatus.Pending)
                throw new InvalidOperationException("Payment is not in Pending status");

            payment.Status = PaymentStatus.Held;
            payment.HeldAt = DateTime.UtcNow;
            payment.ExternalPaymentId = externalPaymentId;

            var task = await _taskRepo.GetByIdAsync(taskId)
                ?? throw new KeyNotFoundException("Task not found");

            task.Status = TaskStatus.Paid;
            await _taskRepo.UpdateAsync(task);

            var updated = await _paymentRepo.UpdateAsync(payment);

            await _notificationService.NotifyPaymentReceivedAsync(
                teacherId: payment.TeacherId,
                studentName: payment.Student?.Name ?? "El estudiante",
                paymentId: payment.Id,
                amount: payment.TeacherAmount
            );

            return MapToDto(updated);
        }

        // ── ConfirmSessionPayment ────────────────────────────────────

        public Task<MarketplacePaymentDto> ConfirmSessionPaymentAsync(
            Guid sessionId, string externalPaymentId)
        {
            // TODO: implementar junto con el módulo de sesiones.
            throw new NotImplementedException(
                "Session payment confirmation will be implemented with the tutoring module.");
        }

        // ── HandleWebhookApproval ────────────────────────────────────

        public async Task HandleWebhookApprovalAsync(string externalPaymentId)
        {
            // Buscar pago en BD por ExternalPaymentId
            var payment = await _paymentRepo.GetByExternalPaymentIdAsync(externalPaymentId);

            if (payment is null)
            {
                _logger.LogWarning(
                    "Webhook approval received for unknown ExternalPaymentId {Id}", externalPaymentId);
                return;
            }

            // Idempotencia: si ya está Held, ignorar
            if (payment.Status != PaymentStatus.Pending)
            {
                _logger.LogInformation(
                    "Webhook: payment {Id} already in status {Status}, skipping",
                    payment.Id, payment.Status);
                return;
            }

            // Reusar la lógica de confirmación según contexto
            if (payment.PaymentContext == "task" && payment.TaskId.HasValue)
            {
                await ConfirmPaymentHeldAsync(payment.TaskId.Value, externalPaymentId);
            }
            else if (payment.PaymentContext == "session" && payment.SessionId.HasValue)
            {
                await ConfirmSessionPaymentAsync(payment.SessionId.Value, externalPaymentId);
            }
            else
            {
                _logger.LogError(
                    "Webhook: payment {Id} has unknown context or missing reference", payment.Id);
            }
        }

        // ── ReleasePayment ───────────────────────────────────────────

        public async Task<MarketplacePaymentDto> ReleasePaymentAsync(Guid taskId, Guid studentId)
        {
            var task = await _taskRepo.GetByIdAsync(taskId)
                ?? throw new KeyNotFoundException("Task not found");

            if (task.StudentId != studentId)
                throw new UnauthorizedAccessException("Only the task owner can release the payment");

            if (task.Status != TaskStatus.Delivered && task.Status != TaskStatus.InCorrection)
                throw new InvalidOperationException("Task must be Delivered to release payment");

            var payment = await _paymentRepo.GetByTaskIdAsync(taskId)
                ?? throw new KeyNotFoundException("Payment not found for this task");

            if (payment.Status != PaymentStatus.Held)
                throw new InvalidOperationException("Payment is not held");

            payment.Status = PaymentStatus.Released;
            payment.ReleasedAt = DateTime.UtcNow;
            task.Status = TaskStatus.Completed;

            await _taskRepo.UpdateAsync(task);
            var updated = await _paymentRepo.UpdateAsync(payment);

            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = payment.TeacherId,
                Title = "Pago liberado 💰",
                Message = $"El estudiante aprobó la tarea \"{task.Title}\". " +
                               $"Se han liberado ${payment.TeacherAmount:N0} a tu cuenta.",
                Type = NotificationType.Success,
                Category = NotificationCategory.System.Payment,
                ReferenceId = taskId,
                ReferenceType = "MarketplaceTask",
                ActionUrl = "/teacher/earnings"
            });

            return MapToDto(updated);
        }

        // ── RefundPayment ────────────────────────────────────────────

        public async Task<MarketplacePaymentDto> RefundPaymentAsync(Guid taskId)
        {
            var payment = await _paymentRepo.GetByTaskIdAsync(taskId)
                ?? throw new KeyNotFoundException("Payment not found");

            if (payment.Status != PaymentStatus.Held)
                throw new InvalidOperationException("Only held payments can be refunded");

            payment.Status = PaymentStatus.Refunded;
            payment.RefundedAt = DateTime.UtcNow;

            var task = await _taskRepo.GetByIdAsync(taskId);
            if (task is not null)
            {
                task.Status = TaskStatus.Cancelled;
                await _taskRepo.UpdateAsync(task);
            }

            return MapToDto(await _paymentRepo.UpdateAsync(payment));
        }

        // ── ProcessAutoReleases ──────────────────────────────────────

        public async Task ProcessAutoReleasesAsync()
        {
            var pending = await _paymentRepo.GetPendingAutoReleaseAsync(DateTime.UtcNow);

            foreach (var payment in pending)
            {
                try
                {
                    payment.Status = PaymentStatus.Released;
                    payment.ReleasedAt = DateTime.UtcNow;
                    await _paymentRepo.UpdateAsync(payment);

                    if (payment.PaymentContext == "task" && payment.TaskId.HasValue)
                    {
                        var task = await _taskRepo.GetByIdAsync(payment.TaskId.Value);
                        if (task is not null)
                        {
                            task.Status = TaskStatus.Completed;
                            await _taskRepo.UpdateAsync(task);
                        }
                    }

                    await _notificationService.CreateAsync(new CreateNotificationDto
                    {
                        UserId = payment.TeacherId,
                        Title = "Pago liberado automáticamente 💰",
                        Message = $"El pago de ${payment.TeacherAmount:N0} fue liberado " +
                                        "automáticamente tras 3 días sin respuesta del estudiante.",
                        Type = NotificationType.Success,
                        Category = NotificationCategory.System.Payment,
                        ReferenceId = payment.TaskId ?? payment.SessionId ?? Guid.Empty,
                        ReferenceType = payment.PaymentContext == "session" ? "TutoringSession" : "MarketplaceTask"
                    });

                    _logger.LogInformation(
                        "Auto-released payment {PaymentId} (context: {Context})",
                        payment.Id, payment.PaymentContext);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to auto-release payment {PaymentId}", payment.Id);
                }
            }
        }

        // ── Helpers ──────────────────────────────────────────────────

        private async Task<decimal> GetCommissionAsync(string context)
        {
            var key = context == "session"
                ? "marketplace.commission.sessions"
                : "marketplace.commission.tasks";

            var param = await _parameterRepo.GetByKeyAsync(key);
            if (param is null || !decimal.TryParse(param.ParamValue, out var pct))
                return 0.10m; // fallback seguro

            return pct / 100m; // "10" → 0.10
        }

        private static MarketplacePaymentDto MapToDto(MarketplacePayment p) => new()
        {
            Id = p.Id,
            TaskId = p.TaskId,
            SessionId = p.SessionId,
            PaymentContext = p.PaymentContext,
            TaskTitle = p.Task?.Title ?? string.Empty,
            StudentId = p.StudentId,
            StudentName = p.Student?.Name ?? string.Empty,
            TeacherId = p.TeacherId,
            TeacherName = p.Teacher?.Name ?? string.Empty,
            Amount = p.Amount,
            PlatformFee = p.PlatformFee,
            TeacherAmount = p.TeacherAmount,
            Status = p.Status.ToString(),
            PaymentMethod = p.PaymentMethod,
            HeldAt = p.HeldAt,
            ReleasedAt = p.ReleasedAt,
            AutoReleaseAt = p.AutoReleaseAt,
            CreatedAt = p.CreatedAt
        };
    }
}