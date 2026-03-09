// LinaTask.Application/Services/Interfaces/IEmailTemplateService.cs
using LinaTask.Domain.DTOs;

namespace LinaTask.Application.Services.Interfaces
{
    public interface IEmailTemplateService
    {
        Task<EmailTemplateDto?> GetByKeyAsync(string key);
        Task<IEnumerable<EmailTemplateDto>> GetAllAsync();
        Task<EmailTemplateDto?> GetByIdAsync(Guid id);
        Task<EmailTemplateDto> CreateAsync(CreateEmailTemplateDto dto);
        Task<EmailTemplateDto> UpdateAsync(Guid id, UpdateEmailTemplateDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}