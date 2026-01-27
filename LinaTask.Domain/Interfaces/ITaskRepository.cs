using LinaTask.Domain.Models;

namespace LinaTask.Domain.Interfaces
{
    public interface ITaskRepository
    {
        Task<IEnumerable<TaskU>> GetAllAsync();
        Task<IEnumerable<TaskU>> GetByStudentIdAsync(Guid studentId);
        Task<IEnumerable<TaskU>> GetByStatusAsync(string status);
        Task<TaskU?> GetByIdAsync(Guid id);
        Task<TaskU> CreateAsync(TaskU task);
        Task<TaskU> UpdateAsync(TaskU task);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}