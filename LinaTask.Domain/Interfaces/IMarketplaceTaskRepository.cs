using LinaTask.Domain.Enums;
using LinaTask.Domain.Models;
using TaskStatus = LinaTask.Domain.Enums.TaskStatus;

namespace LinaTask.Domain.Interfaces
{
    public interface IMarketplaceTaskRepository
    {
        Task<IEnumerable<MarketplaceTask>> GetAllAsync(bool onlyOpen = false);
        Task<IEnumerable<MarketplaceTask>> GetByStudentIdAsync(Guid studentId);
        Task<IEnumerable<MarketplaceTask>> GetByTeacherIdAsync(Guid teacherId);
        Task<IEnumerable<MarketplaceTask>> GetByStatusAsync(TaskStatus status);
        Task<IEnumerable<MarketplaceTask>> GetUrgentAsync();
        Task<MarketplaceTask?> GetByIdAsync(Guid id);
        Task<MarketplaceTask> CreateAsync(MarketplaceTask task);
        Task<MarketplaceTask> UpdateAsync(MarketplaceTask task);
        Task<bool> DeleteAsync(Guid id);

        // Stats para el perfil de docente
        Task<int> GetCompletedCountByTeacherAsync(Guid teacherId);
        Task<double?> GetAverageRatingByTeacherAsync(Guid teacherId);
    }
}
