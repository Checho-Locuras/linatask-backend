// LinaTask.Domain/Models/TutoringSession.cs

using LinaTask.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinaTask.Domain.Models
{
    public class TutoringSession
    {
        [Key]
        public Guid Id { get; set; }

        // ── Participantes ─────────────────────────────────────
        [Required]
        public Guid StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual User Student { get; set; } = null!;

        [Required]
        public Guid TeacherId { get; set; }

        [ForeignKey("TeacherId")]
        public virtual User Teacher { get; set; } = null!;

        // ── Materia ───────────────────────────────────────────
        /// <summary>Materia asociada a la sesión (opcional por compatibilidad).</summary>
        public Guid? SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        // ── Horario ───────────────────────────────────────────
        /// <summary>Inicio programado de la sesión (UTC).</summary>
        [Required]
        public DateTime StartTime { get; set; }

        /// <summary>Fin programado de la sesión (UTC).</summary>
        [Required]
        public DateTime EndTime { get; set; }

        // ── Estado ────────────────────────────────────────────
        public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

        // ── Video (100ms) ─────────────────────────────────────
        /// <summary>Room ID generado por 100ms.</summary>
        [MaxLength(128)]
        public string? VideoRoomId { get; set; }

        /// <summary>Token de acceso del estudiante (corta duración, regenerar si expira).</summary>
        [MaxLength(2048)]
        public string? StudentToken { get; set; }

        /// <summary>Token de acceso del docente.</summary>
        [MaxLength(2048)]
        public string? TeacherToken { get; set; }

        // ── Calificación ──────────────────────────────────────
        public Guid? RatingId { get; set; }

        [ForeignKey("RatingId")]
        public virtual SessionRating? Rating { get; set; }

        // ── Metadata ──────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // ── Precio pactado al momento de reservar ─────────────
        /// <summary>Precio total de la sesión (snapshot del pricePerHour × horas).</summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal? TotalPrice { get; set; }
    }
}