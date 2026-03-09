using LinaTask.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Interfaces
{
    public interface IMarketplacePaymentService
    {
        Task<MarketplacePaymentDto?> GetByTaskIdAsync(Guid taskId);
        Task<MarketplacePaymentDto> InitiatePaymentAsync(InitiatePaymentDto dto, Guid studentId);
        Task<MarketplacePaymentDto> ConfirmPaymentHeldAsync(Guid taskId);
        Task<MarketplacePaymentDto> ReleasePaymentAsync(Guid taskId, Guid studentId);
        Task<MarketplacePaymentDto> RefundPaymentAsync(Guid taskId);
        Task ProcessAutoReleasesAsync();
    }
}
