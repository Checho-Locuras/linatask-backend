// Infrastructure/Repositories/TaskAttachmentRepository.cs
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Infrastructure.Repositories
{
    public class TaskAttachmentRepository : ITaskAttachmentRepository
    {
        private readonly LinaTaskDbContext _context;

        public TaskAttachmentRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<TaskAttachment> AddAsync(TaskAttachment attachment)
        {
            _context.TaskAttachments.Add(attachment);
            await _context.SaveChangesAsync();
            return attachment;
        }

        public async Task<IEnumerable<TaskAttachment>> GetByTaskIdAsync(Guid taskId) =>
            await _context.TaskAttachments
                .Where(a => a.TaskId == taskId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

        public async Task<bool> DeleteAsync(Guid id)
        {
            var attachment = await _context.TaskAttachments.FindAsync(id);
            if (attachment is null) return false;
            _context.TaskAttachments.Remove(attachment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}