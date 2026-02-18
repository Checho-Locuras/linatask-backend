using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LinaTask.Application.DTOs
{
    // ── RESPONSE ──────────────────────────────────────────────

    public class TeacherAvailabilityDto
    {
        public Guid Id { get; set; }
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; }
        public int DayOfWeek { get; set; }
        public string DayName { get; set; }          // "Lunes", "Martes"…
        public string StartTime { get; set; }         // "08:00"
        public string EndTime { get; set; }           // "10:00"
        public int SlotDurationMinutes { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Slots calculados para una fecha concreta (se rellena solo cuando
        /// el cliente pide disponibilidad para un día específico)
        /// </summary>
        public List<AvailabilitySlotDto>? Slots { get; set; }
    }

    public class AvailabilitySlotDto
    {
        public string StartTime { get; set; }    // "08:00"
        public string EndTime { get; set; }      // "09:00"
        public bool IsAvailable { get; set; }
        public string? BlockedReason { get; set; }   // "Sesión agendada"
    }

    /// <summary>
    /// Vista semanal de un docente: lista de bloques + para cada uno, si hay colisión
    /// con sesiones ya agendadas en la semana solicitada.
    /// </summary>
    public class TeacherWeeklyAvailabilityDto
    {
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; }
        public string WeekStart { get; set; }     // ISO "2025-01-06"
        public string WeekEnd { get; set; }       // ISO "2025-01-12"
        public List<DayAvailabilityDto> Days { get; set; } = new();
    }

    public class DayAvailabilityDto
    {
        public int DayOfWeek { get; set; }
        public string DayName { get; set; }
        public string Date { get; set; }           // ISO "2025-01-06"
        public List<AvailabilitySlotDto> Slots { get; set; } = new();
    }

    // ── REQUEST ───────────────────────────────────────────────

    public class CreateAvailabilityDto
    {
        [Required]
        [Range(0, 6)]
        public int DayOfWeek { get; set; }

        [Required]
        public string StartTime { get; set; }     // "08:00"

        [Required]
        public string EndTime { get; set; }       // "10:00"

        [Range(15, 240)]
        public int SlotDurationMinutes { get; set; } = 60;

        public bool IsActive { get; set; } = true;

        [MaxLength(300)]
        public string? Notes { get; set; }
    }

    public class UpdateAvailabilityDto
    {
        [Required]
        public string StartTime { get; set; }

        [Required]
        public string EndTime { get; set; }

        [Range(15, 240)]
        public int SlotDurationMinutes { get; set; } = 60;

        public bool IsActive { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }
    }

    public class BulkSaveAvailabilityDto
    {
        [Required]
        public Guid TeacherId { get; set; }

        /// <summary>
        /// Lista completa de disponibilidades semanales.
        /// El servicio reemplaza los registros existentes del docente.
        /// </summary>
        [Required]
        public List<CreateAvailabilityDto> Availabilities { get; set; } = new();
    }
}