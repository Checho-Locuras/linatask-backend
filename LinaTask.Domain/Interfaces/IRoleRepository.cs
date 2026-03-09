using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinaTask.Domain.Interfaces
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);
        Task<Role?> GetByNameAsync(string name);
        Task<IEnumerable<Role>> GetAllAsync();
        Task<Role> CreateAsync(Role role);
        Task<Role> UpdateAsync(Role role);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsByNameAsync(string name);
    }
}