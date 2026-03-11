using LinaTask.Domain.DTOs;
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

        public async Task<IEnumerable<MenuWithChildrenDto>> GetMenusByRoleIdAsync(Guid roleId)
        {
            // 1️⃣ Obtener permisos del rol
            var rolePermissionIds = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            if (!rolePermissionIds.Any())
                return Enumerable.Empty<MenuWithChildrenDto>();


            // 2️⃣ Obtener todos los menús visibles con sus permisos
            var allMenus = await _context.Menus
                .Include(m => m.MenuPermissions)
                .Where(m => m.IsVisible)
                .OrderBy(m => m.Order)
                .AsNoTracking()
                .ToListAsync();


            // 3️⃣ Filtrar menús accesibles
            var accessibleMenus = allMenus
                .Where(menu =>
                    !menu.MenuPermissions.Any() || // menú público
                    menu.MenuPermissions.Any(mp => rolePermissionIds.Contains(mp.PermissionId))
                )
                .ToList();


            // 4️⃣ Agregar padres necesarios
            var menuDict = allMenus.ToDictionary(m => m.Id);

            var result = new HashSet<Menu>(accessibleMenus);

            foreach (var menu in accessibleMenus)
            {
                var parentId = menu.ParentId;

                while (parentId != null && menuDict.ContainsKey(parentId.Value))
                {
                    var parent = menuDict[parentId.Value];
                    result.Add(parent);
                    parentId = parent.ParentId;
                }
            }

            var finalMenus = result
                .OrderBy(m => m.Order)
                .ToList();


            // 5️⃣ Construir árbol
            return BuildMenuTree(finalMenus);
        }

        private List<MenuWithChildrenDto> BuildMenuTree(List<Menu> menus)
        {
            var lookup = menus.ToLookup(m => m.ParentId);

            List<MenuWithChildrenDto> Build(Guid? parentId)
            {
                return lookup[parentId]
                    .OrderBy(m => m.Order)
                    .Select(m => new MenuWithChildrenDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Icon = m.Icon,
                        Route = m.Route,
                        ParentId = m.ParentId,
                        Order = m.Order,
                        IsVisible = m.IsVisible,
                        PermissionIds = m.MenuPermissions
                            .Select(mp => mp.PermissionId)
                            .ToList(),
                        Children = Build(m.Id)
                    })
                    .ToList();
            }

            return Build(null);
        }

        public async Task<IEnumerable<MenuWithChildrenDto>> GetMenusByRoleNameAsync(string roleName)
        {
            // 1️⃣ Obtener el roleId
            var roleId = await _context.Roles
                .Where(r => r.Name == roleName)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (roleId == Guid.Empty)
                return Enumerable.Empty<MenuWithChildrenDto>();


            // 2️⃣ Obtener permisos del rol
            var rolePermissionIds = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            if (!rolePermissionIds.Any())
                return Enumerable.Empty<MenuWithChildrenDto>();


            // 3️⃣ Obtener todos los menús visibles
            var allMenus = await _context.Menus
                .Include(m => m.MenuPermissions)
                .Where(m => m.IsVisible)
                .OrderBy(m => m.Order)
                .AsNoTracking()
                .ToListAsync();


            // 4️⃣ Filtrar menús accesibles
            var accessibleMenus = allMenus
                .Where(menu =>
                    !menu.MenuPermissions.Any() ||
                    menu.MenuPermissions.Any(mp => rolePermissionIds.Contains(mp.PermissionId))
                )
                .ToList();


            // 5️⃣ Agregar padres necesarios
            var menuDict = allMenus.ToDictionary(m => m.Id);

            var result = new HashSet<Menu>(accessibleMenus);

            foreach (var menu in accessibleMenus)
            {
                var parentId = menu.ParentId;

                while (parentId != null && menuDict.ContainsKey(parentId.Value))
                {
                    var parent = menuDict[parentId.Value];
                    result.Add(parent);
                    parentId = parent.ParentId;
                }
            }

            var finalMenus = result
                .OrderBy(m => m.Order)
                .ToList();


            // 6️⃣ Construir árbol
            return BuildMenuTree(finalMenus);
        }


    }
}