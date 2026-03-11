using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Enums;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.Repositories;
using TaskStatus = LinaTask.Domain.Enums.TaskStatus;

namespace LinaTask.Application.Services
{
    public class TaskOfferService : ITaskOfferService
    {
        private const int MaxOffersPerTask = 10;

        private readonly ITaskOfferRepository _offerRepo;
        private readonly IMarketplaceTaskRepository _taskRepo;
        private readonly IUserRepository _userRepo;
        private readonly INotificationService _notificationService;

        public TaskOfferService(
            ITaskOfferRepository offerRepo,
            IMarketplaceTaskRepository taskRepo,
            IUserRepository userRepo,
            INotificationService notificationService)
        {
            _offerRepo = offerRepo;
            _taskRepo = taskRepo;
            _userRepo = userRepo;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<TaskOfferDto>> GetByTaskIdAsync(Guid taskId) =>
            (await _offerRepo.GetByTaskIdAsync(taskId)).Select(MapToDto);

        public async Task<IEnumerable<TaskOfferDto>> GetByTeacherIdAsync(Guid teacherId) =>
            (await _offerRepo.GetByTeacherIdAsync(teacherId)).Select(MapToDto);

        public async Task<TaskOfferDto?> GetByIdAsync(Guid id)
        {
            var offer = await _offerRepo.GetByIdAsync(id);
            return offer is null ? null : MapToDto(offer);
        }

        public async Task<TaskOfferDto> CreateAsync(CreateTaskOfferDto dto)
        {
            var task = await _taskRepo.GetByIdAsync(dto.TaskId)
                ?? throw new KeyNotFoundException("Task not found");
            if (task.Status != TaskStatus.Open)
                throw new InvalidOperationException("Task is not accepting offers");

            var teacher = await _userRepo.GetByIdAsync(dto.TeacherId)
                ?? throw new InvalidOperationException("Teacher not found");
            if (!teacher.UserRoles.Any(ur => ur.Role.Name == "teacher"))
                throw new InvalidOperationException("User is not a teacher");

            // ── Restricciones ──────────────────────────────────
            var existing = await _offerRepo.GetByTaskAndTeacherAsync(dto.TaskId, dto.TeacherId);

            if (existing is not null)
            {
                // Solo puede volver a ofertar si retiró la oferta anterior
                if (existing.Status != OfferStatus.Withdrawn)
                    throw new InvalidOperationException("You have already placed an active offer on this task");

                // Reutilizar el registro existente actualizando sus campos
                existing.Price = dto.Price;
                existing.Message = dto.Message;
                existing.DeliveryTime = dto.DeliveryTime;
                existing.SkillsSummary = dto.SkillsSummary;
                existing.Status = OfferStatus.Pending;
                existing.CreatedAt = DateTime.UtcNow;

                var updated = await _offerRepo.UpdateAsync(existing);

                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    UserId = task.StudentId,
                    Title = "Nueva oferta recibida 📩",
                    Message = $"{teacher.Name} realizó una oferta de ${dto.Price:N0} para tu tarea \"{task.Title}\".",
                    Type = NotificationType.Info,
                    Category = NotificationCategory.Marketplace.OfferReceived,
                    ReferenceId = dto.TaskId,
                    ReferenceType = "MarketplaceTask",
                    ActionUrl = $"/student/marketplace/tasks/{dto.TaskId}"
                });

                return MapToDto(updated);
            }

            var count = await _offerRepo.CountByTaskIdAsync(dto.TaskId);
            if (count >= MaxOffersPerTask)
                throw new InvalidOperationException($"This task has reached the maximum of {MaxOffersPerTask} offers");

            var offer = new TaskOffer
            {
                Id = Guid.NewGuid(),
                TaskId = dto.TaskId,
                TeacherId = dto.TeacherId,
                Price = dto.Price,
                Message = dto.Message,
                DeliveryTime = dto.DeliveryTime,
                SkillsSummary = dto.SkillsSummary,
                Status = OfferStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _offerRepo.CreateAsync(offer);

            task.OffersCount = count + 1;
            await _taskRepo.UpdateAsync(task);

            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = task.StudentId,
                Title = "Nueva oferta recibida 📩",
                Message = $"{teacher.Name} realizó una oferta de ${dto.Price:N0} para tu tarea \"{task.Title}\".",
                Type = NotificationType.Info,
                Category = NotificationCategory.Marketplace.OfferReceived,
                ReferenceId = dto.TaskId,
                ReferenceType = "MarketplaceTask",
                ActionUrl = $"/student/marketplace/tasks/{dto.TaskId}"
            });

            return MapToDto(created);
        }

        public async Task<TaskOfferDto> UpdateAsync(Guid id, UpdateTaskOfferDto dto, Guid requestingUserId)
        {
            var offer = await _offerRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Offer not found");

            if (offer.TeacherId != requestingUserId)
                throw new UnauthorizedAccessException("Only the offer owner can update it");

            if (offer.Status != OfferStatus.Pending)
                throw new InvalidOperationException("Only pending offers can be updated");

            if (dto.Price.HasValue) offer.Price = dto.Price.Value;
            if (dto.Message is not null) offer.Message = dto.Message;
            if (dto.DeliveryTime is not null) offer.DeliveryTime = dto.DeliveryTime;
            if (dto.SkillsSummary is not null) offer.SkillsSummary = dto.SkillsSummary;

            return MapToDto(await _offerRepo.UpdateAsync(offer));
        }

        public async Task<MarketplaceTaskDto> SelectOfferAsync(Guid taskId, SelectOfferDto dto, Guid studentId)
        {
            var task = await _taskRepo.GetByIdAsync(taskId)
                ?? throw new KeyNotFoundException("Task not found");

            if (task.StudentId != studentId)
                throw new UnauthorizedAccessException("Only the task owner can select an offer");

            if (task.Status != TaskStatus.Open && task.Status != TaskStatus.InReview)
                throw new InvalidOperationException("Task must be Open or InReview to select an offer");

            var selectedOffer = await _offerRepo.GetByIdAsync(dto.OfferId)
                ?? throw new KeyNotFoundException("Offer not found");

            if (selectedOffer.TaskId != taskId)
                throw new InvalidOperationException("Offer does not belong to this task");

            if (selectedOffer.Status != OfferStatus.Pending)
                throw new InvalidOperationException("Offer is no longer available");

            // Aceptar la oferta seleccionada
            selectedOffer.Status = OfferStatus.Accepted;
            await _offerRepo.UpdateAsync(selectedOffer);

            // Rechazar las demás ofertas
            var allOffers = await _offerRepo.GetByTaskIdAsync(taskId);
            foreach (var other in allOffers.Where(o => o.Id != dto.OfferId && o.Status == OfferStatus.Pending))
            {
                other.Status = OfferStatus.Rejected;
                await _offerRepo.UpdateAsync(other);
            }

            // Actualizar la tarea
            task.SelectedOfferId = dto.OfferId;
            task.AssignedTeacherId = selectedOffer.TeacherId;
            task.FinalPrice = selectedOffer.Price;
            task.Status = TaskStatus.Assigned;
            var updated = await _taskRepo.UpdateAsync(task);

            // Notificar al docente seleccionado
            var teacher = selectedOffer.Teacher;
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = selectedOffer.TeacherId,
                Title = "¡Tu oferta fue aceptada! 🎉",
                Message = $"El estudiante seleccionó tu oferta para la tarea \"{task.Title}\". Espera la confirmación del pago.",
                Type = NotificationType.Success,
                Category = NotificationCategory.Marketplace.OfferAccepted,
                ReferenceId = taskId,
                ReferenceType = "MarketplaceTask",
                ActionUrl = $"/teacher/marketplace/tasks/{taskId}"
            });

            return MapTaskToDto(updated);
        }

        public async Task<bool> WithdrawAsync(Guid offerId, Guid teacherId)
        {
            var offer = await _offerRepo.GetByIdAsync(offerId)
                ?? throw new KeyNotFoundException("Offer not found");

            if (offer.TeacherId != teacherId)
                throw new UnauthorizedAccessException("Only the offer owner can withdraw it");

            if (offer.Status != OfferStatus.Pending)
                throw new InvalidOperationException("Only pending offers can be withdrawn");

            offer.Status = OfferStatus.Withdrawn;
            await _offerRepo.UpdateAsync(offer);

            // Decrementar contador
            var task = await _taskRepo.GetByIdAsync(offer.TaskId);
            if (task is not null)
            {
                task.OffersCount = Math.Max(0, task.OffersCount - 1);
                await _taskRepo.UpdateAsync(task);
            }

            return true;
        }

        private static TaskOfferDto MapToDto(TaskOffer o) => new()
        {
            Id = o.Id,
            TaskId = o.TaskId,
            TeacherId = o.TeacherId,
            TeacherName = o.Teacher?.Name ?? string.Empty,
            TeacherPhotoUrl = o.Teacher?.ProfilePhoto,
            Price = o.Price,
            Message = o.Message,
            DeliveryTime = o.DeliveryTime,
            SkillsSummary = o.SkillsSummary,
            Status = o.Status.ToString(),
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt
        };

        private static MarketplaceTaskDto MapTaskToDto(MarketplaceTask t) => new()
        {
            Id = t.Id,
            StudentId = t.StudentId,
            StudentName = t.Student?.Name ?? string.Empty,
            Title = t.Title,
            Status = t.Status.ToString(),
            AssignedTeacherId = t.AssignedTeacherId,
            SelectedOfferId = t.SelectedOfferId,
            FinalPrice = t.FinalPrice,
            CreatedAt = t.CreatedAt
        };
    }
}
