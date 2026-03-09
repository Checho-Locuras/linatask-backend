using LinaTask.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services.Interfaces
{
    public interface ITaskOfferService
    {
        Task<IEnumerable<TaskOfferDto>> GetByTaskIdAsync(Guid taskId);
        Task<IEnumerable<TaskOfferDto>> GetByTeacherIdAsync(Guid teacherId);
        Task<TaskOfferDto?> GetByIdAsync(Guid id);
        Task<TaskOfferDto> CreateAsync(CreateTaskOfferDto dto);
        Task<TaskOfferDto> UpdateAsync(Guid id, UpdateTaskOfferDto dto, Guid requestingUserId);
        Task<MarketplaceTaskDto> SelectOfferAsync(Guid taskId, SelectOfferDto dto, Guid studentId);
        Task<bool> WithdrawAsync(Guid offerId, Guid teacherId);
    }
}
