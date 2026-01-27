using LinaTask.Domain.Models;

namespace LinaTask.Domain.Interfaces
{
    public interface IOfferRepository
    {
        Task<IEnumerable<Offer>> GetAllAsync();
        Task<IEnumerable<Offer>> GetByTaskIdAsync(Guid taskId);
        Task<IEnumerable<Offer>> GetByTeacherIdAsync(Guid teacherId);
        Task<Offer?> GetByIdAsync(Guid id);
        Task<Offer> CreateAsync(Offer offer);
        Task<Offer> UpdateAsync(Offer offer);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid taskId, Guid teacherId);
    }
}