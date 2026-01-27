using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly LinaTaskDbContext _context;

        public PaymentRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Payment>> GetAllAsync()
        {
            return await _context.Payments
                .Include(p => p.TaskU)
                .Include(p => p.Student)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetByStudentIdAsync(Guid studentId)
        {
            return await _context.Payments
                .Include(p => p.TaskU)
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetByTaskIdAsync(Guid taskId)
        {
            return await _context.Payments
                .Include(p => p.Student)
                .Where(p => p.TaskId == taskId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetByStatusAsync(string status)
        {
            return await _context.Payments
                .Include(p => p.TaskU)
                .Include(p => p.Student)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Payment?> GetByIdAsync(Guid id)
        {
            return await _context.Payments
                .Include(p => p.TaskU)
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment> UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var payment = await GetByIdAsync(id);
            if (payment == null) return false;

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetTotalByStudentAsync(Guid studentId)
        {
            return await _context.Payments
                .Where(p => p.StudentId == studentId && p.Status == "completed")
                .SumAsync(p => p.Amount);
        }

        public async Task<decimal> GetPlatformFeeTotalAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Payments
                .Where(p => p.Status == "completed");

            if (startDate.HasValue)
                query = query.Where(p => p.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.CreatedAt <= endDate.Value);

            return await query.SumAsync(p => p.PlatformFee);
        }
    }
}