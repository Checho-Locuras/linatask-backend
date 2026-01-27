namespace LinaTask.Application.DTOs
{
    public class TutoringSessionDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }
        public string? MeetLink { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTutoringSessionDto
    {
        public Guid StudentId { get; set; }
        public Guid TeacherId { get; set; }
        public DateTime SessionDate { get; set; }
        public string? MeetLink { get; set; }
    }

    public class UpdateTutoringSessionDto
    {
        public DateTime? SessionDate { get; set; }
        public string? MeetLink { get; set; }
        public string? Status { get; set; }
    }

    public class SessionStatsDto
    {
        public int TotalSessions { get; set; }
        public int ScheduledSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int CancelledSessions { get; set; }
        public int NoShowSessions { get; set; }
    }
}