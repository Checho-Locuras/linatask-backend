namespace LinaTask.Application.DTOs
{
    public class OfferDto
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Message { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateOfferDto
    {
        public Guid TaskId { get; set; }
        public Guid TeacherId { get; set; }
        public decimal Price { get; set; }
        public string? Message { get; set; }
    }

    public class UpdateOfferDto
    {
        public decimal? Price { get; set; }
        public string? Message { get; set; }
        public string? Status { get; set; }
    }
}