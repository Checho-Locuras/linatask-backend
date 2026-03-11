using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.Common.Utils;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Domain.Models.Login;
using LinaTask.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LinaTask.Application.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IMenuRepository _menuRepository; 
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            ILocationRepository locationRepository,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthService> logger,
            IPermissionRepository permissionRepository,
            IMenuRepository menuRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _locationRepository = locationRepository;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
            _permissionRepository = permissionRepository;
            _menuRepository = menuRepository;
        }

        // =====================================================
        // LOGIN
        // =====================================================

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);

            if (user == null)
                throw new UnauthorizedAccessException("Credenciales inválidas");

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Credenciales inválidas");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Usuario inactivo");

            return await BuildAuthResponseAsync(user);
        }

        // =====================================================
        // REGISTER
        // =====================================================

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                // ── Validaciones previas ──────────────────────────
                var existingUser = await _userRepository.GetByEmailAsync(registerDto.Email);
                if (existingUser != null)
                    throw new InvalidOperationException("El email ya está registrado");

                if (!string.IsNullOrWhiteSpace(registerDto.Phone))
                {
                    existingUser = await _userRepository.GetByPhoneAsync(registerDto.Phone);
                    if (existingUser != null)
                        throw new InvalidOperationException("El número de teléfono ya está registrado");
                }

                // ── Roles ─────────────────────────────────────────
                var roles = new List<Role>();
                if (registerDto.RoleIds == null || !registerDto.RoleIds.Any())
                    throw new InvalidOperationException("Debes seleccionar al menos un rol");

                foreach (var roleId in registerDto.RoleIds)
                {
                    var role = await _roleRepository.GetByIdAsync(roleId);
                    if (role == null)
                        throw new InvalidOperationException($"Rol con ID {roleId} no encontrado");
                    roles.Add(role);
                }

                await ValidateAcademicProfiles(registerDto, roles);

                // ── Instituciones ─────────────────────────────────
                var institutionIds = registerDto.AcademicProfiles
                    .Select(p => p.InstitutionId).Distinct().ToList();

                foreach (var institutionId in institutionIds)
                {
                    var institution = await _locationRepository.GetInstitutionByIdAsync(institutionId);
                    if (institution == null)
                        throw new InvalidOperationException($"Institución con ID {institutionId} no encontrada");
                }

                // ── Crear usuario ─────────────────────────────────
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Name = registerDto.Name,
                    PhoneNumber = PhoneHelper.NormalizeColombianPhone(registerDto.Phone),
                    Email = registerDto.Email.ToLower(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                    BirthDate = registerDto.BirthDate,
                    ProfilePhoto = registerDto.ProfilePhoto,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                foreach (var role in roles)
                    user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });

                foreach (var profileDto in registerDto.AcademicProfiles)
                {
                    user.AcademicProfiles.Add(new UserAcademicProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        RoleId = profileDto.RoleId,
                        InstitutionId = profileDto.InstitutionId,
                        EducationLevel = profileDto.EducationLevel,
                        CurrentSemester = profileDto.CurrentSemester,
                        CurrentGrade = profileDto.CurrentGrade,
                        GraduationYear = profileDto.GraduationYear,
                        StudyArea = profileDto.StudyArea,
                        AcademicStatus = profileDto.AcademicStatus,
                        ProfessionalDescription = profileDto.ProfessionalDescription,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                foreach (var addressDto in registerDto.UserAddresses)
                {
                    var city = await _locationRepository.GetCityByIdAsync(addressDto.CityId);
                    if (city == null)
                        throw new InvalidOperationException($"Ciudad con ID {addressDto.CityId} no encontrada");

                    user.Addresses.Add(new UserAddress
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        CityId = addressDto.CityId,
                        Address = addressDto.Address,
                        IsPrimary = addressDto.IsPrimary,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (registerDto.UserAddresses.Any() &&
                    !registerDto.UserAddresses.Any(a => a.IsPrimary))
                {
                    user.Addresses.First().IsPrimary = true;
                }

                var createdUser = await _userRepository.CreateAsync(user);

                _logger.LogInformation(
                    "Usuario registrado: {Email} | Roles: {Roles} | Perfiles: {Count}",
                    createdUser.Email,
                    string.Join(", ", roles.Select(r => r.Name)),
                    createdUser.AcademicProfiles.Count);

                return await BuildAuthResponseAsync(createdUser);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al registrar usuario: {Email}", registerDto.Email);
                throw;
            }
        }

        // =====================================================
        // SWITCH DE ROL ACTIVO
        // Endpoint sugerido: POST /auth/switch-role
        // Permite al frontend cambiar el rol activo y obtener
        // un nuevo JWT + menús del nuevo rol, sin re-login.
        // =====================================================

        public async Task<AuthResponseDto> SwitchRoleAsync(Guid userId, Guid roleId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new UnauthorizedAccessException("Usuario no encontrado");

            var hasRole = user.UserRoles.Any(ur => ur.RoleId == roleId);
            if (!hasRole)
                throw new UnauthorizedAccessException("El usuario no tiene asignado ese rol");

            return await BuildAuthResponseAsync(user, activeRoleId: roleId);
        }

        // =====================================================
        // REFRESH TOKEN
        // =====================================================

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            var principal = GetPrincipalFromExpiredToken(refreshTokenDto.Token);

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var activeRoleId = principal.FindFirst("activeRoleId")?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Token inválido");

            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId))
                ?? throw new UnauthorizedAccessException("Usuario no encontrado");

            // Mantener el mismo rol activo que tenía antes del refresh
            Guid? parsedRoleId = Guid.TryParse(activeRoleId, out var rid) ? rid : null;

            return await BuildAuthResponseAsync(user, activeRoleId: parsedRoleId);
        }

        public Task<bool> RevokeTokenAsync(string userId) => Task.FromResult(true);

        // =====================================================
        // NÚCLEO: construir la respuesta de autenticación
        // =====================================================

        /// <summary>
        /// Centraliza la construcción del AuthResponseDto.
        /// Si activeRoleId es null, se selecciona automáticamente el rol prioritario.
        /// </summary>
        private async Task<AuthResponseDto> BuildAuthResponseAsync(User user, Guid? activeRoleId = null)
        {
            var roles = user.UserRoles?
                .Where(ur => ur.Role != null)
                .Select(ur => new UserRoleDto
                {
                    RoleId = ur.RoleId,
                    RoleName = ur.Role!.Name,
                    RoleDescription = ur.Role.Description ?? ""
                }).ToList() ?? new List<UserRoleDto>();

            // Determinar rol activo
            var activeUserRole = activeRoleId.HasValue
                ? user.UserRoles?.FirstOrDefault(ur => ur.RoleId == activeRoleId.Value)
                : GetPrimaryUserRole(user);

            if (activeUserRole == null)
                throw new InvalidOperationException("No se pudo determinar el rol activo del usuario");

            var activeRoleDto = new UserRoleDto
            {
                RoleId = activeUserRole.RoleId,
                RoleName = activeUserRole.Role?.Name ?? "",
                RoleDescription = activeUserRole.Role?.Description ?? ""
            };

            // Permisos del rol activo (solo para el JWT)
            var permissions = await _permissionRepository.GetPermissionsByUserIdAsync(user.Id);
            var rolePermissions = permissions; // ya filtrados por userId; ajusta si tienes filtrado por roleId

            var token = GenerateJwtToken(user, activeUserRole, rolePermissions);
            var refreshToken = GenerateRefreshToken();

            // Menús del rol activo (van en la respuesta, NO en el JWT)
            var menus = await _menuRepository.GetMenusByRoleIdAsync(activeUserRole.RoleId);

            _logger.LogInformation(
                "Auth exitosa: {Email} | Rol activo: {Role}",
                user.Email, activeRoleDto.RoleName);

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                User = MapToUserDto(user),
                Roles = roles,
                ActiveRole = activeRoleDto,
                Menus = menus.ToList()
            };
        }

        // =====================================================
        // LÓGICA DE PRIORIDAD DE ROL
        // Ajusta el orden según las reglas de negocio de tu app
        // =====================================================

        private static readonly List<string> RolePriority = new()
        {
            "admin",
            "teacher",
            "student",
            "parent"
        };

        /// <summary>
        /// Devuelve el UserRole de mayor prioridad para el usuario.
        /// Si ninguno coincide con la lista, devuelve el primero disponible.
        /// </summary>
        private UserRole? GetPrimaryUserRole(User user)
        {
            if (user.UserRoles == null || !user.UserRoles.Any())
                return null;

            foreach (var priorityName in RolePriority)
            {
                var match = user.UserRoles.FirstOrDefault(ur =>
                    string.Equals(ur.Role?.Name, priorityName, StringComparison.OrdinalIgnoreCase));

                if (match != null) return match;
            }

            return user.UserRoles.First();
        }

        // =====================================================
        // GENERACIÓN DE JWT (liviano)
        // =====================================================

        private string GenerateJwtToken(User user, UserRole activeRole, IEnumerable<Permission> permissions)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name,            user.Name),
                new(ClaimTypes.Email,           user.Email),
                new(ClaimTypes.Role,            activeRole.Role?.Name ?? ""),
                new("activeRoleId",             activeRole.RoleId.ToString()),  // ← para el refresh
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var permission in permissions)
                claims.Add(new Claim("permission", permission.Code));

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var parameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                ValidateLifetime = false
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Token inválido");

            return principal;
        }

        private UserDto MapToUserDto(User user) => new()
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

            UserRoles = user.UserRoles?.Select(ur => new UserRoleDto
            {
                RoleId = ur.RoleId,
                RoleName = ur.Role?.Name ?? "",
                RoleDescription = ur.Role?.Description ?? ""
            }).ToList() ?? new(),

            AcademicProfiles = user.AcademicProfiles.Select(ap => new UserAcademicProfileDto
            {
                Id = ap.Id,
                RoleId = ap.RoleId,
                RoleName = ap.Role?.Name ?? "",
                InstitutionId = ap.InstitutionId,
                InstitutionName = ap.Institution?.Name ?? "",
                EducationLevel = ap.EducationLevel,
                CurrentSemester = ap.CurrentSemester,
                CurrentGrade = ap.CurrentGrade,
                GraduationYear = ap.GraduationYear,
                StudyArea = ap.StudyArea,
                AcademicStatus = ap.AcademicStatus,
                ProfessionalDescription = ap.ProfessionalDescription,
                CreatedAt = ap.CreatedAt
            }).ToList(),

            Addresses = user.Addresses.Select(a => new UserAddressDto
            {
                Id = a.Id,
                CityId = a.CityId,
                CityName = a.City?.Name ?? "",
                DepartmentName = a.City?.Department?.Name ?? "",
                CountryName = a.City?.Department?.Country?.Name ?? "",
                Address = a.Address,
                IsPrimary = a.IsPrimary,
                CreatedAt = a.CreatedAt
            }).ToList()
        };

        // ── Validaciones académicas (sin cambios) ──────────────

        private async Task ValidateAcademicProfiles(RegisterDto registerDto, List<Role> roles)
        {
            var rolesRequiringProfile = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "student", "teacher" };

            foreach (var role in roles)
            {
                if (!rolesRequiringProfile.Contains(role.Name)) continue;

                var profilesForRole = registerDto.AcademicProfiles
                    .Where(p => p.RoleId == role.Id).ToList();

                if (!profilesForRole.Any())
                    throw new InvalidOperationException(
                        $"El rol '{role.Name}' requiere al menos un perfil académico completo");

                foreach (var profile in profilesForRole)
                    ValidateEducationLevelFields(profile, role.Name);
            }

            var selectedRoleIds = roles.Select(r => r.Id).ToHashSet();
            var orphans = registerDto.AcademicProfiles
                .Where(p => !selectedRoleIds.Contains(p.RoleId)).ToList();

            if (orphans.Any())
                throw new InvalidOperationException(
                    "Hay perfiles académicos para roles que no fueron seleccionados");
        }

        private void ValidateEducationLevelFields(AcademicProfileDto profile, string roleName)
        {
            var level = profile.EducationLevel?.ToLower() ?? string.Empty;

            var needGrade = new HashSet<string> { "primaria", "secundaria" };
            var needSemester = new HashSet<string>
                { "bachillerato", "universidad", "tecnica", "tecnologica",
                  "especializacion", "maestria", "doctorado" };

            if (needGrade.Contains(level))
            {
                if (string.IsNullOrWhiteSpace(profile.CurrentGrade))
                    throw new InvalidOperationException(
                        $"El nivel '{profile.EducationLevel}' requiere especificar el grado actual");

                if (profile.CurrentSemester.HasValue || !string.IsNullOrWhiteSpace(profile.StudyArea))
                    throw new InvalidOperationException(
                        $"El nivel '{profile.EducationLevel}' no debe tener semestre ni área de estudio");
            }
            else if (needSemester.Contains(level))
            {
                if (!profile.CurrentSemester.HasValue || profile.CurrentSemester <= 0)
                    throw new InvalidOperationException(
                        $"El nivel '{profile.EducationLevel}' requiere especificar el semestre actual (mayor a 0)");

                if (profile.CurrentSemester > 20)
                    throw new InvalidOperationException("El semestre no puede ser mayor a 20");

                if (string.IsNullOrWhiteSpace(profile.StudyArea))
                    throw new InvalidOperationException(
                        $"El nivel '{profile.EducationLevel}' requiere especificar el área de estudio");

                if (!string.IsNullOrWhiteSpace(profile.CurrentGrade))
                    throw new InvalidOperationException(
                        $"El nivel '{profile.EducationLevel}' no debe tener grado escolar");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Nivel educativo '{profile.EducationLevel}' no reconocido");
            }

            var validStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "activo", "graduado", "inactivo" };

            if (!validStatuses.Contains(profile.AcademicStatus))
                throw new InvalidOperationException(
                    $"Estado académico '{profile.AcademicStatus}' no válido. Valores permitidos: activo, graduado, inactivo");
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new UnauthorizedAccessException("Usuario no encontrado");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("La contraseña actual es incorrecta");

            if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
                throw new InvalidOperationException("La nueva contraseña debe ser diferente a la actual");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.CreatedAt = user.CreatedAt.ToUniversalTime();
            user.BirthDate = user.BirthDate.ToUniversalTime();

            await _userRepository.UpdateAsync(user);
        }
    }
}