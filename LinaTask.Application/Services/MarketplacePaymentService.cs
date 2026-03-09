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
        private const decimal PlatformFeePercent = 0.10m; // 10% comisión
        private static readonly TimeSpan AutoReleaseDelay = TimeSpan.FromDays(3);

        private readonly IMarketplacePaymentRepository _paymentRepo;
        private readonly IMarketplaceTaskRepository _taskRepo;
        private readonly INotificationService _notificationService;
        private readonly ILogger<MarketplacePaymentService> _logger;

        public MarketplacePaymentService(
            IMarketplacePaymentRepository paymentRepo,
            IMarketplaceTaskRepository taskRepo,
            INotificationService notificationService,
            ILogger<MarketplacePaymentService> logger)
        {
            _paymentRepo = paymentRepo;
            _taskRepo = taskRepo;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<MarketplacePaymentDto?> GetByTaskIdAsync(Guid taskId)
        {
            var payment = await _paymentRepo.GetByTaskIdAsync(taskId);
            return payment is null ? null : MapToDto(payment);
        }

        public async Task<MarketplacePaymentDto> InitiatePaymentAsync(InitiatePaymentDto dto, Guid studentId)
        {
            var task = await _taskRepo.GetByIdAsync(dto.TaskId)
                ?? throw new KeyNotFoundException("Task not found");

            if (task.StudentId != studentId)
                throw new UnauthorizedAccessException("Only the task owner can initiate payment");

            if (task.Status != TaskStatus.Assigned)
                throw new InvalidOperationException("Task must be in Assigned status to initiate payment");

            var existing = await _paymentRepo.GetByTaskIdAsync(dto.TaskId);
            if (existing is not null)
                throw new InvalidOperationException("A payment already exists for this task");

            var amount = task.FinalPrice
                ?? throw new InvalidOperationException("Task has no final price set");

            var platformFee = Math.Round(amount * PlatformFeePercent, 2);
            var teacherAmount = amount - platformFee;

            var payment = new MarketplacePayment
            {
                Id = Guid.NewGuid(),
                TaskId = dto.TaskId,
                StudentId = studentId,
                TeacherId = task.AssignedTeacherId!.Value,
                Amount = amount,
                PlatformFee = platformFee,
                TeacherAmount = teacherAmount,
                Status = PaymentStatus.Pending,
                PaymentMethod = dto.PaymentMethod,
                CreatedAt = DateTime.UtcNow
            };

            return MapToDto(await _paymentRepo.CreateAsync(payment));
        }

        public async Task<MarketplacePaymentDto> ConfirmPaymentHeldAsync(Guid taskId)
        {
            var payment = await _paymentRepo.GetByTaskIdAsync(taskId)
                ?? throw new KeyNotFoundException("Payment not found");

            if (payment.Status != PaymentStatus.Pending)
                throw new InvalidOperationException("Payment is not in Pending status");

            payment.Status = PaymentStatus.Held;
            payment.HeldAt = DateTime.UtcNow;

            var task = await _taskRepo.GetByIdAsync(taskId)
                ?? throw new KeyNotFoundException("Task not found");

            task.Status = TaskStatus.Paid;
            await _taskRepo.UpdateAsync(task);

            var updated = await _paymentRepo.UpdateAsync(payment);

            // Notificar al docente
            await _notificationService.NotifyPaymentReceivedAsync(
                teacherId: payment.TeacherId,
                studentName: payment.Student?.Name ?? "El estudiante",
                paymentId: payment.Id,
                amount: payment.TeacherAmount
            );

            return MapToDto(updated);
        }

        public async Task<MarketplacePaymentDto> ReleasePaymentAsync(Guid taskId, Guid studentId)
        {
            var task = await _taskRepo.GetByIdAsync(taskId)
                ?? throw new KeyNotFoundException("Task not found");

            if (task.StudentId != studentId)
                throw new UnauthorizedAccessException("Only the task owner can release the payment");

            if (task.Status != TaskStatus.Delivered && task.Status != TaskStatus.InCorrection)
                throw new InvalidOperationException("Task must be in Delivered status to release payment");

            var payment = await _paymentRepo.GetByTaskIdAsync(taskId)
                ?? throw new KeyNotFoundException("Payment not found for this task");

            if (payment.Status != PaymentStatus.Held)
                throw new InvalidOperationException("Payment is not held");

            payment.Status = PaymentStatus.Released;
            payment.ReleasedAt = DateTime.UtcNow;
            task.Status = TaskStatus.Completed;

            await _taskRepo.UpdateAsync(task);
            var updated = await _paymentRepo.UpdateAsync(payment);

            // Notificar al docente que el pago fue liberado
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = payment.TeacherId,
                Title = "Pago liberado 💰",
                Message = $"El estudiante aprobó la tarea \"{task.Title}\". " +
                          $"Se han liberado ${payment.TeacherAmount:N0} a tu cuenta.",
                Type = NotificationType.Success,
                Category = NotificationCategory.Payment,
                ReferenceId = taskId,
                ReferenceType = "MarketplaceTask",
                ActionUrl = "/teacher/earnings"
            });

            return MapToDto(updated);
        }

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

                    var task = await _taskRepo.GetByIdAsync(payment.TaskId);
                    if (task is not null)
                    {
                        task.Status = TaskStatus.Completed;
                        await _taskRepo.UpdateAsync(task);
                    }

                    await _notificationService.CreateAsync(new CreateNotificationDto
                    {
                        UserId = payment.TeacherId,
                        Title = "Pago liberado automáticamente 💰",
                        Message = $"El pago de ${payment.TeacherAmount:N0} fue liberado automáticamente " +
                                  $"tras 3 días sin respuesta del estudiante.",
                        Type = NotificationType.Success,
                        Category = NotificationCategory.Payment,
                        ReferenceId = payment.TaskId,
                        ReferenceType = "MarketplaceTask"
                    });

                    _logger.LogInformation("Auto-released payment {PaymentId} for task {TaskId}",
                        payment.Id, payment.TaskId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to auto-release payment {PaymentId}", payment.Id);
                }
            }
        }

        private static MarketplacePaymentDto MapToDto(MarketplacePayment p) => new()
        {
            Id = p.Id,
            TaskId = p.TaskId,
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
