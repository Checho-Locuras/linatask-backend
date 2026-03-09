using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Infrastructure.Repositories
{
    public class TaskCorrectionRepository : ITaskCorrectionRepository
    {
        private readonly LinaTaskDbContext _context;

        public TaskCorrectionRepository(LinaTaskDbContext context) => _context = context;

        public async Task<IEnumerable<TaskCorrectionRequest>> GetByTaskIdAsync(Guid taskId) =>
            await _context.TaskCorrectionRequests
                .Include(c => c.Student)
                .Where(c => c.TaskId == taskId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

        public async Task<TaskCorrectionRequest?> GetByIdAsync(Guid id) =>
            await _context.TaskCorrectionRequests
                .Include(c => c.Student)
                .Include(c => c.Task)
                .FirstOrDefaultAsync(c => c.Id == id);

        public async Task<TaskCorrectionRequest> CreateAsync(TaskCorrectionRequest correction)
        {
            _context.TaskCorrectionRequests.Add(correction);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(correction.Id))!;
        }

        public async Task<TaskCorrectionRequest> UpdateAsync(TaskCorrectionRequest correction)
        {
            correction.UpdatedAt = DateTime.UtcNow;
            _context.TaskCorrectionRequests.Update(correction);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(correction.Id))!;
        }
    }
}
