namespace LinaTask.Application.DTOs
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal TeacherAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePaymentDto
    {
        public Guid TaskId { get; set; }
        public Guid StudentId { get; set; }
        public decimal Amount { get; set; }
        public decimal PlatformFeePercentage { get; set; } = 10; // 10% por defecto
    }

    public class UpdatePaymentDto
    {
        public string? Status { get; set; }
    }

    public class PaymentStatsDto
    {
        public decimal TotalAmount { get; set; }
        public decimal TotalPlatformFees { get; set; }
        public decimal TotalTeacherAmount { get; set; }
        public int TotalPayments { get; set; }
    }
}