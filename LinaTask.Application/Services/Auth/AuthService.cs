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
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            ILocationRepository locationRepository,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthService> logger,
            IPermissionRepository permissionRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _locationRepository = locationRepository;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
            _permissionRepository = permissionRepository;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);

            if (user == null)
                throw new UnauthorizedAccessException("Credenciales inválidas");

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Credenciales inválidas");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Usuario inactivo");

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            _logger.LogInformation("Usuario autenticado exitosamente: {Email}", user.Email);

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                User = MapToUserDto(user)
            };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                // =====================
                // VALIDACIONES PREVIAS
                // =====================

                // 1. Verificar si el email ya existe
                var existingUser = await _userRepository.GetByEmailAsync(registerDto.Email);
                if (existingUser != null)
                    throw new InvalidOperationException("El email ya está registrado");

                // 2. Verificar si el teléfono ya existe (solo si se proporcionó)
                if (!string.IsNullOrWhiteSpace(registerDto.Phone))
                {
                    existingUser = await _userRepository.GetByPhoneAsync(registerDto.Phone);
                    if (existingUser != null)
                        throw new InvalidOperationException("El número de teléfono ya está registrado");
                }

                // =====================
                // VALIDAR Y CARGAR ROLES
                // =====================

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

                // 3. Validar perfiles académicos:
                //    - Cada rol que lo requiera debe tener AL MENOS UN perfil
                //    - Se permiten MÚLTIPLES perfiles para el mismo rol (ej: dos carreras)
                //    - No puede haber perfiles para roles no seleccionados
                await ValidateAcademicProfiles(registerDto, roles);

                // =====================
                // VALIDAR INSTITUCIONES
                // =====================

                var institutionIds = registerDto.AcademicProfiles
                    .Select(p => p.InstitutionId)
                    .Distinct()
                    .ToList();

                foreach (var institutionId in institutionIds)
                {
                    var institution = await _locationRepository.GetInstitutionByIdAsync(institutionId);
                    if (institution == null)
                        throw new InvalidOperationException($"Institución con ID {institutionId} no encontrada");
                }

                // =====================
                // CREAR USUARIO
                // =====================

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

                // =====================
                // ASIGNAR ROLES
                // =====================

                foreach (var role in roles)
                {
                    user.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id,
                    });
                }

                // =====================
                // CREAR PERFILES ACADÉMICOS
                // Se admiten múltiples perfiles por rol (ej: estudiante de dos carreras)
                // =====================

                foreach (var profileDto in registerDto.AcademicProfiles)
                {
                    var academicProfile = new UserAcademicProfile
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
                    };
                    user.AcademicProfiles.Add(academicProfile);
                }

                // =====================
                // CREAR DIRECCIONES
                // =====================

                foreach (var addressDto in registerDto.UserAddresses)
                {
                    var city = await _locationRepository.GetCityByIdAsync(addressDto.CityId);
                    if (city == null)
                        throw new InvalidOperationException($"Ciudad con ID {addressDto.CityId} no encontrada");

                    var address = new UserAddress
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        CityId = addressDto.CityId,
                        Address = addressDto.Address,
                        IsPrimary = addressDto.IsPrimary,
                        CreatedAt = DateTime.UtcNow
                    };
                    user.Addresses.Add(address);
                }

                // Garantizar que haya exactamente una dirección primaria
                if (registerDto.UserAddresses.Any() &&
                    !registerDto.UserAddresses.Any(a => a.IsPrimary))
                {
                    user.Addresses.First().IsPrimary = true;
                }

                // =====================
                // GUARDAR EN BASE DE DATOS
                // =====================

                var createdUser = await _userRepository.CreateAsync(user);

                // =====================
                // GENERAR TOKENS
                // =====================

                var token = GenerateJwtToken(createdUser);
                var refreshToken = GenerateRefreshToken();

                _logger.LogInformation(
                    "Usuario registrado exitosamente: {Email} con roles: {Roles}, perfiles académicos: {ProfileCount}",
                    createdUser.Email,
                    string.Join(", ", roles.Select(r => r.Name)),
                    createdUser.AcademicProfiles.Count
                );

                return new AuthResponseDto
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                    User = MapToUserDto(createdUser)
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al registrar usuario: {Email}", registerDto.Email);
                throw; // Re-lanzar para que el controller devuelva el mensaje correcto al cliente
            }
        }

        // =====================================================
        // MÉTODOS DE VALIDACIÓN PRIVADOS
        // =====================================================

        /// <summary>
        /// Valida los perfiles académicos del registro:
        /// - Cada rol que requiere perfil académico debe tener AL MENOS UNO.
        /// - Se permiten MÚLTIPLES perfiles para el mismo rol (ej: dos carreras simultáneas).
        /// - No puede haber perfiles para roles que no fueron seleccionados.
        /// - Cada perfil individual pasa por validación de campos según nivel educativo.
        /// </summary>
        private async Task ValidateAcademicProfiles(RegisterDto registerDto, List<Role> roles)
        {
            var rolesRequiringProfile = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "student",
                "teacher"
            };

            // Verificar que cada rol que lo requiera tenga AL MENOS UN perfil
            foreach (var role in roles)
            {
                if (rolesRequiringProfile.Contains(role.Name))
                {
                    var profilesForRole = registerDto.AcademicProfiles
                        .Where(p => p.RoleId == role.Id)
                        .ToList();

                    if (!profilesForRole.Any())
                        throw new InvalidOperationException(
                            $"El rol '{role.Name}' requiere al menos un perfil académico completo");

                    // Validar cada perfil de este rol individualmente
                    foreach (var profile in profilesForRole)
                    {
                        ValidateEducationLevelFields(profile, role.Name);
                    }
                }
            }

            // Validar que no haya perfiles para roles no seleccionados
            var selectedRoleIds = roles.Select(r => r.Id).ToHashSet();
            var orphanProfiles = registerDto.AcademicProfiles
                .Where(p => !selectedRoleIds.Contains(p.RoleId))
                .ToList();

            if (orphanProfiles.Any())
                throw new InvalidOperationException(
                    "Hay perfiles académicos para roles que no fueron seleccionados");
        }

        /// <summary>
        /// Valida que los campos de un perfil académico sean coherentes con su nivel educativo.
        /// </summary>
        private void ValidateEducationLevelFields(AcademicProfileDto profile, string roleName)
        {
            var educationLevel = profile.EducationLevel?.ToLower() ?? string.Empty;

            var levelsRequiringGrade = new HashSet<string> { "primaria", "secundaria" };
            var levelsRequiringSemester = new HashSet<string>
            {
                "bachillerato", "universidad", "tecnica",
                "tecnologica", "especializacion", "maestria", "doctorado"
            };

            if (levelsRequiringGrade.Contains(educationLevel))
            {
                if (string.IsNullOrWhiteSpace(profile.CurrentGrade))
                    throw new InvalidOperationException(
                        $"El nivel '{profile.EducationLevel}' requiere especificar el grado actual");

                if (profile.CurrentSemester.HasValue || !string.IsNullOrWhiteSpace(profile.StudyArea))
                    throw new InvalidOperationException(
                        $"El nivel '{profile.EducationLevel}' no debe tener semestre ni área de estudio");
            }
            else if (levelsRequiringSemester.Contains(educationLevel))
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
            {
                "activo", "graduado", "inactivo"
            };

            if (!validStatuses.Contains(profile.AcademicStatus))
                throw new InvalidOperationException(
                    $"Estado académico '{profile.AcademicStatus}' no válido. Valores permitidos: activo, graduado, inactivo");
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            var principal = GetPrincipalFromExpiredToken(refreshTokenDto.Token);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Invalid token");

            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            var newToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            return new AuthResponseDto
            {
                Token = newToken,
                RefreshToken = newRefreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                User = MapToUserDto(user)
            };
        }

        public Task<bool> RevokeTokenAsync(string userId)
        {
            return Task.FromResult(true);
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (user.UserRoles != null && user.UserRoles.Any())
            {
                foreach (var userRole in user.UserRoles)
                {
                    if (userRole.Role != null)
                        claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                }
            }

            var permissions = GetUserPermissions(user.Id).Result;
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

        private async Task<IEnumerable<Permission>> GetUserPermissions(Guid userId)
        {
            return await _permissionRepository.GetPermissionsByUserIdAsync(userId);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        private UserDto MapToUserDto(User user)
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

                UserRoles = user.UserRoles?.Select(ur => new UserRoleDto
                {
                    RoleId = ur.RoleId,
                    RoleName = ur.Role?.Name ?? "",
                    RoleDescription = ur.Role?.Description ?? ""
                }).ToList() ?? new List<UserRoleDto>(),

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