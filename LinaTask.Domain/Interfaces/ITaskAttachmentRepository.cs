using LinaTask.Domain.Models;

namespace LinaTask.Domain.Interfaces
{
    public interface ITaskAttachmentRepository
    {
        Task<TaskAttachment> AddAsync(TaskAttachment attachment);
        Task<IEnumerable<TaskAttachment>> GetByTaskIdAsync(Guid taskId);
        Task<bool> DeleteAsync(Guid id);
    }
}
