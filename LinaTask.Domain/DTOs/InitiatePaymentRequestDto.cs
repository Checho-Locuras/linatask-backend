using LinaTask.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.DTOs
{
    public class InitiatePaymentRequestDto
    {
        public Guid? TaskId { get; set; }
        public Guid? SessionId { get; set; }
        public PaymentContextType Context { get; set; } = PaymentContextType.Task;    // "task" | "session"
        public string? PaymentMethod { get; set; }
    }

    public class InitiatePaymentResponseDto
    {
        public Guid PaymentId { get; set; }
        public string PreferenceId { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal TeacherAmount { get; set; }
    }
}
