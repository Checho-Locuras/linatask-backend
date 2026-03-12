using LinaTask.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using TaskStatus = LinaTask.Domain.Enums.TaskStatus;

namespace LinaTask.Domain.DTOs
{
    public class MarketplaceTaskDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public Guid? SubjectId { get; set; }
        public string? SubjectName { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string WorkType { get; set; } = string.Empty;
        public string AcademicLevel { get; set; } = string.Empty;
        public string RequiredFormat { get; set; } = string.Empty;

        public decimal Budget { get; set; }
        public decimal? SuggestedPrice { get; set; }
        public decimal? FinalPrice { get; set; }

        public DateTime Deadline { get; set; }
        public int? EstimatedPages { get; set; }
        public string? EstimatedDuration { get; set; }
        public bool IsUrgent { get; set; }
        public int OffersCount { get; set; }

        public string Status { get; set; } = string.Empty;
        public Guid? AssignedTeacherId { get; set; }
        public string? AssignedTeacherName { get; set; }
        public Guid? SelectedOfferId { get; set; }
        public int CorrectionsUsed { get; set; }
        public int MaxCorrections { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<TaskAttachmentDto> Attachments { get; set; } = new();
        public List<TaskOfferDto> Offers { get; set; } = new();
    }

    public class CreateMarketplaceTaskDto
    {
        public Guid StudentId { get; set; }
        public Guid? SubjectId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WorkType WorkType { get; set; }
        public AcademicLevel AcademicLevel { get; set; }
        public RequiredFormat RequiredFormat { get; set; }

        public decimal Budget { get; set; }
        public DateTime Deadline { get; set; }
        public int? EstimatedPages { get; set; }
        public string? EstimatedDuration { get; set; }
        public bool IsUrgent { get; set; }
    }

    public class UpdateMarketplaceTaskDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal? Budget { get; set; }
        public DateTime? Deadline { get; set; }
        public bool? IsUrgent { get; set; }
        public TaskStatus? Status { get; set; }
    }

    // ══════════════════════════════════════════════════════════
    // OFFER DTOs
    // ══════════════════════════════════════════════════════════

    public class TaskOfferDto
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string? TeacherPhotoUrl { get; set; }
        public double? TeacherRating { get; set; }
        public int TeacherCompletedTasks { get; set; }

        public decimal Price { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DeliveryTime { get; set; } = string.Empty;
        public string? SkillsSummary { get; set; }
        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateTaskOfferDto
    {
        public Guid TaskId { get; set; }
        public Guid TeacherId { get; set; }
        public decimal Price { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DeliveryTime { get; set; } = string.Empty;
        public string? SkillsSummary { get; set; }
    }

    public class UpdateTaskOfferDto
    {
        public decimal? Price { get; set; }
        public string? Message { get; set; }
        public string? DeliveryTime { get; set; }
        public string? SkillsSummary { get; set; }
    }

    public class SelectOfferDto
    {
        public Guid OfferId { get; set; }
    }

    // ══════════════════════════════════════════════════════════
    // PAYMENT DTOs
    // ══════════════════════════════════════════════════════════

    public class MarketplacePaymentDto
    {
        public Guid Id { get; set; }
        public Guid? TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal TeacherAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public DateTime? HeldAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
        public DateTime? AutoReleaseAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? SessionId { get; set; }          // null si es tarea
        public string PaymentContext { get; set; } = "task"; // "task" | "session"
    }

    public class InitiatePaymentDto
    {
        public Guid? TaskId { get; set; }
        public Guid? SessionId { get; set; }

        public PaymentContextType PaymentContext { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class ReleasePaymentDto
    {
        public Guid TaskId { get; set; }
    }

    // ══════════════════════════════════════════════════════════
    // ATTACHMENT DTOs
    // ══════════════════════════════════════════════════════════

    public class TaskAttachmentDto
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public string? MimeType { get; set; }
        public Guid UploadedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ══════════════════════════════════════════════════════════
    // CORRECTION DTOs
    // ══════════════════════════════════════════════════════════

    public class TaskCorrectionRequestDto
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateCorrectionRequestDto
    {
        public Guid TaskId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════
    // RATING DTOs
    // ══════════════════════════════════════════════════════════

    public class MarketplaceRatingDto
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid RatedBy { get; set; }
        public string RatedByName { get; set; } = string.Empty;
        public Guid RatedUser { get; set; }
        public string RatedUserName { get; set; } = string.Empty;
        public int Score { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateMarketplaceRatingDto
    {
        public Guid TaskId { get; set; }
        public Guid RatedUser { get; set; }
        public int Score { get; set; }
        public string? Comment { get; set; }
    }

    // ══════════════════════════════════════════════════════════
    // STATS DTO
    // ══════════════════════════════════════════════════════════

    public class MarketplaceStatsDto
    {
        public int TotalTasks { get; set; }
        public int OpenTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int CancelledTasks { get; set; }
        public int UrgentTasks { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // ══════════════════════════════════════════════════════════
    // SUGGESTED PRICE DTO
    // ══════════════════════════════════════════════════════════

    public class SuggestedPriceDto
    {
        public decimal SuggestedPrice { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public string Rationale { get; set; } = string.Empty;

        // Desglose de factores (nuevo — para mostrar en UI)
        public decimal BasePrice { get; set; }
        public decimal UrgencyMultiplier { get; set; }
        public decimal DeadlineMultiplier { get; set; }
        public decimal ComplexityMultiplier { get; set; }
    }

    public class AddTaskAttachmentDto
    {
        public Guid UploadedBy { get; set; }
        // El archivo viene como IFormFile en el controller
    }
}
