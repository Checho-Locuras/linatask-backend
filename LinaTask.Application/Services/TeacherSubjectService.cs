using LinaTask.Application.DTOs;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.Repositories;

namespace LinaTask.Application.Services
{
    public class TeacherSubjectService : ITeacherSubjectService
    {
        private readonly ITeacherSubjectRepository _teacherSubjectRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISubjectRepository _subjectRepository;

        public TeacherSubjectService(
            ITeacherSubjectRepository teacherSubjectRepository,
            IUserRepository userRepository,
            ISubjectRepository subjectRepository)
        {
            _teacherSubjectRepository = teacherSubjectRepository;
            _userRepository = userRepository;
            _subjectRepository = subjectRepository;
        }

        public async Task<IEnumerable<TeacherSubjectDto>> GetAllTeacherSubjectsAsync()
        {
            var teacherSubjects = await _teacherSubjectRepository.GetAllAsync();
            return teacherSubjects.Select(MapToDto);
        }

        public async Task<IEnumerable<TeacherSubjectDto>> GetByTeacherAsync(Guid teacherId)
        {
            var teacherSubjects = await _teacherSubjectRepository.GetByTeacherIdAsync(teacherId);
            return teacherSubjects.Select(MapToDto);
        }

        public async Task<IEnumerable<TeacherSubjectDto>> GetBySubjectAsync(Guid subjectId)
        {
            var teacherSubjects = await _teacherSubjectRepository.GetBySubjectIdAsync(subjectId);
            return teacherSubjects.Select(MapToDto);
        }

        public async Task<TeacherSubjectDto?> GetByIdAsync(Guid id)
        {
            var teacherSubject = await _teacherSubjectRepository.GetByIdAsync(id);
            return teacherSubject == null ? null : MapToDto(teacherSubject);
        }

        public async Task<TeacherSubjectDto> CreateTeacherSubjectAsync(CreateTeacherSubjectDto createDto)
        {
            // Validar que el profesor existe y es profesor
            var teacher = await _userRepository.GetByIdAsync(createDto.TeacherId);
            if (teacher == null || !teacher.UserRoles.Any(ur => ur.Role.Name == "teacher"))
                throw new InvalidOperationException("Invalid teacher ID");

            // Validar que la materia existe
            var subject = await _subjectRepository.GetByIdAsync(createDto.SubjectId);
            if (subject == null)
                throw new InvalidOperationException("Subject not found");

            // Validar que no exista ya esta relación
            if (await _teacherSubjectRepository.ExistsAsync(createDto.TeacherId, createDto.SubjectId))
                throw new InvalidOperationException("Teacher already teaches this subject");

            var teacherSubject = new TeacherSubject
            {
                Id = Guid.NewGuid(),
                TeacherId = createDto.TeacherId,
                SubjectId = createDto.SubjectId,
                ExperienceYears = createDto.ExperienceYears,
                CertificationUrl = createDto.CertificationUrl,
                IsPrimary = createDto.IsPrimary,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _teacherSubjectRepository.CreateAsync(teacherSubject);
            return MapToDto(created);
        }

        public async Task<TeacherSubjectDto> UpdateTeacherSubjectAsync(Guid id, UpdateTeacherSubjectDto updateDto)
        {
            var teacherSubject = await _teacherSubjectRepository.GetByIdAsync(id);
            if (teacherSubject == null)
                throw new KeyNotFoundException($"TeacherSubject with ID {id} not found");

            if (updateDto.ExperienceYears.HasValue)
                teacherSubject.ExperienceYears = updateDto.ExperienceYears;

            if (updateDto.CertificationUrl != null)
                teacherSubject.CertificationUrl = updateDto.CertificationUrl;

            if (updateDto.IsPrimary.HasValue)
                teacherSubject.IsPrimary = updateDto.IsPrimary.Value;

            teacherSubject.CreatedAt = teacherSubject.CreatedAt.ToUniversalTime();

            var updated = await _teacherSubjectRepository.UpdateAsync(teacherSubject);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteTeacherSubjectAsync(Guid id)
        {
            return await _teacherSubjectRepository.DeleteAsync(id);
        }

        private static TeacherSubjectDto MapToDto(TeacherSubject ts)
        {
            return new TeacherSubjectDto
            {
                Id = ts.Id,
                TeacherId = ts.TeacherId,
                TeacherName = ts.Teacher?.Name ?? string.Empty,
                SubjectId = ts.SubjectId,
                SubjectName = ts.Subject?.Name ?? string.Empty,
                ExperienceYears = ts.ExperienceYears,
                CertificationUrl = ts.CertificationUrl,
                IsPrimary = ts.IsPrimary,
                CreatedAt = ts.CreatedAt
            };
        }
    }
}
