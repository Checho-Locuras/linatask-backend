// LinaTask.Domain/DTOs/EmailTemplateDto.cs
namespace LinaTask.Domain.DTOs
{
    public record EmailTemplateDto(
        Guid Id, string Key, string Name,
        string? Description, string Subject,
        string HtmlBody, bool IsActive,
        DateTime CreatedAt, DateTime? UpdatedAt);

    public record CreateEmailTemplateDto(
        string Key, string Name,
        string? Description, string Subject,
        string HtmlBody, bool IsActive = true);

    public record UpdateEmailTemplateDto(
        string? Name, string? Description,
        string? Subject, string? HtmlBody,
        bool? IsActive);
}