using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Interfaces
{
    public interface IMarketplaceRatingRepository
    {
        Task<IEnumerable<MarketplaceRating>> GetByTaskIdAsync(Guid taskId);
        Task<IEnumerable<MarketplaceRating>> GetByRatedUserAsync(Guid userId);
        Task<MarketplaceRating?> GetByIdAsync(Guid id);
        Task<bool> ExistsAsync(Guid taskId, Guid ratedBy, Guid ratedUser);
        Task<MarketplaceRating> CreateAsync(MarketplaceRating rating);
    }
}
