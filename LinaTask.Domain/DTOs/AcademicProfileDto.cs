using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LinaTask.Domain.DTOs
{
    // DTO para perfiles académicos en el registro
    public class AcademicProfileDto
    {
        public Guid? Id { get; set; }
        // Identificador del rol asociado (student o teacher)
        [Required]
        public Guid RoleId { get; set; }

        [Required(ErrorMessage = "La institución es requerida")]
        public Guid InstitutionId { get; set; }

        [Required(ErrorMessage = "El nivel educativo es requerido")]
        [MaxLength(50)]
        public string EducationLevel { get; set; } = string.Empty;

        public int? CurrentSemester { get; set; }

        [MaxLength(20)]
        public string? CurrentGrade { get; set; }

        public int? GraduationYear { get; set; }

        [MaxLength(100)]
        public string? StudyArea { get; set; }

        [Required]
        [MaxLength(30)]
        public string AcademicStatus { get; set; } = "activo";
        public string? ProfessionalDescription { get; set; }
    }
}
