// Domain/Interfaces/ISystemParameterRepository.cs
using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinaTask.Domain.Interfaces
{
    public interface ISystemParameterRepository
    {
        Task<IEnumerable<SystemParameter>> GetAllAsync();
        Task<IEnumerable<SystemParameter>> GetActiveAsync();
        Task<SystemParameter?> GetByIdAsync(Guid id);
        Task<SystemParameter?> GetByKeyAsync(string key);
        Task<SystemParameter> CreateAsync(SystemParameter parameter);
        Task<SystemParameter> UpdateAsync(SystemParameter parameter);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsByKeyAsync(string key);
        Task<IEnumerable<SystemParameter>> SearchAsync(string searchTerm);
        Task<SystemParameter?> GetByKeyAndTypeAsync(string key, string dataType);
    }
}