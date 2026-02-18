using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinaTask.Domain.Models
{
    /// <summary>
    /// Bloque de disponibilidad semanal recurrente de un docente.
    /// Representa "todos los lunes de 08:00 a 10:00" por ejemplo.
    /// </summary>
    public class TeacherAvailability
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid TeacherId { get; set; }

        [ForeignKey("TeacherId")]
        public virtual User Teacher { get; set; }

        /// <summary>
        /// Día de la semana: 0=Domingo, 1=Lunes, ..., 6=Sábado
        /// </summary>
        [Required]
        [Range(0, 6)]
        public int DayOfWeek { get; set; }

        /// <summary>
        /// Hora de inicio del bloque (ej: 08:00)
        /// </summary>
        [Required]
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Hora de fin del bloque (ej: 10:00)
        /// </summary>
        [Required]
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Duración en minutos de cada slot de tutoría dentro del bloque
        /// </summary>
        [Required]
        [Range(15, 240)]
        public int SlotDurationMinutes { get; set; } = 60;

        /// <summary>
        /// Indica si el bloque está activo (el docente puede desactivarlo temporalmente)
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Notas opcionales del docente sobre este bloque
        /// </summary>
        [MaxLength(300)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}