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
    public class MarketplaceRatingService : IMarketplaceRatingService
    {
        private readonly IMarketplaceRatingRepository _ratingRepo;
        private readonly IMarketplaceTaskRepository _taskRepo;
        private readonly INotificationService _notificationService;

        public MarketplaceRatingService(
            IMarketplaceRatingRepository ratingRepo,
            IMarketplaceTaskRepository taskRepo,
            INotificationService notificationService)
        {
            _ratingRepo = ratingRepo;
            _taskRepo = taskRepo;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<MarketplaceRatingDto>> GetByTaskIdAsync(Guid taskId) =>
            (await _ratingRepo.GetByTaskIdAsync(taskId)).Select(MapToDto);

        public async Task<IEnumerable<MarketplaceRatingDto>> GetByUserAsync(Guid userId) =>
            (await _ratingRepo.GetByRatedUserAsync(userId)).Select(MapToDto);

        public async Task<MarketplaceRatingDto> CreateAsync(CreateMarketplaceRatingDto dto, Guid ratedBy)
        {
            var task = await _taskRepo.GetByIdAsync(dto.TaskId)
                ?? throw new KeyNotFoundException("Task not found");

            if (task.Status != TaskStatus.Completed)
                throw new InvalidOperationException("Can only rate completed tasks");

            // Validar que quien califica sea participante
            bool isStudent = task.StudentId == ratedBy;
            bool isTeacher = task.AssignedTeacherId == ratedBy;

            if (!isStudent && !isTeacher)
                throw new UnauthorizedAccessException("Only task participants can rate");

            // Validar que el usuario calificado sea el otro participante
            var expectedRatedUser = isStudent ? task.AssignedTeacherId!.Value : task.StudentId;
            if (dto.RatedUser != expectedRatedUser)
                throw new InvalidOperationException("Invalid rated user for this task");

            if (await _ratingRepo.ExistsAsync(dto.TaskId, ratedBy, dto.RatedUser))
                throw new InvalidOperationException("You have already rated this user for this task");

            if (dto.Score < 1 || dto.Score > 5)
                throw new InvalidOperationException("Score must be between 1 and 5");

            var rating = new MarketplaceRating
            {
                Id = Guid.NewGuid(),
                TaskId = dto.TaskId,
                RatedBy = ratedBy,
                RatedUser = dto.RatedUser,
                Score = dto.Score,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _ratingRepo.CreateAsync(rating);

            // Notificar al usuario calificado
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = dto.RatedUser,
                Title = $"Nueva calificación: {dto.Score}/5 ⭐",
                Message = $"Recibiste una calificación de {dto.Score}/5 por la tarea \"{task.Title}\".",
                Type = NotificationType.Info,
                Category = NotificationCategory.Marketplace.OfferAccepted,
                ReferenceId = dto.TaskId,
                ReferenceType = "MarketplaceTask"
            });

            return MapToDto(created);
        }

        private static MarketplaceRatingDto MapToDto(MarketplaceRating r) => new()
        {
            Id = r.Id,
            TaskId = r.TaskId,
            RatedBy = r.RatedBy,
            RatedByName = r.Rater?.Name ?? string.Empty,
            RatedUser = r.RatedUser,
            RatedUserName = r.RatedUserNavigation?.Name ?? string.Empty,
            Score = r.Score,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        };
    }
}
