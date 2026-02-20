using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinaTask.Domain.Models
{
    public class SessionRating
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SessionId { get; set; }

        [ForeignKey("SessionId")]
        public virtual TutoringSession Session { get; set; } = null!;

        /// <summary>Quién dejó la calificación (siempre el estudiante por ahora).</summary>
        [Required]
        public Guid RatedByUserId { get; set; }

        [ForeignKey("RatedByUserId")]
        public virtual User RatedByUser { get; set; } = null!;

        /// <summary>1–5</summary>
        [Range(1, 5)]
        public int Score { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}