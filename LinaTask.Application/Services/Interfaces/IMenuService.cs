using LinaTask.Domain.DTOs;

namespace LinaTask.Application.Services.Interfaces
{
    public interface IMenuService
    {
        Task<IEnumerable<MenuDto>> GetAllAsync();
        Task<IEnumerable<MenuDto>> GetVisibleMenusAsync();
        Task<MenuWithChildrenDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<MenuWithChildrenDto>> GetMenuHierarchyAsync();
        Task<MenuDto> CreateAsync(CreateMenuDto dto);
        Task<MenuDto?> UpdateAsync(Guid id, UpdateMenuDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task AssignPermissionsAsync(Guid menuId, List<Guid> permissionIds);
        Task<IEnumerable<MenuWithChildrenDto>> GetMenusByRoleAsync(Guid roleId);
        Task<IEnumerable<MenuWithChildrenDto>> GetMenusByRoleNameAsync(string roleName);

    }
}