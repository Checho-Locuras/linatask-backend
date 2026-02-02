using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LinaTask.Domain.DTOs
{
    public class CreateUserDto
    {
        // =====================
        // USER
        // =====================
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public string? ProfilePhoto { get; set; }

        public DateTime? BirthDate { get; set; }

        // =====================
        // ACADEMIC PROFILE (inicial)
        // =====================
        [Required]
        public Guid InstitutionId { get; set; }

        [Required]
        public string EducationLevel { get; set; } = string.Empty;

        public int? CurrentSemester { get; set; }

        public string? CurrentGrade { get; set; }

        public int? GraduationYear { get; set; }

        public string? StudyArea { get; set; }

        [Required]
        public string AcademicStatus { get; set; } = "activo";

        // =====================
        // ADDRESS (inicial)
        // =====================
        [Required]
        public Guid CityId { get; set; }

        [Required, MaxLength(255)]
        public string Address { get; set; } = string.Empty;
    }
}
