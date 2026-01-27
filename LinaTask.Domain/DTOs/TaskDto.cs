namespace LinaTask.Application.DTOs
{
    public class TaskDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Subject { get; set; }
        public DateTime? Deadline { get; set; }
        public decimal? Budget { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int OffersCount { get; set; }
    }

    public class CreateTaskDto
    {
        public Guid StudentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Subject { get; set; }
        public DateTime? Deadline { get; set; }
        public decimal? Budget { get; set; }
    }

    public class UpdateTaskDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Subject { get; set; }
        public DateTime? Deadline { get; set; }
        public decimal? Budget { get; set; }
        public string? Status { get; set; }
    }
}