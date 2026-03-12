using LinaTask.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services.Interfaces
{
    public interface IMercadoPagoService
    {
        Task<CreatePaymentPreferenceResult> CreatePreferenceAsync(CreatePaymentPreferenceDto dto);
        Task<MercadoPagoPaymentInfo> GetPaymentInfoAsync(string externalPaymentId);
    }
}
