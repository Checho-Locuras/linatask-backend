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
            // Buscar usuario por email
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);

            if (user == null)
                throw new UnauthorizedAccessException("Credenciales inválidas");

            // Verificar contraseña
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Credenciales inválidas");

            // Verificar si el usuario está activo
            if (!user.IsActive)
                throw new UnauthorizedAccessException("Usuario inactivo");

            // Generar tokens
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
            try {
                // =====================
                // VALIDACIONES PREVIAS
                // =====================

                // 1. Verificar si el email ya existe
                var existingUser = await _userRepository.GetByEmailAsync(registerDto.Email);
                if (existingUser != null)
                    throw new InvalidOperationException("El email ya está registrado");

                // 2. Verificar si el teléfono ya existe
                existingUser = await _userRepository.GetByPhoneAsync(registerDto.Phone);
                if (existingUser != null)
                    throw new InvalidOperationException("El número de teléfono ya está registrado");

                // 3. Validar que no haya perfiles duplicados para el mismo rol
                ValidateNoDuplicateProfiles(registerDto);

                // =====================
                // VALIDAR Y CARGAR ROLES
                // =====================

                var roles = new List<Role>();
                if (registerDto.RoleIds == null || !registerDto.RoleIds.Any())
                {
                    throw new InvalidOperationException("Debes seleccionar al menos un rol");
                }

                foreach (var roleId in registerDto.RoleIds)
                {
                    var role = await _roleRepository.GetByIdAsync(roleId);
                    if (role == null)
                        throw new InvalidOperationException($"Rol con ID {roleId} no encontrado");
                    roles.Add(role);
                }

                // 4. Validar que los roles que requieren perfil académico lo tengan
                await ValidateAcademicProfiles(registerDto, roles);

                // =====================
                // VALIDAR UBICACIÓN E INSTITUCIÓN
                // =====================

                // 6. Validar que todas las instituciones en los perfiles existen
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

                // 7. Crear nuevo usuario
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

                // 8. Asignar roles al usuario
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
                // =====================

                // 9. Crear perfiles académicos (uno por cada rol que lo requiera)
                foreach (var profileDto in registerDto.AcademicProfiles)
                {
                    var academicProfile = new UserAcademicProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        InstitutionId = profileDto.InstitutionId,
                        EducationLevel = profileDto.EducationLevel,
                        CurrentSemester = profileDto.CurrentSemester,
                        CurrentGrade = profileDto.CurrentGrade,
                        GraduationYear = profileDto.GraduationYear,
                        StudyArea = profileDto.StudyArea,
                        AcademicStatus = profileDto.AcademicStatus,
                        CreatedAt = DateTime.UtcNow
                    };
                    user.AcademicProfiles.Add(academicProfile);
                }

                // =====================
                // CREAR DIRECCIONES
                // =====================

                // 10. Crear direcciones desde la lista del DTO
                foreach (var addressDto in registerDto.UserAddresses)
                {
                    // Validar que la ciudad existe
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

                // Validar que haya al menos una dirección primaria si hay direcciones
                if (registerDto.UserAddresses.Any() &&
                    !registerDto.UserAddresses.Any(a => a.IsPrimary))
                {
                    // Si no hay dirección primaria, marcar la primera como primaria
                    user.Addresses.First().IsPrimary = true;
                }
                // =====================
                // GUARDAR EN BASE DE DATOS
                // =====================

                // 11. Guardar en base de datos
                var createdUser = await _userRepository.CreateAsync(user);

                // =====================
                // GENERAR TOKENS
                // =====================

                // 12. Generar tokens
                var token = GenerateJwtToken(createdUser);
                var refreshToken = GenerateRefreshToken();

                _logger.LogInformation(
                    "Usuario registrado exitosamente: {Email} con roles: {Roles}",
                    createdUser.Email,
                    string.Join(", ", roles.Select(r => r.Name))
                );

                // =====================
                // RETORNAR RESPUESTA
                // =====================

                // 13. Retornar respuesta
                return new AuthResponseDto
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                    User = MapToUserDto(createdUser)
                };
            } catch(Exception e)
            {
                return null;
            }
            
        }

        // =====================================================
        // MÉTODOS DE VALIDACIÓN PRIVADOS
        // =====================================================

        /// <summary>
        /// Valida que no existan perfiles académicos duplicados para el mismo rol
        /// </summary>
        private void ValidateNoDuplicateProfiles(RegisterDto registerDto)
        {
            var duplicateRoles = registerDto.AcademicProfiles
                .GroupBy(p => p.RoleId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateRoles.Any())
            {
                throw new InvalidOperationException(
                    "No puedes tener múltiples perfiles académicos para el mismo rol"
                );
            }
        }

        /// <summary>
        /// Valida que los roles que requieren perfil académico lo tengan
        /// </summary>
        private async Task ValidateAcademicProfiles(RegisterDto registerDto, List<Role> roles)
        {
            // Roles que requieren perfil académico obligatorio
            var rolesRequiringProfile = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "student",
                    "teacher"
                };

            // Verificar cada rol seleccionado
            foreach (var role in roles)
            {
                // Si el rol requiere perfil académico
                if (rolesRequiringProfile.Contains(role.Name))
                {
                    // Verificar que exista un perfil para este rol
                    var hasProfile = registerDto.AcademicProfiles
                        .Any(p => p.RoleId == role.Id);

                    if (!hasProfile)
                    {
                        throw new InvalidOperationException(
                            $"El rol '{role.Name}' requiere un perfil académico completo"
                        );
                    }

                    // Obtener el perfil para validaciones adicionales
                    var profile = registerDto.AcademicProfiles
                        .First(p => p.RoleId == role.Id);

                    // Validaciones específicas según el nivel educativo
                    ValidateEducationLevelFields(profile, role.Name);
                }
            }

            // Validar que no haya perfiles para roles que no fueron seleccionados
            var selectedRoleIds = roles.Select(r => r.Id).ToHashSet();
            var profilesWithoutRole = registerDto.AcademicProfiles
                .Where(p => !selectedRoleIds.Contains(p.RoleId))
                .ToList();

            if (profilesWithoutRole.Any())
            {
                throw new InvalidOperationException(
                    "Hay perfiles académicos para roles que no fueron seleccionados"
                );
            }
        }

        /// <summary>
        /// Valida que los campos específicos del nivel educativo estén correctos
        /// </summary>
        private void ValidateEducationLevelFields(AcademicProfileDto profile, string roleName)
        {
            var educationLevel = profile.EducationLevel.ToLower();

            // Niveles que requieren grado (primaria y secundaria)
            var levelsRequiringGrade = new HashSet<string> { "primaria", "secundaria" };

            // Niveles que requieren semestre y área de estudio
            var levelsRequiringSemester = new HashSet<string>
                {
                    "bachillerato",
                    "universidad",
                    "tecnica",
                    "tecnologica",
                    "especializacion",
                    "maestria",
                    "doctorado"
                };

            if (levelsRequiringGrade.Contains(educationLevel))
            {
                // Primaria y Secundaria requieren grado
                if (string.IsNullOrWhiteSpace(profile.CurrentGrade))
                {
                    throw new InvalidOperationException(
                        $"El nivel '{profile.EducationLevel}' requiere especificar el grado actual"
                    );
                }

                // Validar que no tengan semestre ni área de estudio
                if (profile.CurrentSemester.HasValue || !string.IsNullOrWhiteSpace(profile.StudyArea))
                {
                    throw new InvalidOperationException(
                        $"El nivel '{profile.EducationLevel}' no debe tener semestre ni área de estudio"
                    );
                }
            }
            else if (levelsRequiringSemester.Contains(educationLevel))
            {
                // Educación superior requiere semestre y área de estudio
                if (!profile.CurrentSemester.HasValue || profile.CurrentSemester <= 0)
                {
                    throw new InvalidOperationException(
                        $"El nivel '{profile.EducationLevel}' requiere especificar el semestre actual (mayor a 0)"
                    );
                }

                if (string.IsNullOrWhiteSpace(profile.StudyArea))
                {
                    throw new InvalidOperationException(
                        $"El nivel '{profile.EducationLevel}' requiere especificar el área de estudio"
                    );
                }

                // Validar que no tengan grado
                if (!string.IsNullOrWhiteSpace(profile.CurrentGrade))
                {
                    throw new InvalidOperationException(
                        $"El nivel '{profile.EducationLevel}' no debe tener grado escolar"
                    );
                }

                // Validar rango de semestre razonable
                if (profile.CurrentSemester > 20)
                {
                    throw new InvalidOperationException(
                        "El semestre no puede ser mayor a 20"
                    );
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"Nivel educativo '{profile.EducationLevel}' no reconocido"
                );
            }

            // Validar estado académico
            var validStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "activo",
                    "graduado",
                    "inactivo"
                };

            if (!validStatuses.Contains(profile.AcademicStatus))
            {
                throw new InvalidOperationException(
                    $"Estado académico '{profile.AcademicStatus}' no válido. Valores permitidos: activo, graduado, inactivo"
                );
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            // Validar el token actual
            var principal = GetPrincipalFromExpiredToken(refreshTokenDto.Token);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Invalid token");

            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            // Generar nuevos tokens
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
            // Implementar lógica para invalidar refresh tokens en base de datos
            // Por ahora retornamos true
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

            // Agregar claims de roles
            if (user.UserRoles != null && user.UserRoles.Any())
            {
                foreach (var userRole in user.UserRoles)
                {
                    if (userRole.Role != null)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                    }
                }
            }

            // ===== NUEVO: Agregar claims de permisos =====
            var permissions = GetUserPermissions(user.Id).Result;
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission.Code));
            }
            // ==============================================

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Método auxiliar para obtener permisos del usuario
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
                ValidateLifetime = false // No validamos expiración aquí
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

                // Mapear roles
                UserRoles = user.UserRoles?.Select(ur => new UserRoleDto
                {
                    RoleId = ur.RoleId,
                    RoleName = ur.Role?.Name ?? "",
                    RoleDescription = ur.Role?.Description ?? ""
                }).ToList() ?? new List<UserRoleDto>(),

                // Mapear perfiles académicos
                AcademicProfiles = user.AcademicProfiles.Select(ap => new UserAcademicProfileDto
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
                }).ToList(),

                // Mapear direcciones
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
    }
}