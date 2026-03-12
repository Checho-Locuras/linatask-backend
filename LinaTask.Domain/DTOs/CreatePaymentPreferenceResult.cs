using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.DTOs
{
    public record CreatePaymentPreferenceResult(
        string PreferenceId,
        string InitPoint,     // URL checkout clásico (fallback)
        string PublicKey
    );

    public record MercadoPagoPaymentInfo(
        string Id,
        string Status,        // approved | pending | rejected
        string StatusDetail,
        decimal TransactionAmount
    );
}
