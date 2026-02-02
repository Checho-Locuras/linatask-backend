using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Domain.Models.Login;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LinaTask.Application.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            ILocationRepository locationRepository,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _locationRepository = locationRepository;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
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
            // 1. Verificar si el email ya existe
            var existingUser = await _userRepository.GetByEmailAsync(registerDto.Email);
            if (existingUser != null)
                throw new InvalidOperationException("El email ya está registrado");

            // 2. Verificar si el teléfono ya existe
            existingUser = await _userRepository.GetByPhoneAsync(registerDto.Phone);
            if (existingUser != null)
                throw new InvalidOperationException("El número de teléfono ya está registrado");

            // 3. Validar rol
            var validRoles = new[] { "student", "teacher", "admin" };
            if (!validRoles.Contains(registerDto.Role.ToLower()))
                throw new InvalidOperationException("Rol inválido");

            // 4. Validar que la institución existe
            var institution = await _locationRepository.GetInstitutionByIdAsync(registerDto.InstitutionId);
            if (institution == null)
                throw new InvalidOperationException("Institución no encontrada");

            // 5. Validar que la ciudad existe
            var city = await _locationRepository.GetCityByIdAsync(registerDto.CityId);
            if (city == null)
                throw new InvalidOperationException("Ciudad no encontrada");

            // 6. Crear nuevo usuario
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = registerDto.Name,
                PhoneNumber = registerDto.Phone,
                Email = registerDto.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Role = registerDto.Role.ToLower(),
                BirthDate = registerDto.BirthDate,
                ProfilePhoto = registerDto.ProfilePhoto,
                CreatedAt = DateTime.UtcNow.ToUniversalTime(),
                IsActive = true
            };

            // 7. Crear perfil académico inicial
            var academicProfile = new UserAcademicProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                InstitutionId = registerDto.InstitutionId,
                EducationLevel = registerDto.EducationLevel,
                CurrentSemester = registerDto.CurrentSemester,
                CurrentGrade = registerDto.CurrentGrade,
                GraduationYear = registerDto.GraduationYear,
                StudyArea = registerDto.StudyArea,
                AcademicStatus = registerDto.AcademicStatus,
                CreatedAt = DateTime.UtcNow.ToUniversalTime()
            };

            // 8. Crear dirección inicial
            var address = new UserAddress
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CityId = registerDto.CityId,
                Address = registerDto.Address,
                IsPrimary = true, // Primera dirección es primaria
                CreatedAt = DateTime.UtcNow.ToUniversalTime(),
            };

            // 9. Agregar relaciones
            user.AcademicProfiles.Add(academicProfile);
            user.Addresses.Add(address);

            // 10. Guardar en base de datos
            var createdUser = await _userRepository.CreateAsync(user);

            // 11. Generar tokens
            var token = GenerateJwtToken(createdUser);
            var refreshToken = GenerateRefreshToken();

            _logger.LogInformation("Usuario registrado exitosamente: {Email}", createdUser.Email);

            // 12. Retornar respuesta
            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                User = MapToUserDto(createdUser)
            };
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

            // Aquí deberías validar el refresh token contra tu base de datos
            // Por simplicidad, generamos nuevos tokens

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

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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
                Role = user.Role,
                Rating = user.Rating,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                ProfilePhoto = user.ProfilePhoto,
                PhoneNumber = user.PhoneNumber,
                BirthDate = user.BirthDate,
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
