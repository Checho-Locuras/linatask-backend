using LinaTask.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services.Interfaces
{
    public interface ITeacherSubjectService
    {
        Task<IEnumerable<TeacherSubjectDto>> GetAllTeacherSubjectsAsync();
        Task<IEnumerable<TeacherSubjectDto>> GetByTeacherAsync(Guid teacherId);
        Task<IEnumerable<TeacherSubjectDto>> GetBySubjectAsync(Guid subjectId);
        Task<TeacherSubjectDto?> GetByIdAsync(Guid id);
        Task<TeacherSubjectDto> CreateTeacherSubjectAsync(CreateTeacherSubjectDto createDto);
        Task<TeacherSubjectDto> UpdateTeacherSubjectAsync(Guid id, UpdateTeacherSubjectDto updateDto);
        Task<bool> DeleteTeacherSubjectAsync(Guid id);
    }
}
