using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Infrastructure.Repositories
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly LinaTaskDbContext _context;

        public PermissionRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Permission>> GetPermissionsByUserIdAsync(Guid userId)
        {
            try {
                // Obtener permisos únicos del usuario a través de todos sus roles
                var permissions = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission)
                    .Distinct()
                    .AsNoTracking()
                    .ToListAsync();

                return permissions;
            }catch (Exception e)
            {

            }
            return null;
            
        }

        public async Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(Guid roleId)
        {
            return await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
        {
            return await _context.Permissions
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Code)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Permission?> GetPermissionByCodeAsync(string code)
        {
            return await _context.Permissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == code);
        }

        public async Task<bool> UserHasPermissionAsync(Guid userId, string permissionCode)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .AnyAsync(rp => rp.Permission.Code == permissionCode);
        }

        public async Task AssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds)
        {
            // Verificar que el rol existe
            var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
                throw new InvalidOperationException($"Rol con ID {roleId} no encontrado");

            // Obtener permisos actuales del rol
            var existingPermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            // Filtrar solo los permisos nuevos
            var newPermissionIds = permissionIds
                .Except(existingPermissions)
                .ToList();

            // Crear las relaciones nuevas
            var rolePermissions = newPermissionIds.Select(permissionId => new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            }).ToList();

            await _context.RolePermissions.AddRangeAsync(rolePermissions);
            await _context.SaveChangesAsync();
        }

        public async Task RemovePermissionsFromRoleAsync(Guid roleId, List<Guid> permissionIds)
        {
            var rolePermissionsToRemove = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId && permissionIds.Contains(rp.PermissionId))
                .ToListAsync();

            _context.RolePermissions.RemoveRange(rolePermissionsToRemove);
            await _context.SaveChangesAsync();
        }

        public async Task ReplaceRolePermissionsAsync(Guid roleId, List<Guid> permissionIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1️⃣ Eliminar todos los permisos actuales del rol
                var existing = _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId);

                _context.RolePermissions.RemoveRange(existing);
                await _context.SaveChangesAsync();

                // 2️⃣ Insertar los nuevos
                var newRolePermissions = permissionIds
                    .Distinct()
                    .Select(permissionId => new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId
                    });

                await _context.RolePermissions.AddRangeAsync(newRolePermissions);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<Permission>> GetPermissionsByMenuIdAsync(Guid menuId)
        {
            var menuExists = await _context.Menus.AnyAsync(m => m.Id == menuId);
            if (!menuExists)
            {
                return Enumerable.Empty<Permission>();
            }else
            {
                return await _context.MenuPermissions
                    .Where(mp => mp.MenuId == menuId)
                    .Select(mp => mp.Permission)
                    .AsNoTracking()
                    .ToListAsync();
            }   
        }
    }
}