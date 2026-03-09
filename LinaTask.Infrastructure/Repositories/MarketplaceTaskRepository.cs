using LinaTask.Domain.Enums;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TaskStatus = LinaTask.Domain.Enums.TaskStatus;

namespace LinaTask.Infrastructure.Repositories
{
    public class MarketplaceTaskRepository : IMarketplaceTaskRepository
    {
        private readonly LinaTaskDbContext _context;

        public MarketplaceTaskRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        private IQueryable<MarketplaceTask> BaseQuery() =>
            _context.MarketplaceTasks
                .Include(t => t.Student)
                .Include(t => t.AssignedTeacher)
                .Include(t => t.Attachments)
                    .ThenInclude(a => a.Uploader)
                .Include(t => t.Offers)
                    .ThenInclude(o => o.Teacher)
                .Include(t => t.SelectedOffer);

        public async Task<IEnumerable<MarketplaceTask>> GetAllAsync(bool onlyOpen = false)
        {
            var query = BaseQuery();
            if (onlyOpen)
                query = query.Where(t => t.Status == TaskStatus.Open);
            return await query.OrderByDescending(t => t.IsUrgent)
                              .ThenByDescending(t => t.CreatedAt)
                              .ToListAsync();
        }

        public async Task<IEnumerable<MarketplaceTask>> GetByStudentIdAsync(Guid studentId) =>
            await BaseQuery()
                .Where(t => t.StudentId == studentId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<MarketplaceTask>> GetByTeacherIdAsync(Guid teacherId) =>
            await BaseQuery()
                .Where(t => t.AssignedTeacherId == teacherId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<MarketplaceTask>> GetByStatusAsync(TaskStatus status) =>
            await BaseQuery()
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.IsUrgent)
                .ThenBy(t => t.Deadline)
                .ToListAsync();

        public async Task<IEnumerable<MarketplaceTask>> GetUrgentAsync() =>
            await BaseQuery()
                .Where(t => t.IsUrgent && t.Status == TaskStatus.Open)
                .OrderBy(t => t.Deadline)
                .ToListAsync();

        public async Task<MarketplaceTask?> GetByIdAsync(Guid id) =>
            await BaseQuery().FirstOrDefaultAsync(t => t.Id == id);

        public async Task<MarketplaceTask> CreateAsync(MarketplaceTask task)
        {
            _context.MarketplaceTasks.Add(task);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(task.Id))!;
        }

        public async Task<MarketplaceTask> UpdateAsync(MarketplaceTask task)
        {
            task.UpdatedAt = DateTime.UtcNow;
            _context.MarketplaceTasks.Update(task);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(task.Id))!;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var task = await _context.MarketplaceTasks.FindAsync(id);
            if (task is null) return false;
            _context.MarketplaceTasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetCompletedCountByTeacherAsync(Guid teacherId) =>
            await _context.MarketplaceTasks
                .CountAsync(t => t.AssignedTeacherId == teacherId && t.Status == TaskStatus.Completed);

        public async Task<double?> GetAverageRatingByTeacherAsync(Guid teacherId)
        {
            var ratings = await _context.MarketplaceRatings
                .Where(r => r.RatedUser == teacherId)
                .Select(r => r.Score)
                .ToListAsync();
            return ratings.Count == 0 ? null : ratings.Average();
        }
    }
}
