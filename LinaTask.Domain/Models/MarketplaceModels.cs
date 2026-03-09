using LinaTask.Domain.Enums;

namespace LinaTask.Domain.Models
{
    // ──────────────────────────────────────────────────────────
    // MarketplaceTask
    // ──────────────────────────────────────────────────────────
    public class MarketplaceTask
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid? SubjectId { get; set; }

        // Información básica
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WorkType WorkType { get; set; }
        public AcademicLevel AcademicLevel { get; set; }
        public RequiredFormat RequiredFormat { get; set; }

        // Precio
        public decimal Budget { get; set; }
        public decimal? SuggestedPrice { get; set; }
        public decimal? FinalPrice { get; set; }

        // Tiempo
        public DateTime Deadline { get; set; }
        public int? EstimatedPages { get; set; }
        public string? EstimatedDuration { get; set; }

        // Flags
        public bool IsUrgent { get; set; }
        public int OffersCount { get; set; }

        // Estado
        public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.Open;
        public Guid? AssignedTeacherId { get; set; }
        public Guid? SelectedOfferId { get; set; }
        public int CorrectionsUsed { get; set; }
        public int MaxCorrections { get; set; } = 2;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navegación
        public User? Student { get; set; }
        public User? AssignedTeacher { get; set; }
        public TaskOffer? SelectedOffer { get; set; }
        public ICollection<TaskOffer> Offers { get; set; } = new List<TaskOffer>();
        public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
        public ICollection<TaskCorrectionRequest> CorrectionRequests { get; set; } = new List<TaskCorrectionRequest>();
        public ICollection<MarketplacePayment> Payments { get; set; } = new List<MarketplacePayment>();
        public ICollection<MarketplaceRating> Ratings { get; set; } = new List<MarketplaceRating>();
    }

    // ──────────────────────────────────────────────────────────
    // TaskOffer
    // ──────────────────────────────────────────────────────────
    public class TaskOffer
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid TeacherId { get; set; }

        public decimal Price { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DeliveryTime { get; set; } = string.Empty;
        public string? SkillsSummary { get; set; }

        public OfferStatus Status { get; set; } = OfferStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navegación
        public MarketplaceTask? Task { get; set; }
        public User? Teacher { get; set; }
    }

    // ──────────────────────────────────────────────────────────
    // TaskAttachment
    // ──────────────────────────────────────────────────────────
    public class TaskAttachment
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public string? MimeType { get; set; }
        public Guid UploadedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public MarketplaceTask? Task { get; set; }
        public User? Uploader { get; set; }
    }

    // ──────────────────────────────────────────────────────────
    // MarketplacePayment
    // ──────────────────────────────────────────────────────────
    public class MarketplacePayment
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid StudentId { get; set; }
        public Guid TeacherId { get; set; }

        public decimal Amount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal TeacherAmount { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string? PaymentMethod { get; set; }
        public string? ExternalPaymentId { get; set; }

        public DateTime? HeldAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
        public DateTime? RefundedAt { get; set; }
        public DateTime? AutoReleaseAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navegación
        public MarketplaceTask? Task { get; set; }
        public User? Student { get; set; }
        public User? Teacher { get; set; }
    }

    // ──────────────────────────────────────────────────────────
    // TaskCorrectionRequest
    // ──────────────────────────────────────────────────────────
    public class TaskCorrectionRequest
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid StudentId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public CorrectionStatus Status { get; set; } = CorrectionStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navegación
        public MarketplaceTask? Task { get; set; }
        public User? Student { get; set; }
    }

    // ──────────────────────────────────────────────────────────
    // MarketplaceRating
    // ──────────────────────────────────────────────────────────
    public class MarketplaceRating
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid RatedBy { get; set; }
        public Guid RatedUser { get; set; }
        public int Score { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public MarketplaceTask? Task { get; set; }
        public User? Rater { get; set; }
        public User? RatedUserNavigation { get; set; }
    }
}