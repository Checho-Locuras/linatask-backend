using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Enums;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
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
        private readonly ITaskAttachmentRepository _attachmentRepository;
        private readonly IFileUploadService _fileUploadService;

        public MarketplaceTaskService(
            IMarketplaceTaskRepository taskRepo,
            IUserRepository userRepo,
            INotificationService notificationService,
            ITaskAttachmentRepository attachmentRepository, 
            IFileUploadService fileUploadService,
            ILogger<MarketplaceTaskService> logger)
        {
            _taskRepo = taskRepo;
            _userRepo = userRepo;
            _attachmentRepository = attachmentRepository;
            _fileUploadService = fileUploadService;
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

        public Task<SuggestedPriceDto> GetSuggestedPriceAsync(WorkType workType, AcademicLevel level, bool isUrgent, DateTime deadline, int? estimatedPages = null, string? estimatedDuration = null, string? description = null)
        {
            // ── 1. Precio base por tipo de trabajo ──────────────────────────────
            decimal basePrice = workType switch
            {
                WorkType.Essay => 40_000,
                WorkType.Workshop => 55_000,
                WorkType.Exam => 50_000,
                WorkType.Project => 80_000,
                WorkType.Programming => 90_000,
                WorkType.Research => 70_000,
                WorkType.Presentation => 60_000,
                WorkType.Other => 45_000,
                _ => 45_000
            };

            // ── 2. Multiplicador de nivel académico ─────────────────────────────
            decimal levelMultiplier = level switch
            {
                AcademicLevel.School => 0.7m,
                AcademicLevel.Technical => 0.9m,
                AcademicLevel.University => 1.0m,
                AcademicLevel.Postgraduate => 1.4m,
                _ => 1.0m
            };

            // ── 3. Multiplicador por páginas estimadas ──────────────────────────
            decimal pageMultiplier = 1.0m;
            if (estimatedPages.HasValue && estimatedPages > 0)
            {
                // Escala logarítmica: 1p=0.8x  5p=1.0x  10p=1.3x  20p=1.6x  50p=2.0x
                pageMultiplier = 0.7m + (decimal)Math.Log(estimatedPages.Value + 1, 2) * 0.18m;
                pageMultiplier = Math.Min(pageMultiplier, 2.5m); // tope
            }

            // ── 4. Multiplicador por duración/cantidad estimada ─────────────────
            decimal durationMultiplier = 1.0m;
            if (!string.IsNullOrWhiteSpace(estimatedDuration))
            {
                // Busca números en el texto: "10 ejercicios" → 10, "2 horas" → 2
                var numbers = System.Text.RegularExpressions.Regex
                    .Matches(estimatedDuration, @"\d+")
                    .Select(m => int.Parse(m.Value))
                    .ToList();

                if (numbers.Any())
                {
                    int qty = numbers.Max();
                    // Escalado suave: 1=0.9  10=1.1  30=1.3  50+=1.5
                    durationMultiplier = 0.9m + Math.Min((decimal)Math.Log(qty + 1, 10) * 0.3m, 0.6m);
                }
            }

            // ── 5. Multiplicador por complejidad de descripción ─────────────────
            decimal complexityMultiplier = 1.0m;
            if (!string.IsNullOrWhiteSpace(description))
            {
                int wordCount = description.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                // Más palabras = descripción más detallada = trabajo más específico
                if (wordCount > 150) complexityMultiplier = 1.20m;
                else if (wordCount > 75) complexityMultiplier = 1.10m;
                else if (wordCount > 30) complexityMultiplier = 1.05m;
                // <30 palabras: sin ajuste
            }

            // ── 6. Multiplicador por urgencia ───────────────────────────────────
            decimal urgencyMultiplier = isUrgent ? 1.35m : 1.0m;

            // ── 7. Multiplicador por deadline ───────────────────────────────────
            var hoursUntilDeadline = (deadline - DateTime.UtcNow).TotalHours;
            decimal deadlineMultiplier = hoursUntilDeadline switch
            {
                < 12 => 1.8m,
                < 24 => 1.5m,
                < 48 => 1.25m,
                < 72 => 1.10m,
                < 168 => 1.0m,
                _ => 0.92m   // más de 7 días → ligero descuento
            };

            // ── 8. Precio final ─────────────────────────────────────────────────
            decimal price = basePrice
                * levelMultiplier
                * pageMultiplier
                * durationMultiplier
                * complexityMultiplier
                * urgencyMultiplier
                * deadlineMultiplier;

            price = Math.Round(price / 1000m, 0) * 1000m; // redondear a miles

            // ── 9. Construir rationale ───────────────────────────────────────────
            var factors = new List<string>();
            if (estimatedPages.HasValue) factors.Add($"{estimatedPages} pág.");
            if (!string.IsNullOrEmpty(estimatedDuration)) factors.Add(estimatedDuration);
            if (isUrgent) factors.Add("recargo de urgencia");
            if (hoursUntilDeadline < 72) factors.Add($"plazo de {Math.Round(hoursUntilDeadline)}h");

            string rationale = $"Estimado para {workType} de nivel {level}"
                + (factors.Any() ? $" — {string.Join(", ", factors)}" : "");

            return Task.FromResult(new SuggestedPriceDto
            {
                SuggestedPrice = price,
                MinPrice = Math.Round(price * 0.7m / 1000m, 0) * 1000m,
                MaxPrice = Math.Round(price * 1.5m / 1000m, 0) * 1000m,
                Rationale = rationale,
                BasePrice = Math.Round(basePrice * levelMultiplier / 1000m, 0) * 1000m,
                UrgencyMultiplier = urgencyMultiplier,
                DeadlineMultiplier = deadlineMultiplier,
                ComplexityMultiplier = complexityMultiplier * pageMultiplier * durationMultiplier
            });
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

        public async Task<MarketplaceTaskDto> DeliverAsync(Guid taskId, IFormFile file, Guid teacherId)
        {
            var task = await _taskRepo.GetByIdAsync(taskId)
                ?? throw new KeyNotFoundException("La tarea no existe");

            if (task.AssignedTeacherId != teacherId)
                throw new UnauthorizedAccessException("No eres el docente asignado");

            if (task.Status != TaskStatus.InProgress && task.Status != TaskStatus.InCorrection && task.Status != TaskStatus.Paid)
                throw new InvalidOperationException("La tarea no está en progreso");

            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Debes subir un archivo");

            // ── Subir archivo a Azure Blob ─────────────────────────────
            var uploadResult = await _fileUploadService.UploadAsync(file, "marketplace-deliveries");

            // ── Registrar adjunto ──────────────────────────────────────
            var attachment = new TaskAttachment
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                FileName = file.FileName,
                FileUrl = uploadResult.Url,
                FileSize = uploadResult.FileSize,
                MimeType = uploadResult.ContentType,
                UploadedBy = teacherId,
                CreatedAt = DateTime.UtcNow
            };

            await _attachmentRepository.AddAsync(attachment);

            // ── Cambiar estado de la tarea ─────────────────────────────
            task.Status = TaskStatus.Delivered;
            task.UpdatedAt = DateTime.UtcNow;

            await _taskRepo.UpdateAsync(task);

            // ── Notificar al estudiante ─────────────────────────────────
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = task.StudentId,
                Title = "📬 Tarea entregada",
                Message = $"El docente entregó la tarea \"{task.Title}\". Revísala y aprueba o solicita correcciones.",
                Type = NotificationType.Info,
                Category = NotificationCategory.Marketplace.Delivered,
                ReferenceId = taskId,
                ReferenceType = "MarketplaceTask",
                ActionUrl = $"/student/tasks/{taskId}",
                Actions = new List<NotificationActionDto>
                {
                    new()
                    {
                        Label = "🔍 Revisar tarea",
                        ActionType = "navigate",
                        Url = $"/student/tasks/{taskId}",
                        Style = "primary"
                    }
                }
            });

            return await GetByIdAsync(taskId);
        }

        public async Task<TaskAttachmentDto> AddAttachmentAsync(Guid taskId, IFormFile file, Guid uploadedBy)
        {
            var task = await _taskRepo.GetByIdAsync(taskId)
                ?? throw new KeyNotFoundException("La tarea no existe");

            if (file == null || file.Length == 0)
                throw new InvalidOperationException("El archivo está vacío");

            if (file.Length > 52_428_800) // 50MB
                throw new InvalidOperationException("El archivo no puede superar 50MB");

            var uploadResult = await _fileUploadService.UploadAsync(file, "marketplace-attachments");

            var attachment = new TaskAttachment
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                FileName = file.FileName,
                FileUrl = uploadResult.Url,
                FileSize = uploadResult.FileSize,
                MimeType = uploadResult.ContentType,
                UploadedBy = uploadedBy,
                CreatedAt = DateTime.UtcNow
            };

            await _attachmentRepository.AddAsync(attachment);

            return new TaskAttachmentDto
            {
                Id = attachment.Id,
                TaskId = attachment.TaskId,
                FileName = attachment.FileName,
                FileUrl = attachment.FileUrl,
                FileSize = attachment.FileSize,
                MimeType = attachment.MimeType,
                UploadedBy = attachment.UploadedBy,
                CreatedAt = attachment.CreatedAt
            };
        }
    }
}
