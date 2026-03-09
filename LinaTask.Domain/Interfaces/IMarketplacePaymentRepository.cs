using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Interfaces
{
    public interface IMarketplacePaymentRepository
    {
        Task<MarketplacePayment?> GetByTaskIdAsync(Guid taskId);
        Task<MarketplacePayment?> GetByIdAsync(Guid id);
        Task<IEnumerable<MarketplacePayment>> GetPendingAutoReleaseAsync(DateTime upTo);
        Task<MarketplacePayment> CreateAsync(MarketplacePayment payment);
        Task<MarketplacePayment> UpdateAsync(MarketplacePayment payment);
    }
}
