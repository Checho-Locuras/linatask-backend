// LinaTask.Application/DTOs/TutoringSession/TutoringSessionDtos.cs

using LinaTask.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LinaTask.Application.DTOs
{
    // ── RESPUESTA ────────────────────────────────────────────────

    public class TutoringSessionDto
    {
        public Guid Id { get; set; }

        // Participantes
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;

        // Materia
        public Guid? SubjectId { get; set; }
        public string? SubjectName { get; set; }

        // Horario
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        /// <summary>Duración en minutos (calculada).</summary>
        public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;

        // Estado
        public SessionStatus Status { get; set; }
        public string StatusLabel => Status.ToString();

        // Video
        public string? VideoRoomId { get; set; }

        /// <summary>Token que corresponde al usuario que hace la petición (asignado en el servicio).</summary>
        public string? VideoToken { get; set; }

        // Calificación
        public Guid? RatingId { get; set; }
        public SessionRatingDto? Rating { get; set; }

        // Precio
        public decimal? TotalPrice { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ── CREAR ────────────────────────────────────────────────────

    public class CreateTutoringSessionDto
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid TeacherId { get; set; }

        public Guid? SubjectId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        /// <summary>Precio total acordado al reservar.</summary>
        public decimal? TotalPrice { get; set; }
    }

    // ── ACTUALIZAR ───────────────────────────────────────────────

    public class UpdateTutoringSessionDto
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public SessionStatus? Status { get; set; }
    }

    // ── STATS ────────────────────────────────────────────────────

    public class SessionStatsDto
    {
        public int TotalSessions { get; set; }
        public int ScheduledSessions { get; set; }
        public int ReadySessions { get; set; }
        public int InProgressSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int CancelledSessions { get; set; }
        public int NoShowSessions { get; set; }
    }

    // ── RATING ───────────────────────────────────────────────────

    public class SessionRatingDto
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public Guid RatedByUserId { get; set; }
        public int Score { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateSessionRatingDto
    {
        [Required]
        public Guid SessionId { get; set; }

        [Required, Range(1, 5)]
        public int Score { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }

    // ── VIDEO TOKEN (respuesta al pedir acceso a sala) ───────────

    public class VideoRoomAccessDto
    {
        public string RoomId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RoomUrl { get; set; } = string.Empty;
    }
}