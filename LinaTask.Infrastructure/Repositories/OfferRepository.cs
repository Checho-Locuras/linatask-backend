using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Infrastructure.Repositories
{
    public class OfferRepository : IOfferRepository
    {
        private readonly LinaTaskDbContext _context;

        public OfferRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Offer>> GetAllAsync()
        {
            return await _context.Offers
                .Include(o => o.Task)
                .Include(o => o.Teacher)
                .ToListAsync();
        }

        public async Task<IEnumerable<Offer>> GetByTaskIdAsync(Guid taskId)
        {
            return await _context.Offers
                .Include(o => o.Teacher)
                .Where(o => o.TaskId == taskId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Offer>> GetByTeacherIdAsync(Guid teacherId)
        {
            return await _context.Offers
                .Include(o => o.Task)
                .Where(o => o.TeacherId == teacherId)
                .ToListAsync();
        }

        public async Task<Offer?> GetByIdAsync(Guid id)
        {
            return await _context.Offers
                .Include(o => o.Task)
                .Include(o => o.Teacher)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Offer> CreateAsync(Offer offer)
        {
            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();
            return offer;
        }

        public async Task<Offer> UpdateAsync(Offer offer)
        {
            _context.Offers.Update(offer);
            await _context.SaveChangesAsync();
            return offer;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var offer = await GetByIdAsync(id);
            if (offer == null) return false;

            _context.Offers.Remove(offer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid taskId, Guid teacherId)
        {
            return await _context.Offers
                .AnyAsync(o => o.TaskId == taskId && o.TeacherId == teacherId);
        }
    }
}