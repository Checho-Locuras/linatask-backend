// Application/Services/RoleService.cs
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LinaTask.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            IRoleRepository roleRepository,
            ILogger<RoleService> logger)
        {
            _roleRepository = roleRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            try
            {
                var roles = await _roleRepository.GetAllAsync();
                return roles.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los roles");
                throw;
            }
        }

        public async Task<RoleDto?> GetRoleByIdAsync(Guid id)
        {
            try
            {
                var role = await _roleRepository.GetByIdAsync(id);
                return role == null ? null : MapToDto(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener rol por ID: {RoleId}", id);
                throw;
            }
        }

        public async Task<RoleDto?> GetRoleByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("El nombre del rol no puede estar vacío");

                var role = await _roleRepository.GetByNameAsync(name);
                return role == null ? null : MapToDto(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener rol por nombre: {RoleName}", name);
                throw;
            }
        }

        public async Task<RoleDto> CreateRoleAsync(CreateRoleDto createRoleDto)
        {
            try
            {
                // Validar que el nombre no exista
                var existingRole = await _roleRepository.GetByNameAsync(createRoleDto.Name);
                if (existingRole != null)
                    throw new InvalidOperationException($"Ya existe un rol con el nombre '{createRoleDto.Name}'");

                // Crear nuevo rol
                var role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = createRoleDto.Name.Trim(),
                    Description = createRoleDto.Description?.Trim() ?? string.Empty
                };

                var createdRole = await _roleRepository.CreateAsync(role);
                return MapToDto(createdRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear rol: {RoleName}", createRoleDto.Name);
                throw;
            }
        }

        public async Task<RoleDto> UpdateRoleAsync(Guid id, UpdateRoleDto updateRoleDto)
        {
            try
            {
                var role = await _roleRepository.GetByIdAsync(id);
                if (role == null)
                    throw new KeyNotFoundException($"Rol con ID {id} no encontrado");

                // Si se cambia el nombre, validar que no exista otro con el mismo nombre
                if (!string.IsNullOrWhiteSpace(updateRoleDto.Name) &&
                    updateRoleDto.Name.Trim() != role.Name)
                {
                    var existingRole = await _roleRepository.GetByNameAsync(updateRoleDto.Name.Trim());
                    if (existingRole != null && existingRole.Id != id)
                        throw new InvalidOperationException($"Ya existe un rol con el nombre '{updateRoleDto.Name}'");
                }

                // Actualizar campos
                if (!string.IsNullOrWhiteSpace(updateRoleDto.Name))
                    role.Name = updateRoleDto.Name.Trim();

                if (!string.IsNullOrWhiteSpace(updateRoleDto.Description))
                    role.Description = updateRoleDto.Description.Trim();

                var updatedRole = await _roleRepository.UpdateAsync(role);
                return MapToDto(updatedRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar rol: {RoleId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteRoleAsync(Guid id)
        {
            try
            {
                return await _roleRepository.DeleteAsync(id);
            }
            catch (InvalidOperationException ex)
            {
                // Relanzar con mensaje amigable
                throw new InvalidOperationException($"No se puede eliminar el rol: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar rol: {RoleId}", id);
                throw;
            }
        }

        public async Task<bool> RoleExistsAsync(Guid id)
        {
            try
            {
                return await _roleRepository.ExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia del rol: {RoleId}", id);
                throw;
            }
        }

        public async Task<bool> RoleExistsByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("El nombre del rol no puede estar vacío");

                return await _roleRepository.ExistsByNameAsync(name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia del rol por nombre: {RoleName}", name);
                throw;
            }
        }

        private static RoleDto MapToDto(Role role)
        {
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                UserCount = role.UserRoles?.Count ?? 0
            };
        }
    }
}