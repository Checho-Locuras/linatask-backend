using LinaTask.Domain.Interfaces;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinaTask.Infrastructure.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly LinaTaskDbContext _context;

        public UserRoleRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<bool> UserHasRoleAsync(Guid userId, string roleName)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur =>
                    ur.UserId == userId &&
                    ur.Role.Name.ToLower() == roleName.ToLower());
        }

        public async Task AssignRoleAsync(Guid userId, string roleName)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower());

            if (role == null)
                throw new KeyNotFoundException($"Rol '{roleName}' no encontrado");

            var exists = await UserHasRoleAsync(userId, roleName);
            if (exists)
                return;

            _context.UserRoles.Add(new Domain.Models.UserRole
            {
                UserId = userId,
                RoleId = role.Id
            });

            await _context.SaveChangesAsync();
        }

        public async Task RemoveRoleAsync(Guid userId, string roleName)
        {
            var userRole = await _context.UserRoles
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur =>
                    ur.UserId == userId &&
                    ur.Role.Name.ToLower() == roleName.ToLower());

            if (userRole == null)
                return;

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<string>> GetRolesByUserAsync(Guid userId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role.Name)
                .ToListAsync();
        }
    }
}
