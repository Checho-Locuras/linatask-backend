using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinaTask.Domain.Models
{
    public class UserAcademicProfile
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid InstitutionId { get; set; }

        [Required, MaxLength(50)]
        public string EducationLevel { get; set; } = null!;

        public int? CurrentSemester { get; set; }

        [MaxLength(20)]
        public string? CurrentGrade { get; set; }

        public int? GraduationYear { get; set; }

        [MaxLength(100)]
        public string? StudyArea { get; set; }

        [Column(TypeName = "text")]
        public string? ProfessionalDescription { get; set; }

        [Required, MaxLength(30)]
        public string AcademicStatus { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        // Relaciones
        public User User { get; set; } = null!;
        public Institution Institution { get; set; } = null!;
        [Required]
        public Guid RoleId { get; set; }

        // Relación
        public Role Role { get; set; } = null!;
    }
}
