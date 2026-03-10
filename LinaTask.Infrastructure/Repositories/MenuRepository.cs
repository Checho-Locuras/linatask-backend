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
            // 1. Obtener el nombre del rol
            var role = await _context.Roles
                .Where(r => r.Id == roleId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync();

            if (role == null) return Enumerable.Empty<Menu>();

            // 2. Prefijo de ruta según el rol
            var routePrefix = role switch
            {
                "SUPER_ADMIN" or "admin" => "/admin/",
                "teacher" => "/teacher/",
                "student" => "/student/",
                _ => "/admin/"
            };

            // 3. Módulos del rol
            var roleModules = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission.Module)
                .Distinct()
                .ToListAsync();

            // 4. Mapeo explícito ruta → módulos requeridos
            var menuModuleMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                // ── SHARED (un único menú para todos los roles) ──
                { "/shared/dashboard",     new[] { "*" } },
                { "/shared/chat",          new[] { "Chat" } },
                { "/shared/profile",       new[] { "Profile" } },
                { "/shared/marketplace",                new[] { "Marketplace" } },
                { "/shared/marketplace/tasks",          new[] { "Marketplace" } },
                { "/shared/marketplace/my-tasks",       new[] { "Marketplace" } },
                { "/shared/marketplace/my-offers",      new[] { "Marketplace" } },
                { "/shared/marketplace/payments",       new[] { "Marketplace" } },
                { "/shared/marketplace/history",        new[] { "Marketplace" } },

                // ── ADMIN ────────────────────────────────────────
                { "/admin/users",          new[] { "Users" } },
                { "/admin/students",       new[] { "Students", "Users" } },
                { "/admin/teachers",       new[] { "Teachers", "Users" } },
                { "/admin/tasks",          new[] { "Tasks" } },
                { "/admin/offers",         new[] { "Offers" } },
                { "/admin/payments",       new[] { "Payments" } },
                { "/admin/subjects",       new[] { "Subjects", "Teachers" } },
                { "/admin/sessions",       new[] { "Sessions" } },
                { "/admin/reports",        new[] { "Reports", "Payments", "Sessions", "Tasks" } },
                { "/admin/settings",       new[] { "*" } },

                // ── STUDENT ──────────────────────────────────────
                { "/student/schedule",     new[] { "Schedule" } },
                { "/student/sessions",     new[] { "Sessions" } },
                { "/student/tasks",        new[] { "Tasks" } },
                { "/student/payments",     new[] { "Payments" } },

                // ── TEACHER ──────────────────────────────────────
                { "/teacher/subjects",     new[] { "Subjects" } },
                { "/teacher/availability", new[] { "Availability" } },
                { "/teacher/requests",     new[] { "Requests" } },
                { "/teacher/sessions",     new[] { "Sessions" } },
                { "/teacher/tasks",        new[] { "Tasks" } },
                { "/teacher/earnings",     new[] { "Earnings" } },
            };

            // 5. Obtener menús del rol + los compartidos
            var allMenus = await _context.Menus
                .Where(m => m.IsVisible &&
                            (m.Route.StartsWith(routePrefix) || m.Route.StartsWith("/shared/")))
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Name)
                .AsNoTracking()
                .ToListAsync();

            // 6. Filtrar según permisos
            var accessibleMenus = allMenus.Where(menu =>
            {
                if (!menuModuleMap.TryGetValue(menu.Route, out var requiredModules))
                    return false;

                if (requiredModules.Contains("*"))
                    return true;

                return requiredModules.Any(required =>
                    roleModules.Contains(required, StringComparer.OrdinalIgnoreCase));
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