using LinaTask.Application.DTOs;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;

namespace LinaTask.Application.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly ISubjectRepository _subjectRepository;

        public SubjectService(ISubjectRepository subjectRepository)
        {
            _subjectRepository = subjectRepository;
        }

        public async Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync()
        {
            var subjects = await _subjectRepository.GetAllAsync();
            return subjects.Select(MapToDto);
        }

        public async Task<IEnumerable<SubjectDto>> GetActiveSubjectsAsync()
        {
            var subjects = await _subjectRepository.GetActivesAsync();
            return subjects.Select(MapToDto);
        }

        public async Task<IEnumerable<SubjectDto>> GetSubjectsByCategoryAsync(string category)
        {
            var subjects = await _subjectRepository.GetByCategoryAsync(category);
            return subjects.Select(MapToDto);
        }

        public async Task<SubjectDto?> GetSubjectByIdAsync(Guid id)
        {
            var subject = await _subjectRepository.GetByIdAsync(id);
            return subject == null ? null : MapToDto(subject);
        }

        public async Task<SubjectDto> CreateSubjectAsync(CreateSubjectDto createSubjectDto)
        {
            // Validar que no exista una materia con el mismo nombre
            var existing = await _subjectRepository.GetByNameAsync(createSubjectDto.Name);
            if (existing != null)
                throw new InvalidOperationException($"Subject '{createSubjectDto.Name}' already exists");

            var subject = new Subject
            {
                Id = Guid.NewGuid(),
                Name = createSubjectDto.Name,
                Description = createSubjectDto.Description,
                Category = createSubjectDto.Category,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createdSubject = await _subjectRepository.CreateAsync(subject);
            return MapToDto(createdSubject);
        }

        public async Task<SubjectDto> UpdateSubjectAsync(Guid id, UpdateSubjectDto updateSubjectDto)
        {
            var subject = await _subjectRepository.GetByIdAsync(id);
            if (subject == null)
                throw new KeyNotFoundException($"Subject with ID {id} not found");

            if (!string.IsNullOrEmpty(updateSubjectDto.Name))
            {
                var existing = await _subjectRepository.GetByNameAsync(updateSubjectDto.Name);
                if (existing != null && existing.Id != id)
                    throw new InvalidOperationException($"Subject '{updateSubjectDto.Name}' already exists");

                subject.Name = updateSubjectDto.Name;
            }

            if (updateSubjectDto.Description != null)
                subject.Description = updateSubjectDto.Description;

            if (updateSubjectDto.Category != null)
                subject.Category = updateSubjectDto.Category;

            if (updateSubjectDto.IsActive.HasValue)
                subject.IsActive = updateSubjectDto.IsActive.Value;

            var updatedSubject = await _subjectRepository.UpdateAsync(subject);
            return MapToDto(updatedSubject);
        }

        public async Task<bool> DeleteSubjectAsync(Guid id)
        {
            return await _subjectRepository.DeleteAsync(id);
        }

        private static SubjectDto MapToDto(Subject subject)
        {
            return new SubjectDto
            {
                Id = subject.Id,
                Name = subject.Name,
                Description = subject.Description,
                Category = subject.Category,
                IsActive = subject.IsActive,
                CreatedAt = subject.CreatedAt,
                TeachersCount = subject.TeacherSubjects?.Count ?? 0
            };
        }
    }
}
