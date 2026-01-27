using LinaTask.Domain.Models;

namespace LinaTask.Domain.Interfaces
{
    public interface ISubjectRepository
    {
        Task<IEnumerable<Subject>> GetAllAsync();
        Task<IEnumerable<Subject>> GetActivesAsync();
        Task<IEnumerable<Subject>> GetByCategoryAsync(string category);
        Task<Subject?> GetByIdAsync(Guid id);
        Task<Subject?> GetByNameAsync(string name);
        Task<Subject> CreateAsync(Subject subject);
        Task<Subject> UpdateAsync(Subject subject);
        Task<bool> DeleteAsync(Guid id);
    }
}