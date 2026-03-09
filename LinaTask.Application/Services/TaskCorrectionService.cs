using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Enums;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using TaskStatus = LinaTask.Domain.Enums.TaskStatus;

namespace LinaTask.Application.Services
{
    public class TaskCorrectionService : ITaskCorrectionService
    {
        private readonly ITaskCorrectionRepository _correctionRepo;
        private readonly IMarketplaceTaskRepository _taskRepo;
        private readonly INotificationService _notificationService;

        public TaskCorrectionService(
            ITaskCorrectionRepository correctionRepo,
            IMarketplaceTaskRepository taskRepo,
            INotificationService notificationService)
        {
            _correctionRepo = correctionRepo;
            _taskRepo = taskRepo;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<TaskCorrectionRequestDto>> GetByTaskIdAsync(Guid taskId) =>
            (await _correctionRepo.GetByTaskIdAsync(taskId)).Select(MapToDto);

        public async Task<TaskCorrectionRequestDto> CreateAsync(CreateCorrectionRequestDto dto, Guid studentId)
        {
            var task = await _taskRepo.GetByIdAsync(dto.TaskId)
                ?? throw new KeyNotFoundException("Task not found");

            if (task.StudentId != studentId)
                throw new UnauthorizedAccessException("Only the task owner can request corrections");

            if (task.Status != TaskStatus.Delivered)
                throw new InvalidOperationException("Task must be in Delivered status to request a correction");

            if (task.CorrectionsUsed >= task.MaxCorrections)
                throw new InvalidOperationException(
                    $"Maximum of {task.MaxCorrections} corrections reached. Please approve or dispute the task.");

            var correction = new TaskCorrectionRequest
            {
                Id = Guid.NewGuid(),
                TaskId = dto.TaskId,
                StudentId = studentId,
                Reason = dto.Reason,
                Status = CorrectionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _correctionRepo.CreateAsync(correction);

            // Actualizar contadores y estado de la tarea
            task.CorrectionsUsed++;
            task.Status = TaskStatus.InCorrection;
            await _taskRepo.UpdateAsync(task);

            // Notificar al docente
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = task.AssignedTeacherId!.Value,
                Title = $"Corrección solicitada ({task.CorrectionsUsed}/{task.MaxCorrections}) 📝",
                Message = $"El estudiante solicitó una corrección para \"{task.Title}\": {dto.Reason}",
                Type = NotificationType.Warning,
                Category = NotificationCategory.General,
                ReferenceId = dto.TaskId,
                ReferenceType = "MarketplaceTask",
                ActionUrl = $"/teacher/marketplace/tasks/{dto.TaskId}"
            });

            return MapToDto(created);
        }

        public async Task<TaskCorrectionRequestDto> ResolveAsync(Guid correctionId, Guid teacherId)
        {
            var correction = await _correctionRepo.GetByIdAsync(correctionId)
                ?? throw new KeyNotFoundException("Correction not found");

            var task = await _taskRepo.GetByIdAsync(correction.TaskId)
                ?? throw new KeyNotFoundException("Task not found");

            if (task.AssignedTeacherId != teacherId)
                throw new UnauthorizedAccessException("Only the assigned teacher can resolve corrections");

            if (correction.Status != CorrectionStatus.Pending && correction.Status != CorrectionStatus.InProgress)
                throw new InvalidOperationException("Correction is already resolved");

            correction.Status = CorrectionStatus.Resolved;
            var updated = await _correctionRepo.UpdateAsync(correction);

            // Volver a estado Delivered
            task.Status = TaskStatus.Delivered;
            await _taskRepo.UpdateAsync(task);

            // Notificar al estudiante
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = task.StudentId,
                Title = "Corrección entregada ✅",
                Message = $"El docente entregó la corrección para \"{task.Title}\". Revísala y aprueba o solicita otra corrección.",
                Type = NotificationType.Info,
                Category = NotificationCategory.General,
                ReferenceId = task.Id,
                ReferenceType = "MarketplaceTask",
                ActionUrl = $"/student/marketplace/tasks/{task.Id}"
            });

            return MapToDto(updated);
        }

        private static TaskCorrectionRequestDto MapToDto(TaskCorrectionRequest c) => new()
        {
            Id = c.Id,
            TaskId = c.TaskId,
            StudentId = c.StudentId,
            StudentName = c.Student?.Name ?? string.Empty,
            Reason = c.Reason,
            Status = c.Status.ToString(),
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
    }
}
