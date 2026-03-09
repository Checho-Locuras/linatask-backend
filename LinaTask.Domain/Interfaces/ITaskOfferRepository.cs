using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Interfaces
{
    public interface ITaskOfferRepository
    {
        Task<IEnumerable<TaskOffer>> GetByTaskIdAsync(Guid taskId);
        Task<IEnumerable<TaskOffer>> GetByTeacherIdAsync(Guid teacherId);
        Task<TaskOffer?> GetByIdAsync(Guid id);
        Task<TaskOffer?> GetByTaskAndTeacherAsync(Guid taskId, Guid teacherId);
        Task<int> CountByTaskIdAsync(Guid taskId);
        Task<TaskOffer> CreateAsync(TaskOffer offer);
        Task<TaskOffer> UpdateAsync(TaskOffer offer);
        Task<bool> DeleteAsync(Guid id);
    }
}
