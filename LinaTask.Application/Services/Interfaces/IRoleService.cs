// Application/Services/RoleService.cs
using LinaTask.Domain.DTOs;

namespace LinaTask.Application.Services
{
    public interface IRoleService
    {
        Task<RoleDto> CreateRoleAsync(CreateRoleDto createRoleDto);
        Task<bool> DeleteRoleAsync(Guid id);
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
        Task<RoleDto?> GetRoleByIdAsync(Guid id);
        Task<RoleDto?> GetRoleByNameAsync(string name);
        Task<bool> RoleExistsAsync(Guid id);
        Task<bool> RoleExistsByNameAsync(string name);
        Task<RoleDto> UpdateRoleAsync(Guid id, UpdateRoleDto updateRoleDto);
    }
}