// Application/Services/SystemParameterService.cs
using LinaTask.Domain.DTOs;

namespace LinaTask.Application.Services.Interfaces
{
    public interface ISystemParameterService
    {
        Task<SystemParameterDto> CreateParameterAsync(CreateSystemParameterDto createDto);
        Task<bool> DeleteParameterAsync(Guid id);
        Task<bool> DeleteParameterByKeyAsync(string key);
        Task<IEnumerable<SystemParameterDto>> GetActiveParametersAsync();
        Task<IEnumerable<SystemParameterDto>> GetAllParametersAsync();
        Task<SystemParameterDto?> GetParameterByIdAsync(Guid id);
        Task<SystemParameterDto?> GetParameterByKeyAsync(string key);
        Task<object?> GetParameterValueAsync(string key);
        Task<IEnumerable<SystemParameterDto>> SearchParametersAsync(SystemParameterSearchDto searchDto);
        Task<SystemParameterDto> UpdateParameterAsync(Guid id, UpdateSystemParameterDto updateDto);
        Task<SystemParameterDto> UpdateParameterByKeyAsync(string key, UpdateSystemParameterDto updateDto);
    }
}