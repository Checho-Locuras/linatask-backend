using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Infrastructure.Repositories
{
    public class MarketplaceRatingRepository : IMarketplaceRatingRepository
    {
        private readonly LinaTaskDbContext _context;

        public MarketplaceRatingRepository(LinaTaskDbContext context) => _context = context;

        public async Task<IEnumerable<MarketplaceRating>> GetByTaskIdAsync(Guid taskId) =>
            await _context.MarketplaceRatings
                .Include(r => r.Rater)
                .Include(r => r.RatedUserNavigation)
                .Where(r => r.TaskId == taskId)
                .ToListAsync();

        public async Task<IEnumerable<MarketplaceRating>> GetByRatedUserAsync(Guid userId) =>
            await _context.MarketplaceRatings
                .Include(r => r.Rater)
                .Include(r => r.Task)
                .Where(r => r.RatedUser == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<MarketplaceRating?> GetByIdAsync(Guid id) =>
            await _context.MarketplaceRatings.FindAsync(id);

        public async Task<bool> ExistsAsync(Guid taskId, Guid ratedBy, Guid ratedUser) =>
            await _context.MarketplaceRatings
                .AnyAsync(r => r.TaskId == taskId && r.RatedBy == ratedBy && r.RatedUser == ratedUser);

        public async Task<MarketplaceRating> CreateAsync(MarketplaceRating rating)
        {
            _context.MarketplaceRatings.Add(rating);
            await _context.SaveChangesAsync();
            return rating;
        }
    }
}
