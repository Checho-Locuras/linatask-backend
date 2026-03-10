using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Enums;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using TaskStatus = LinaTask.Domain.Enums.TaskStatus;

namespace LinaTask.Application.Services
{
    public class MarketplaceTaskService : IMarketplaceTaskService
    {
        private readonly IMarketplaceTaskRepository _taskRepo;
        private readonly IUserRepository _userRepo;
        private readonly INotificationService _notificationService;
        private readonly ILogger<MarketplaceTaskService> _logger;

        public MarketplaceTaskService(
            IMarketplaceTaskRepository taskRepo,
            IUserRepository userRepo,
            INotificationService notificationService,
            ILogger<MarketplaceTaskService> logger)
        {
            _taskRepo = taskRepo;
            _userRepo = userRepo;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IEnumerable<MarketplaceTaskDto>> GetAllAsync(bool onlyOpen = false) =>
            (await _taskRepo.GetAllAsync(onlyOpen)).Select(MapToDto);

        public async Task<IEnumerable<MarketplaceTaskDto>> GetByStudentIdAsync(Guid studentId) =>
            (await _taskRepo.GetByStudentIdAsync(studentId)).Select(MapToDto);

        public async Task<IEnumerable<MarketplaceTaskDto>> GetByTeacherIdAsync(Guid teacherId) =>
            (await _taskRepo.GetByTeacherIdAsync(teacherId)).Select(MapToDto);

        public async Task<IEnumerable<MarketplaceTaskDto>> GetByStatusAsync(TaskStatus status) =>
            (await _taskRepo.GetByStatusAsync(status)).Select(MapToDto);

        public async Task<IEnumerable<MarketplaceTaskDto>> GetUrgentAsync() =>
            (await _taskRepo.GetUrgentAsync()).Select(MapToDto);

        public async Task<MarketplaceTaskDto?> GetByIdAsync(Guid id)
        {
            var task = await _taskRepo.GetByIdAsync(id);
            return task is null ? null : MapToDto(task);
        }

        public async Task<MarketplaceTaskDto> CreateAsync(CreateMarketplaceTaskDto dto, Guid requestingUserId)
        {
            var student = await _userRepo.GetByIdAsync(dto.StudentId)
                ?? throw new InvalidOperationException("Student not found");

            if (!student.UserRoles.Any(ur => ur.Role.Name == "student"))
                throw new InvalidOperationException("User is not a student");

            if (dto.Deadline <= DateTime.UtcNow)
                throw new InvalidOperationException("Deadline must be in the future");

            if (dto.Budget <= 0)
                throw new InvalidOperationException("Budget must be greater than zero");

            var suggestedPrice = CalculateSuggestedPrice(dto.WorkType, dto.AcademicLevel, dto.IsUrgent, dto.Deadline);

            var task = new MarketplaceTask
            {
                Id = Guid.NewGuid(),
                StudentId = dto.StudentId,
                SubjectId = dto.SubjectId,
                Title = dto.Title,
                Description = dto.Description,
                WorkType = dto.WorkType,
                AcademicLevel = dto.AcademicLevel,
                RequiredFormat = dto.RequiredFormat,
                Budget = dto.Budget,
                SuggestedPrice = suggestedPrice,
                Deadline = dto.Deadline.ToUniversalTime(),
                EstimatedPages = dto.EstimatedPages,
                EstimatedDuration = dto.EstimatedDuration,
                IsUrgent = dto.IsUrgent,
                Status = TaskStatus.Open,
                MaxCorrections = 2,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _taskRepo.CreateAsync(task);

            // Si es urgente, notificar a docentes (la plataforma deberá tener mecanismo para esto)
            _logger.LogInformation("Marketplace task {TaskId} created by student {StudentId}", created.Id, dto.StudentId);

            return MapToDto(created);
        }

        public async Task<MarketplaceTaskDto> UpdateAsync(Guid id, UpdateMarketplaceTaskDto dto, Guid requestingUserId)
        {
            var task = await _taskRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Task {id} not found");

            if (task.StudentId != requestingUserId)
                throw new UnauthorizedAccessException("Only the task owner can update it");

            if (task.Status != TaskStatus.Open && task.Status != TaskStatus.InReview)
                throw new InvalidOperationException("Only Open or InReview tasks can be edited");

            if (dto.Title is not null) task.Title = dto.Title;
            if (dto.Description is not null) task.Description = dto.Description;
            if (dto.Budget.HasValue) task.Budget = dto.Budget.Value;
            if (dto.Deadline.HasValue) task.Deadline = dto.Deadline.Value.ToUniversalTime();
            if (dto.IsUrgent.HasValue) task.IsUrgent = dto.IsUrgent.Value;

            return MapToDto(await _taskRepo.UpdateAsync(task));
        }

        public async Task<bool> DeleteAsync(Guid id, Guid requestingUserId)
        {
            var task = await _taskRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Task {id} not found");

            if (task.StudentId != requestingUserId)
                throw new UnauthorizedAccessException("Only the task owner can delete it");

            if (task.Status == TaskStatus.Paid || task.Status == TaskStatus.InProgress)
                throw new InvalidOperationException("Cannot delete a task that is paid or in progress");

            return await _taskRepo.DeleteAsync(id);
        }

        public Task<SuggestedPriceDto> GetSuggestedPriceAsync(
            WorkType workType, AcademicLevel level, bool isUrgent, DateTime deadline)
        {
            var price = CalculateSuggestedPrice(workType, level, isUrgent, deadline);
            var result = new SuggestedPriceDto
            {
                SuggestedPrice = price,
                MinPrice = Math.Round(price * 0.7m, 0),
                MaxPrice = Math.Round(price * 1.5m, 0),
                Rationale = $"Basado en {workType} de nivel {level}" +
                            (isUrgent ? ", con recargo de urgencia" : "")
            };
            return Task.FromResult(result);
        }

        public async Task<MarketplaceStatsDto> GetStatsAsync(Guid? userId = null)
        {
            IEnumerable<MarketplaceTask> tasks;

            if (userId.HasValue)
            {
                var studentTasks = await _taskRepo.GetByStudentIdAsync(userId.Value);
                var teacherTasks = await _taskRepo.GetByTeacherIdAsync(userId.Value);
                tasks = studentTasks.Concat(teacherTasks).DistinctBy(t => t.Id);
            }
            else
            {
                tasks = await _taskRepo.GetAllAsync();
            }

            var list = tasks.ToList();
            return new MarketplaceStatsDto
            {
                TotalTasks = list.Count,
                OpenTasks = list.Count(t => t.Status == TaskStatus.Open),
                InProgressTasks = list.Count(t => t.Status == TaskStatus.InProgress),
                CompletedTasks = list.Count(t => t.Status == TaskStatus.Completed),
                CancelledTasks = list.Count(t => t.Status == TaskStatus.Cancelled),
                UrgentTasks = list.Count(t => t.IsUrgent),
                TotalRevenue = list.Where(t => t.Status == TaskStatus.Completed)
                                   .Sum(t => t.FinalPrice ?? 0)
            };
        }

        // ── PRECIO SUGERIDO ────────────────────────────────────
        private static decimal CalculateSuggestedPrice(WorkType workType, AcademicLevel level, bool isUrgent, DateTime deadline)
        {
            decimal base_ = workType switch
            {
                WorkType.Essay => 30000,
                WorkType.Workshop => 25000,
                WorkType.Exam => 40000,
                WorkType.Project => 60000,
                WorkType.Programming => 70000,
                WorkType.Research => 50000,
                WorkType.Presentation => 35000,
                _ => 25000
            };

            decimal levelFactor = level switch
            {
                AcademicLevel.School => 0.7m,
                AcademicLevel.Technical => 0.9m,
                AcademicLevel.University => 1.0m,
                AcademicLevel.Postgraduate => 1.5m,
                _ => 1.0m
            };

            var hoursUntilDeadline = (deadline - DateTime.UtcNow).TotalHours;

            decimal urgencyFactor =
                isUrgent || hoursUntilDeadline < 24 ? 1.5m :
                hoursUntilDeadline < 72 ? 1.2m :
                1.0m;

            var price = base_ * levelFactor * urgencyFactor;

            return Math.Round(price / 1000m, 0) * 1000m; // redondeo a miles
        }

        // ── MAPPER ─────────────────────────────────────────────
        private static MarketplaceTaskDto MapToDto(MarketplaceTask t) => new()
        {
            Id = t.Id,
            StudentId = t.StudentId,
            StudentName = t.Student?.Name ?? string.Empty,
            SubjectId = t.SubjectId,
            Title = t.Title,
            Description = t.Description,
            WorkType = t.WorkType.ToString(),
            AcademicLevel = t.AcademicLevel.ToString(),
            RequiredFormat = t.RequiredFormat.ToString(),
            Budget = t.Budget,
            SuggestedPrice = t.SuggestedPrice,
            FinalPrice = t.FinalPrice,
            Deadline = t.Deadline,
            EstimatedPages = t.EstimatedPages,
            EstimatedDuration = t.EstimatedDuration,
            IsUrgent = t.IsUrgent,
            OffersCount = t.OffersCount,
            Status = t.Status.ToString(),
            AssignedTeacherId = t.AssignedTeacherId,
            AssignedTeacherName = t.AssignedTeacher?.Name,
            SelectedOfferId = t.SelectedOfferId,
            CorrectionsUsed = t.CorrectionsUsed,
            MaxCorrections = t.MaxCorrections,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            Attachments = t.Attachments.Select(a => new TaskAttachmentDto
            {
                Id = a.Id,
                TaskId = a.TaskId,
                FileName = a.FileName,
                FileUrl = a.FileUrl,
                FileSize = a.FileSize,
                MimeType = a.MimeType,
                UploadedBy = a.UploadedBy,
                CreatedAt = a.CreatedAt
            }).ToList(),
            Offers = t.Offers.Select(o => new TaskOfferDto
            {
                Id = o.Id,
                TaskId = o.TaskId,
                TeacherId = o.TeacherId,
                TeacherName = o.Teacher?.Name ?? string.Empty,
                Price = o.Price,
                Message = o.Message,
                DeliveryTime = o.DeliveryTime,
                SkillsSummary = o.SkillsSummary,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            }).ToList()
        };
    }
}
