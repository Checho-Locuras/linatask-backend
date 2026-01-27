namespace LinaTask.Domain.Models
{
    public class TeacherSubject
    {
        public Guid Id { get; set; }
        public Guid TeacherId { get; set; }
        public Guid SubjectId { get; set; }
        public int? ExperienceYears { get; set; }
        public string? CertificationUrl { get; set; }
        public bool IsPrimary { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relaciones
        public User Teacher { get; set; } = null!;
        public Subject Subject { get; set; } = null!;
    }
}