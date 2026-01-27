using LinaTask.Domain.Models;

namespace LinaTask.Domain.Interfaces
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<Payment>> GetAllAsync();
        Task<IEnumerable<Payment>> GetByStudentIdAsync(Guid studentId);
        Task<IEnumerable<Payment>> GetByTaskIdAsync(Guid taskId);
        Task<IEnumerable<Payment>> GetByStatusAsync(string status);
        Task<Payment?> GetByIdAsync(Guid id);
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment> UpdateAsync(Payment payment);
        Task<bool> DeleteAsync(Guid id);
        Task<decimal> GetTotalByStudentAsync(Guid studentId);
        Task<decimal> GetPlatformFeeTotalAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}