using LinaTask.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services.Interfaces
{
    public interface IMarketplaceRatingService
    {
        Task<IEnumerable<MarketplaceRatingDto>> GetByTaskIdAsync(Guid taskId);
        Task<IEnumerable<MarketplaceRatingDto>> GetByUserAsync(Guid userId);
        Task<MarketplaceRatingDto> CreateAsync(CreateMarketplaceRatingDto dto, Guid ratedBy);
    }
}
