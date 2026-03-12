using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.DTOs
{
    public class ConfirmHeldRequestDto
    {
        public string ExternalPaymentId { get; set; } = string.Empty;
    }
}
