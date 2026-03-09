using LinaTask.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services.Interfaces
{
    public interface ITaskCorrectionService
    {
        Task<IEnumerable<TaskCorrectionRequestDto>> GetByTaskIdAsync(Guid taskId);
        Task<TaskCorrectionRequestDto> CreateAsync(CreateCorrectionRequestDto dto, Guid studentId);
        Task<TaskCorrectionRequestDto> ResolveAsync(Guid correctionId, Guid teacherId);
    }
}
