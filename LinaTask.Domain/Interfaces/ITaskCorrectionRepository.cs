using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Interfaces
{
    public interface ITaskCorrectionRepository
    {
        Task<IEnumerable<TaskCorrectionRequest>> GetByTaskIdAsync(Guid taskId);
        Task<TaskCorrectionRequest?> GetByIdAsync(Guid id);
        Task<TaskCorrectionRequest> CreateAsync(TaskCorrectionRequest correction);
        Task<TaskCorrectionRequest> UpdateAsync(TaskCorrectionRequest correction);
    }
}
