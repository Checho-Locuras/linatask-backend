using LinaTask.Domain.DTOs;
using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinaTask.Domain.Interfaces
{
    public interface IMenuRepository
    {
        Task<IEnumerable<Menu>> GetAllAsync();
        Task<IEnumerable<Menu>> GetVisibleMenusAsync();
        Task<Menu?> GetByIdAsync(Guid id);
        Task<IEnumerable<Menu>> GetByParentIdAsync(Guid? parentId);
        Task<IEnumerable<Menu>> GetMenuHierarchyAsync();
        Task<bool> HasChildrenAsync(Guid id);
        Task<Menu> CreateAsync(Menu menu);
        Task<Menu> UpdateAsync(Menu menu);
        Task<bool> DeleteAsync(Guid id);
        Task AssignPermissionsAsync(Guid menuId, IEnumerable<Guid> permissionIds);
        Task<IEnumerable<Guid>> GetPermissionIdsAsync(Guid menuId);
        Task<IEnumerable<Menu>> GetMenusByRoleIdAsync(Guid roleId);
        Task<IEnumerable<Menu>> GetMenusByRoleNameAsync(string roleName);

    }
}
