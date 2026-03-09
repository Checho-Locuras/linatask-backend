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
    public class RoleRepository : IRoleRepository
    {
        private readonly LinaTaskDbContext _context;

        public RoleRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        // Método auxiliar para cargar relaciones comunes
        private IQueryable<Role> GetRoleWithIncludes()
        {
            return _context.Roles
                .Include(r => r.UserRoles)
                    .ThenInclude(ur => ur.User);
        }

        public async Task<IEnumerable<Role>> GetAllAsync()
        {
            try
            {
                return await _context.Roles
                    .AsNoTracking()
                    .OrderBy(r => r.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Considera usar un logger aquí
                // _logger.LogError(ex, "Error al obtener todos los roles");
                throw new ApplicationException($"Error al obtener todos los roles: {ex.Message}", ex);
            }
        }

        public async Task<Role?> GetByIdAsync(Guid id)
        {
            return await GetRoleWithIncludes()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Role?> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("El nombre del rol no puede estar vacío", nameof(name));

            return await _context.Roles
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
        }

        public async Task<Role> CreateAsync(Role role)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            // Validar que no exista un rol con el mismo nombre
            if (await ExistsByNameAsync(role.Name))
                throw new InvalidOperationException($"Ya existe un rol con el nombre '{role.Name}'");

            try
            {
                // Normalizar el nombre
                role.Name = role.Name.Trim();

                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
                return role;
            }
            catch (Exception ex)
            {
                // Considera usar un logger aquí
                // _logger.LogError(ex, "Error al crear rol");
                throw new ApplicationException($"Error al crear el rol: {ex.Message}", ex);
            }
        }

        public async Task<Role> UpdateAsync(Role role)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            // Validar que el rol existe
            var existingRole = await GetByIdAsync(role.Id);
            if (existingRole == null)
                throw new KeyNotFoundException($"Rol con ID {role.Id} no encontrado");

            // Validar que no exista otro rol con el mismo nombre (excepto este mismo)
            if (await _context.Roles.AnyAsync(r =>
                r.Id != role.Id && r.Name.ToLower() == role.Name.ToLower()))
            {
                throw new InvalidOperationException($"Ya existe otro rol con el nombre '{role.Name}'");
            }

            try
            {
                // Actualizar propiedades
                existingRole.Name = role.Name.Trim();
                existingRole.Description = role.Description;

                _context.Roles.Update(existingRole);
                await _context.SaveChangesAsync();
                return existingRole;
            }
            catch (Exception ex)
            {
                // Considera usar un logger aquí
                // _logger.LogError(ex, "Error al actualizar rol");
                throw new ApplicationException($"Error al actualizar el rol: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var role = await GetByIdAsync(id);
            if (role == null)
                return false;

            // Verificar si el rol está siendo usado por algún usuario
            if (role.UserRoles != null && role.UserRoles.Any())
                throw new InvalidOperationException(
                    $"No se puede eliminar el rol '{role.Name}' porque está asignado a {role.UserRoles.Count} usuario(s).");

            try
            {
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Considera usar un logger aquí
                // _logger.LogError(ex, "Error al eliminar rol");
                throw new ApplicationException($"Error al eliminar el rol: {ex.Message}", ex);
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Roles.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("El nombre del rol no puede estar vacío", nameof(name));

            return await _context.Roles
                .AnyAsync(r => r.Name.ToLower() == name.ToLower());
        }

        // Métodos adicionales útiles

        public async Task<IEnumerable<Role>> GetRolesByUserAsync(Guid userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<int> CountUsersWithRoleAsync(Guid roleId)
        {
            return await _context.UserRoles
                .CountAsync(ur => ur.RoleId == roleId);
        }

        public async Task<IEnumerable<Role>> SearchByNameAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            return await _context.Roles
                .Where(r => r.Name.ToLower().Contains(searchTerm.ToLower()))
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<Role> GetOrCreateByNameAsync(string name, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("El nombre del rol no puede estar vacío", nameof(name));

            var existingRole = await GetByNameAsync(name);
            if (existingRole != null)
                return existingRole;

            // Crear nuevo rol
            var newRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Description = description
            };

            return await CreateAsync(newRole);
        }
    }
}