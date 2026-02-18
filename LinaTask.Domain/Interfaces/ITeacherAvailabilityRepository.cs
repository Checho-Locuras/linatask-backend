using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Interfaces
{
    public interface ITeacherAvailabilityRepository
    {
        Task<IEnumerable<TeacherAvailability>> GetByTeacherIdAsync(Guid teacherId);
        Task<TeacherAvailability?> GetByIdAsync(Guid id);
        Task<TeacherAvailability> CreateAsync(TeacherAvailability availability);
        Task<TeacherAvailability> UpdateAsync(TeacherAvailability availability);
        Task<bool> DeleteAsync(Guid id);
        Task DeleteByTeacherIdAsync(Guid teacherId);
        Task BulkInsertAsync(IEnumerable<TeacherAvailability> availabilities);
    }
}
