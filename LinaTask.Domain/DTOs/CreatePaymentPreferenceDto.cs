using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.DTOs
{
    public record CreatePaymentPreferenceDto(
        string Description,
        decimal Amount,
        string PayerEmail,
        string PayerName,
        string ReferenceId,      // taskId o sessionId
        string Context,          // "task" | "session"
        string FrontendBaseUrl,
        string BackendBaseUrl
    );

    public record ConfirmPaymentDto(
        string ExternalPaymentId,
        Guid? TaskId,
        Guid? SessionId
    );
}
