using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinaTask.Domain.Interfaces
{
    public interface IUserRoleRepository
    {
        Task<bool> UserHasRoleAsync(Guid userId, string roleName);
        Task AssignRoleAsync(Guid userId, string roleName);
        Task RemoveRoleAsync(Guid userId, string roleName);
        Task<IEnumerable<string>> GetRolesByUserAsync(Guid userId);
    }
}
