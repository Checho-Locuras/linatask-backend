using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Infrastructure.Repositories
{
    public class TaskOfferRepository : ITaskOfferRepository
    {
        private readonly LinaTaskDbContext _context;

        public TaskOfferRepository(LinaTaskDbContext context) => _context = context;

        private IQueryable<TaskOffer> BaseQuery() =>
            _context.TaskOffers
                .Include(o => o.Teacher)
                .Include(o => o.Task);

        public async Task<IEnumerable<TaskOffer>> GetByTaskIdAsync(Guid taskId) =>
            await BaseQuery()
                .Where(o => o.TaskId == taskId)
                .OrderBy(o => o.Price)
                .ToListAsync();

        public async Task<IEnumerable<TaskOffer>> GetByTeacherIdAsync(Guid teacherId) =>
            await BaseQuery()
                .Where(o => o.TeacherId == teacherId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

        public async Task<TaskOffer?> GetByIdAsync(Guid id) =>
            await BaseQuery().FirstOrDefaultAsync(o => o.Id == id);

        public async Task<TaskOffer?> GetByTaskAndTeacherAsync(Guid taskId, Guid teacherId) =>
            await BaseQuery().FirstOrDefaultAsync(o => o.TaskId == taskId && o.TeacherId == teacherId);

        public async Task<int> CountByTaskIdAsync(Guid taskId) =>
            await _context.TaskOffers.CountAsync(o => o.TaskId == taskId);

        public async Task<TaskOffer> CreateAsync(TaskOffer offer)
        {
            _context.TaskOffers.Add(offer);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(offer.Id))!;
        }

        public async Task<TaskOffer> UpdateAsync(TaskOffer offer)
        {
            offer.UpdatedAt = DateTime.UtcNow;
            _context.TaskOffers.Update(offer);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(offer.Id))!;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var offer = await _context.TaskOffers.FindAsync(id);
            if (offer is null) return false;
            _context.TaskOffers.Remove(offer);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
