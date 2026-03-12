using LinaTask.Domain.Enums;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Infrastructure.Repositories
{
    public class MarketplacePaymentRepository : IMarketplacePaymentRepository
    {
        private readonly LinaTaskDbContext _context;

        public MarketplacePaymentRepository(LinaTaskDbContext context) => _context = context;

        private IQueryable<MarketplacePayment> BaseQuery() =>
            _context.MarketplacePayments
                .Include(p => p.Task)
                .Include(p => p.Student)
                .Include(p => p.Teacher);

        public async Task<MarketplacePayment?> GetByTaskIdAsync(Guid taskId) =>
            await BaseQuery().FirstOrDefaultAsync(p => p.TaskId == taskId);

        public async Task<MarketplacePayment?> GetByIdAsync(Guid id) =>
            await BaseQuery().FirstOrDefaultAsync(p => p.Id == id);

        public async Task<IEnumerable<MarketplacePayment>> GetPendingAutoReleaseAsync(DateTime upTo) =>
            await BaseQuery()
                .Where(p => p.Status == PaymentStatus.Held
                         && p.AutoReleaseAt.HasValue
                         && p.AutoReleaseAt.Value <= upTo)
                .ToListAsync();

        public async Task<MarketplacePayment> CreateAsync(MarketplacePayment payment)
        {
            _context.MarketplacePayments.Add(payment);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(payment.Id))!;
        }

        public async Task<MarketplacePayment> UpdateAsync(MarketplacePayment payment)
        {
            payment.UpdatedAt = DateTime.UtcNow;
            _context.MarketplacePayments.Update(payment);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(payment.Id))!;
        }

        public async Task<MarketplacePayment?> GetByExternalPaymentIdAsync(string externalPaymentId) =>
            await BaseQuery()
                .FirstOrDefaultAsync(p => p.ExternalPaymentId == externalPaymentId);

        public async Task<MarketplacePayment?> GetBySessionIdAsync(Guid sessionId) =>
            await BaseQuery().FirstOrDefaultAsync(p => p.SessionId == sessionId);
    }
}
