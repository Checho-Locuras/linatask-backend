using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinaTask.Infrastructure.Repositories
{
    public class MenuRepository : IMenuRepository
    {
        private readonly LinaTaskDbContext _context;

        public MenuRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Menu>> GetAllAsync()
        {
            return await _context.Menus
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Menu>> GetVisibleMenusAsync()
        {
            return await _context.Menus
                .Where(m => m.IsVisible)
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Menu?> GetByIdAsync(Guid id)
        {
            return await _context.Menus
                .Include(m => m.MenuPermissions)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<Menu>> GetByParentIdAsync(Guid? parentId)
        {
            return await _context.Menus
                .Where(m => m.ParentId == parentId)
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Menu>> GetMenuHierarchyAsync()
        {
            return await _context.Menus
                .Include(m => m.Children.OrderBy(c => c.Order).ThenBy(c => c.Name))
                    .ThenInclude(c => c.Children)
                .Where(m => m.ParentId == null)
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> HasChildrenAsync(Guid id)
        {
            return await _context.Menus
                .AnyAsync(m => m.ParentId == id);
        }

        public async Task<Menu> CreateAsync(Menu menu)
        {
            menu.Id = Guid.NewGuid();
            menu.CreatedAt = DateTime.UtcNow;
            menu.UpdatedAt = DateTime.UtcNow;

            await _context.Menus.AddAsync(menu);
            await _context.SaveChangesAsync();
            return menu;
        }

        public async Task<Menu> UpdateAsync(Menu menu)
        {
            menu.UpdatedAt = DateTime.UtcNow;
            _context.Menus.Update(menu);
            await _context.SaveChangesAsync();
            return menu;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null)
                return false;

            _context.Menus.Remove(menu);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AssignPermissionsAsync(Guid menuId, IEnumerable<Guid> permissionIds)
        {
            var existingPermissions = await _context.MenuPermissions
                .Where(mp => mp.MenuId == menuId)
                .ToListAsync();

            _context.MenuPermissions.RemoveRange(existingPermissions);

            var newPermissions = permissionIds.Select(permissionId => new MenuPermission
            {
                MenuId = menuId,
                PermissionId = permissionId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.MenuPermissions.AddRangeAsync(newPermissions);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Guid>> GetPermissionIdsAsync(Guid menuId)
        {
            return await _context.MenuPermissions
                .Where(mp => mp.MenuId == menuId)
                .Select(mp => mp.PermissionId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Menu>> GetMenusByRoleIdAsync(Guid roleId)
        {
            // 1. Obtener todos los módulos a los que el rol tiene acceso
            var roleModules = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission.Module)
                .Distinct()
                .ToListAsync();

            // 2. Obtener todos los menús visibles
            var allMenus = await _context.Menus
                .Where(m => m.IsVisible)
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Name)
                .AsNoTracking()
                .ToListAsync();

            // 3. Filtrar menús según módulos
            var accessibleMenus = allMenus.Where(menu =>
            {
                // Siempre mostrar Dashboard y Settings
                if (menu.Route == "/admin/dashboard" || menu.Route == "/admin/settings")
                    return true;

                // Extraer el nombre del módulo de la ruta
                var routePart = menu.Route.Replace("/admin/", "").ToUpper();

                // Remover 'S' final para singular/plural
                var routeModule = routePart.TrimEnd('S');

                // Verificar si el módulo del menú está en los permisos del rol
                return roleModules.Any(module =>
                    module.Equals(routeModule, StringComparison.OrdinalIgnoreCase) ||
                    module.StartsWith(routeModule, StringComparison.OrdinalIgnoreCase) ||
                    routeModule.StartsWith(module.Replace("_", ""), StringComparison.OrdinalIgnoreCase)
                );
            }).ToList();

            return accessibleMenus;
        }


        public async Task<IEnumerable<Menu>> GetMenusByRoleNameAsync(string roleName)
        {
            return await _context.Menus
                .Where(m => m.IsVisible &&
                            m.MenuPermissions.Any(mp =>
                                mp.Permission.RolePermissions
                                    .Any(rp => rp.Role.Name == roleName)))
                .Include(m => m.Children)
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Name)
                .AsNoTracking()
                .ToListAsync();
        }


    }
}