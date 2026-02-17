using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinaTask.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IRoleRepository _roleRepository;

        public UserService(
            IUserRepository userRepository,
            ILocationRepository locationRepository,
            IRoleRepository roleRepository)
        {
            _userRepository = userRepository;
            _locationRepository = locationRepository;
            _roleRepository = roleRepository;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToDto);
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? null : MapToDto(user);
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user == null ? null : MapToDto(user);
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            // Validar que la institución existe
            var institution = await _locationRepository.GetInstitutionByIdAsync(createUserDto.InstitutionId);
            if (institution == null)
                throw new InvalidOperationException("Institución no encontrada");

            // Validar que la ciudad existe
            var city = await _locationRepository.GetCityByIdAsync(createUserDto.CityId);
            if (city == null)
                throw new InvalidOperationException("Ciudad no encontrada");

            // Validar roles
            if (createUserDto.RoleIds == null || !createUserDto.RoleIds.Any())
                throw new InvalidOperationException("El usuario debe tener al menos un rol");

            var roles = new List<Role>();
            foreach (var roleId in createUserDto.RoleIds)
            {
                var role = await _roleRepository.GetByIdAsync(roleId);
                if (role == null)
                    throw new InvalidOperationException($"Rol con ID {roleId} no encontrado");
                roles.Add(role);
            }

            // Crear usuario
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = createUserDto.Name,
                Email = createUserDto.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                PhoneNumber = createUserDto.PhoneNumber,
                ProfilePhoto = createUserDto.ProfilePhoto,
                BirthDate = createUserDto.BirthDate,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Asignar roles
            foreach (var role in roles)
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
            }

            // Crear perfil académico
            var academicProfile = new UserAcademicProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                InstitutionId = createUserDto.InstitutionId,
                EducationLevel = createUserDto.EducationLevel,
                CurrentSemester = createUserDto.CurrentSemester,
                CurrentGrade = createUserDto.CurrentGrade,
                GraduationYear = createUserDto.GraduationYear,
                StudyArea = createUserDto.StudyArea,
                AcademicStatus = createUserDto.AcademicStatus,
                CreatedAt = DateTime.UtcNow
            };

            // Crear dirección inicial
            var address = new UserAddress
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CityId = createUserDto.CityId,
                Address = createUserDto.Address,
                IsPrimary = true,
                CreatedAt = DateTime.UtcNow
            };

            // Agregar relaciones
            user.AcademicProfiles.Add(academicProfile);
            user.Addresses.Add(address);

            var createdUser = await _userRepository.CreateAsync(user);
            return MapToDto(createdUser);
        }

        public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"Usuario con ID {id} no encontrado");

            // Actualizar solo los campos que vienen en el DTO
            if (!string.IsNullOrEmpty(updateUserDto.Name))
                user.Name = updateUserDto.Name;

            if (!string.IsNullOrEmpty(updateUserDto.Email))
                user.Email = updateUserDto.Email.ToLower();

            if (updateUserDto.Rating.HasValue)
                user.Rating = updateUserDto.Rating.Value;

            if (!string.IsNullOrEmpty(updateUserDto.PhoneNumber))
                user.PhoneNumber = updateUserDto.PhoneNumber;

            if (!string.IsNullOrEmpty(updateUserDto.ProfilePhoto))
                user.ProfilePhoto = updateUserDto.ProfilePhoto;

            if (updateUserDto.BirthDate.HasValue)
                user.BirthDate = updateUserDto.BirthDate.Value;

            if (updateUserDto.IsActive.HasValue)
                user.IsActive = updateUserDto.IsActive.Value;

            // Si se proporciona una nueva contraseña, hashearla
            if (!string.IsNullOrEmpty(updateUserDto.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateUserDto.Password);

            // Actualizar roles si se proporcionan
            if (updateUserDto.RoleIds != null && updateUserDto.RoleIds.Any())
            {
                await UpdateUserRolesAsync(user, updateUserDto.RoleIds);
            }

            var updatedUser = await _userRepository.UpdateAsync(user);
            return MapToDto(updatedUser);
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            return await _userRepository.DeleteAsync(id);
        }

        // ==========================================
        // MÉTODOS PARA DIRECCIONES
        // ==========================================

        public async Task<UserAddressDto> AddAddressAsync(Guid userId, CreateAddressDto createAddressDto)
        {
            // Verificar que el usuario existe
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"Usuario con ID {userId} no encontrado");

            // Verificar que la ciudad existe
            var city = await _locationRepository.GetCityByIdAsync(createAddressDto.CityId);
            if (city == null)
                throw new InvalidOperationException("Ciudad no encontrada");

            // Si se marca como primaria, desmarcar las demás
            if (createAddressDto.IsPrimary)
            {
                var existingAddresses = await _userRepository.GetUserAddressesAsync(userId);
                foreach (var addr in existingAddresses.Where(a => a.IsPrimary))
                {
                    addr.IsPrimary = false;
                    await _userRepository.UpdateAddressAsync(addr);
                }
            }

            var address = new UserAddress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CityId = createAddressDto.CityId,
                Address = createAddressDto.Address,
                IsPrimary = createAddressDto.IsPrimary,
                CreatedAt = DateTime.UtcNow
            };

            var createdAddress = await _userRepository.AddAddressAsync(address);

            return new UserAddressDto
            {
                Id = createdAddress.Id,
                CityId = createdAddress.CityId,
                CityName = city.Name,
                DepartmentName = city.Department?.Name ?? "",
                CountryName = city.Department?.Country?.Name ?? "",
                Address = createdAddress.Address,
                IsPrimary = createdAddress.IsPrimary,
                CreatedAt = createdAddress.CreatedAt
            };
        }

        public async Task<UserAddressDto> UpdateAddressAsync(Guid addressId, UpdateAddressDto updateAddressDto)
        {
            var address = await _userRepository.GetAddressByIdAsync(addressId);
            if (address == null)
                throw new KeyNotFoundException($"Dirección con ID {addressId} no encontrada");

            // Si se cambia la ciudad, validar que existe
            if (updateAddressDto.CityId.HasValue)
            {
                var city = await _locationRepository.GetCityByIdAsync(updateAddressDto.CityId.Value);
                if (city == null)
                    throw new InvalidOperationException("Ciudad no encontrada");

                address.CityId = updateAddressDto.CityId.Value;
            }

            if (!string.IsNullOrEmpty(updateAddressDto.Address))
                address.Address = updateAddressDto.Address;

            // Si se marca como primaria, desmarcar las demás
            if (updateAddressDto.IsPrimary.HasValue && updateAddressDto.IsPrimary.Value)
            {
                var existingAddresses = await _userRepository.GetUserAddressesAsync(address.UserId);
                foreach (var addr in existingAddresses.Where(a => a.Id != addressId && a.IsPrimary))
                {
                    addr.IsPrimary = false;
                    await _userRepository.UpdateAddressAsync(addr);
                }
                address.IsPrimary = true;
            }

            var updatedAddress = await _userRepository.UpdateAddressAsync(address);
            var cityData = await _locationRepository.GetCityByIdAsync(updatedAddress.CityId);

            return new UserAddressDto
            {
                Id = updatedAddress.Id,
                CityId = updatedAddress.CityId,
                CityName = cityData?.Name ?? "",
                DepartmentName = cityData?.Department?.Name ?? "",
                CountryName = cityData?.Department?.Country?.Name ?? "",
                Address = updatedAddress.Address,
                IsPrimary = updatedAddress.IsPrimary,
                CreatedAt = updatedAddress.CreatedAt
            };
        }

        public async Task<bool> DeleteAddressAsync(Guid addressId)
        {
            var address = await _userRepository.GetAddressByIdAsync(addressId);
            if (address == null)
                return false;

            // No permitir eliminar si es la única dirección
            var userAddresses = await _userRepository.GetUserAddressesAsync(address.UserId);
            if (userAddresses.Count() == 1)
                throw new InvalidOperationException("No se puede eliminar la única dirección del usuario");

            // Si era primaria, asignar otra como primaria
            if (address.IsPrimary)
            {
                var newPrimary = userAddresses.FirstOrDefault(a => a.Id != addressId);
                if (newPrimary != null)
                {
                    newPrimary.IsPrimary = true;
                    await _userRepository.UpdateAddressAsync(newPrimary);
                }
            }

            return await _userRepository.DeleteAddressAsync(addressId);
        }

        public async Task<UserAddressDto> SetPrimaryAddressAsync(Guid addressId)
        {
            var address = await _userRepository.GetAddressByIdAsync(addressId);
            if (address == null)
                throw new KeyNotFoundException($"Dirección con ID {addressId} no encontrada");

            // Desmarcar todas las direcciones del usuario
            var userAddresses = await _userRepository.GetUserAddressesAsync(address.UserId);
            foreach (var addr in userAddresses.Where(a => a.IsPrimary))
            {
                addr.IsPrimary = false;
                await _userRepository.UpdateAddressAsync(addr);
            }

            // Marcar la nueva como primaria
            address.IsPrimary = true;
            var updatedAddress = await _userRepository.UpdateAddressAsync(address);
            var city = await _locationRepository.GetCityByIdAsync(updatedAddress.CityId);

            return new UserAddressDto
            {
                Id = updatedAddress.Id,
                CityId = updatedAddress.CityId,
                CityName = city?.Name ?? "",
                DepartmentName = city?.Department?.Name ?? "",
                CountryName = city?.Department?.Country?.Name ?? "",
                Address = updatedAddress.Address,
                IsPrimary = updatedAddress.IsPrimary,
                CreatedAt = updatedAddress.CreatedAt
            };
        }

        public async Task<IEnumerable<UserAddressDto>> GetUserAddressesAsync(Guid userId)
        {
            var addresses = await _userRepository.GetUserAddressesAsync(userId);
            var result = new List<UserAddressDto>();

            foreach (var address in addresses)
            {
                var city = await _locationRepository.GetCityByIdAsync(address.CityId);
                result.Add(new UserAddressDto
                {
                    Id = address.Id,
                    CityId = address.CityId,
                    CityName = city?.Name ?? "",
                    DepartmentName = city?.Department?.Name ?? "",
                    CountryName = city?.Department?.Country?.Name ?? "",
                    Address = address.Address,
                    IsPrimary = address.IsPrimary,
                    CreatedAt = address.CreatedAt
                });
            }

            return result;
        }

        // ==========================================
        // MÉTODOS PARA ROLES
        // ==========================================

        public async Task<IEnumerable<UserRoleDto>> GetUserRolesAsync(Guid userId)
        {
            var userRoles = await _userRepository.GetUserRolesAsync(userId);

            return userRoles.Select(ur => new UserRoleDto
            {
                RoleId = ur.RoleId,
                RoleName = ur.Role.Name,
                RoleDescription = ur.Role.Description
            }).ToList();
        }

        public async Task<bool> UserHasRoleAsync(Guid userId, string roleName)
        {
            return await _userRepository.HasRoleAsync(userId, roleName);
        }

        public async Task<UserDto> AddRoleToUserAsync(Guid userId, Guid roleId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"Usuario con ID {userId} no encontrado");

            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null)
                throw new InvalidOperationException($"Rol con ID {roleId} no encontrado");

            // Verificar si ya tiene el rol
            if (user.UserRoles.Any(ur => ur.RoleId == roleId))
                throw new InvalidOperationException("El usuario ya tiene este rol asignado");

            user.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId
            });

            await _userRepository.UpdateAsync(user);
            return MapToDto(user);
        }

        public async Task<UserDto> RemoveRoleFromUserAsync(Guid userId, Guid roleId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"Usuario con ID {userId} no encontrado");

            var userRole = user.UserRoles.FirstOrDefault(ur => ur.RoleId == roleId);
            if (userRole == null)
                throw new InvalidOperationException("El usuario no tiene este rol asignado");

            // Verificar que no sea el último rol
            if (user.UserRoles.Count == 1)
                throw new InvalidOperationException("No se puede eliminar el único rol del usuario");

            user.UserRoles.Remove(userRole);
            await _userRepository.UpdateAsync(user);
            return MapToDto(user);
        }

        // ==========================================
        // MÉTODOS PRIVADOS
        // ==========================================

        private async Task UpdateUserRolesAsync(User user, IEnumerable<Guid> roleIds)
        {
            // Limpiar roles actuales
            user.UserRoles.Clear();

            // Agregar nuevos roles
            foreach (var roleId in roleIds)
            {
                var role = await _roleRepository.GetByIdAsync(roleId);
                if (role == null)
                    throw new InvalidOperationException($"Rol con ID {roleId} no encontrado");

                user.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId
                });
            }

            // Verificar que al menos quede un rol
            if (!user.UserRoles.Any())
                throw new InvalidOperationException("El usuario debe tener al menos un rol");
        }

        private static UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Rating = user.Rating,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                ProfilePhoto = user.ProfilePhoto,
                PhoneNumber = user.PhoneNumber,
                BirthDate = user.BirthDate,

                // Mapear roles
                UserRoles = user.UserRoles?.Select(ur => new UserRoleDto
                {
                    RoleId = ur.RoleId,
                    RoleName = ur.Role?.Name ?? "",
                    RoleDescription = ur.Role?.Description ?? ""
                }).ToList() ?? new List<UserRoleDto>(),

                // Mapear perfiles académicos
                AcademicProfiles = user.AcademicProfiles?.Select(ap => new UserAcademicProfileDto
                {
                    Id = ap.Id,
                    InstitutionId = ap.InstitutionId,
                    InstitutionName = ap.Institution?.Name ?? "",
                    EducationLevel = ap.EducationLevel,
                    CurrentSemester = ap.CurrentSemester,
                    CurrentGrade = ap.CurrentGrade,
                    GraduationYear = ap.GraduationYear,
                    StudyArea = ap.StudyArea,
                    AcademicStatus = ap.AcademicStatus,
                    CreatedAt = ap.CreatedAt
                }).ToList() ?? new List<UserAcademicProfileDto>(),

                // Mapear direcciones
                Addresses = user.Addresses?.Select(a => new UserAddressDto
                {
                    Id = a.Id,
                    CityId = a.CityId,
                    CityName = a.City?.Name ?? "",
                    DepartmentName = a.City?.Department?.Name ?? "",
                    CountryName = a.City?.Department?.Country?.Name ?? "",
                    Address = a.Address,
                    IsPrimary = a.IsPrimary,
                    CreatedAt = a.CreatedAt
                }).ToList() ?? new List<UserAddressDto>()
            };
        }
    }
}