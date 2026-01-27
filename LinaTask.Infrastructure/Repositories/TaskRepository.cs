using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Infrastructure.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly LinaTaskDbContext _context;

        public TaskRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TaskU>> GetAllAsync()
        {
            return await _context.Tasks
                .Include(t => t.Student)
                .Include(t => t.Offers)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskU>> GetByStudentIdAsync(Guid studentId)
        {
            return await _context.Tasks
                .Include(t => t.Student)
                .Include(t => t.Offers)
                .Where(t => t.StudentId == studentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskU>> GetByStatusAsync(string status)
        {
            return await _context.Tasks
                .Include(t => t.Student)
                .Include(t => t.Offers)
                .Where(t => t.Status == status)
                .ToListAsync();
        }

        public async Task<TaskU?> GetByIdAsync(Guid id)
        {
            return await _context.Tasks
                .Include(t => t.Student)
                .Include(t => t.Offers)
                .ThenInclude(o => o.Teacher)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TaskU> CreateAsync(TaskU task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<TaskU> UpdateAsync(TaskU task)
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var task = await GetByIdAsync(id);
            if (task == null) return false;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Tasks.AnyAsync(t => t.Id == id);
        }
    }
}