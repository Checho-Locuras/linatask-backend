using LinaTask.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LinaTask.Domain.Models.Login
{
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        // =====================
        // DATOS BÁSICOS
        // =====================
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es requerido")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; } = string.Empty;

        // Lista de roles seleccionados (student, teacher, o ambos)
        [Required(ErrorMessage = "Debes seleccionar al menos un rol")]
        [MinLength(1, ErrorMessage = "Debes seleccionar al menos un rol")]
        public List<Guid> RoleIds { get; set; } = new();

        public DateTime? BirthDate { get; set; }
        public string? ProfilePhoto { get; set; }

        // =====================
        // PERFILES ACADÉMICOS (MÚLTIPLES)
        // =====================
        // Un perfil para estudiante, otro para profesor si selecciona ambos
        [Required(ErrorMessage = "Debes proporcionar al menos un perfil académico")]
        [MinLength(1, ErrorMessage = "Debes proporcionar al menos un perfil académico")]
        public List<AcademicProfileDto> AcademicProfiles { get; set; } = new();

        public List<UserAddressDto> UserAddresses { get; set; } = new();
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public UserDto User { get; set; } = null!;
    }

    public class RefreshTokenDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
