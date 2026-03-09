// LinaTask.Infrastructure/Repositories/EmailTemplateRepository.cs
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Infrastructure.Repositories
{
    public class EmailTemplateRepository : IEmailTemplateRepository
    {
        private readonly LinaTaskDbContext _context;

        public EmailTemplateRepository(LinaTaskDbContext context) => _context = context;

        public Task<EmailTemplate?> GetByKeyAsync(string key) =>
            _context.EmailTemplates.FirstOrDefaultAsync(t => t.Key == key && t.IsActive);

        public Task<IEnumerable<EmailTemplate>> GetAllAsync() =>
            Task.FromResult<IEnumerable<EmailTemplate>>(
                _context.EmailTemplates.OrderBy(t => t.Key).AsEnumerable());

        public Task<EmailTemplate?> GetByIdAsync(Guid id) =>
            _context.EmailTemplates.FindAsync(id).AsTask()!;

        public async Task<EmailTemplate> CreateAsync(EmailTemplate template)
        {
            _context.EmailTemplates.Add(template);
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task<EmailTemplate> UpdateAsync(EmailTemplate template)
        {
            template.UpdatedAt = DateTime.UtcNow;
            _context.EmailTemplates.Update(template);
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var t = await _context.EmailTemplates.FindAsync(id);
            if (t is null) return false;
            _context.EmailTemplates.Remove(t);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}