using LinaTask.Domain.Models;

namespace LinaTask.Domain.Interfaces
{
    public interface IPermissionRepository
    {
        /// <summary>
        /// Obtiene todos los permisos asignados a un usuario a través de sus roles
        /// </summary>
        Task<IEnumerable<Permission>> GetPermissionsByUserIdAsync(Guid userId);

        /// <summary>
        /// Obtiene todos los permisos de un rol específico
        /// </summary>
        Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(Guid roleId);

        /// <summary>
        /// Obtiene todos los permisos disponibles en el sistema
        /// </summary>
        Task<IEnumerable<Permission>> GetAllPermissionsAsync();

        /// <summary>
        /// Obtiene un permiso por su código
        /// </summary>
        Task<Permission?> GetPermissionByCodeAsync(string code);

        /// <summary>
        /// Verifica si un usuario tiene un permiso específico
        /// </summary>
        Task<bool> UserHasPermissionAsync(Guid userId, string permissionCode);

        /// <summary>
        /// Asigna permisos a un rol
        /// </summary>
        Task AssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds);

        /// <summary>
        /// Remueve permisos de un rol
        /// </summary>
        Task RemovePermissionsFromRoleAsync(Guid roleId, List<Guid> permissionIds);

        Task ReplaceRolePermissionsAsync(Guid roleId, List<Guid> permissionIds);
        Task<IEnumerable<Permission>> GetPermissionsByMenuIdAsync(Guid menuId);

    }
}