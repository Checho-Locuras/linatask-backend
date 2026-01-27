using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Interfaces
{
    public interface ITeacherSubjectRepository
    {
        Task<IEnumerable<TeacherSubject>> GetAllAsync();
        Task<IEnumerable<TeacherSubject>> GetByTeacherIdAsync(Guid teacherId);
        Task<IEnumerable<TeacherSubject>> GetBySubjectIdAsync(Guid subjectId);
        Task<TeacherSubject?> GetByIdAsync(Guid id);
        Task<TeacherSubject> CreateAsync(TeacherSubject teacherSubject);
        Task<TeacherSubject> UpdateAsync(TeacherSubject teacherSubject);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid teacherId, Guid subjectId);
    }
}
