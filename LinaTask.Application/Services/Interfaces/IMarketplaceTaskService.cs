using LinaTask.Domain.DTOs;
using LinaTask.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using TaskStatus = LinaTask.Domain.Enums.TaskStatus;

namespace LinaTask.Application.Services.Interfaces
{
    public interface IMarketplaceTaskService
    {
        Task<IEnumerable<MarketplaceTaskDto>> GetAllAsync(bool onlyOpen = false);
        Task<IEnumerable<MarketplaceTaskDto>> GetByStudentIdAsync(Guid studentId);
        Task<IEnumerable<MarketplaceTaskDto>> GetByTeacherIdAsync(Guid teacherId);
        Task<IEnumerable<MarketplaceTaskDto>> GetByStatusAsync(TaskStatus status);
        Task<IEnumerable<MarketplaceTaskDto>> GetUrgentAsync();
        Task<MarketplaceTaskDto?> GetByIdAsync(Guid id);
        Task<MarketplaceTaskDto> CreateAsync(CreateMarketplaceTaskDto dto, Guid requestingUserId);
        Task<MarketplaceTaskDto> UpdateAsync(Guid id, UpdateMarketplaceTaskDto dto, Guid requestingUserId);
        Task<bool> DeleteAsync(Guid id, Guid requestingUserId);
        Task<SuggestedPriceDto> GetSuggestedPriceAsync(WorkType workType, AcademicLevel level, bool isUrgent, DateTime deadline);
        Task<MarketplaceStatsDto> GetStatsAsync(Guid? userId = null);
    }
}
