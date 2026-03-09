using System;

namespace LinaTask.Domain.DTOs
{
    public class UserAcademicProfileDto
    {
        public Guid Id { get; set; }

        public Guid InstitutionId { get; set; }
        public string InstitutionName { get; set; } = string.Empty;

        public string EducationLevel { get; set; } = string.Empty;
        public int? CurrentSemester { get; set; }
        public string? CurrentGrade { get; set; }
        public int? GraduationYear { get; set; }
        public string? StudyArea { get; set; }
        public string AcademicStatus { get; set; } = string.Empty;
        public string? ProfessionalDescription { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}
