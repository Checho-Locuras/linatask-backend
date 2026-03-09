// LinaTask.Domain/Interfaces/IEmailTemplateRepository.cs
using LinaTask.Domain.Models;

namespace LinaTask.Domain.Interfaces
{
    public interface IEmailTemplateRepository
    {
        Task<EmailTemplate?> GetByKeyAsync(string key);
        Task<IEnumerable<EmailTemplate>> GetAllAsync();
        Task<EmailTemplate?> GetByIdAsync(Guid id);
        Task<EmailTemplate> CreateAsync(EmailTemplate template);
        Task<EmailTemplate> UpdateAsync(EmailTemplate template);
        Task<bool> DeleteAsync(Guid id);
    }
}