using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;

namespace LinaTask.Application.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IEmailTemplateRepository _repo;

        public EmailTemplateService(IEmailTemplateRepository repo) => _repo = repo;

        public async Task<EmailTemplateDto?> GetByKeyAsync(string key)
        {
            var t = await _repo.GetByKeyAsync(key);
            return t is null ? null : Map(t);
        }

        public async Task<IEnumerable<EmailTemplateDto>> GetAllAsync() =>
            (await _repo.GetAllAsync()).Select(Map);

        public async Task<EmailTemplateDto?> GetByIdAsync(Guid id)
        {
            var t = await _repo.GetByIdAsync(id);
            return t is null ? null : Map(t);
        }

        public async Task<EmailTemplateDto> CreateAsync(CreateEmailTemplateDto dto)
        {
            var entity = new EmailTemplate
            {
                Key = dto.Key,
                Name = dto.Name,
                Description = dto.Description,
                Subject = dto.Subject,
                HtmlBody = dto.HtmlBody,
                IsActive = dto.IsActive
            };
            return Map(await _repo.CreateAsync(entity));
        }

        public async Task<EmailTemplateDto> UpdateAsync(Guid id, UpdateEmailTemplateDto dto)
        {
            var entity = await _repo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Template {id} not found");

            if (dto.Name is not null) entity.Name = dto.Name;
            if (dto.Description is not null) entity.Description = dto.Description;
            if (dto.Subject is not null) entity.Subject = dto.Subject;
            if (dto.HtmlBody is not null) entity.HtmlBody = dto.HtmlBody;
            if (dto.IsActive is not null) entity.IsActive = dto.IsActive.Value;

            return Map(await _repo.UpdateAsync(entity));
        }

        public Task<bool> DeleteAsync(Guid id) => _repo.DeleteAsync(id);

        private static EmailTemplateDto Map(EmailTemplate t) =>
            new(t.Id, t.Key, t.Name, t.Description,
                t.Subject, t.HtmlBody, t.IsActive,
                t.CreatedAt, t.UpdatedAt);
    }
}