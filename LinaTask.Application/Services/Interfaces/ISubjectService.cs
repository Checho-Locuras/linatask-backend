using LinaTask.Application.DTOs;

namespace LinaTask.Application.Services.Interfaces
{
    public interface ISubjectService
    {
        Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync();
        Task<IEnumerable<SubjectDto>> GetActiveSubjectsAsync();
        Task<IEnumerable<SubjectDto>> GetSubjectsByCategoryAsync(string category);
        Task<SubjectDto?> GetSubjectByIdAsync(Guid id);
        Task<SubjectDto> CreateSubjectAsync(CreateSubjectDto createSubjectDto);
        Task<SubjectDto> UpdateSubjectAsync(Guid id, UpdateSubjectDto updateSubjectDto);
        Task<bool> DeleteSubjectAsync(Guid id);
    }
}
