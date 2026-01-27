namespace LinaTask.Application.DTOs
{
    public class TeacherSubjectDto
    {
        public Guid Id { get; set; }
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int? ExperienceYears { get; set; }
        public string? CertificationUrl { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTeacherSubjectDto
    {
        public Guid TeacherId { get; set; }
        public Guid SubjectId { get; set; }
        public int? ExperienceYears { get; set; }
        public string? CertificationUrl { get; set; }
        public bool IsPrimary { get; set; } = false;
    }

    public class UpdateTeacherSubjectDto
    {
        public int? ExperienceYears { get; set; }
        public string? CertificationUrl { get; set; }
        public bool? IsPrimary { get; set; }
    }
}