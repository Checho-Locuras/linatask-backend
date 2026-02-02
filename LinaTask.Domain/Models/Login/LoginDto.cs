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

        [Required(ErrorMessage = "El rol es requerido")]
        public string Role { get; set; } = "student";

        public DateTime? BirthDate { get; set; }

        public string? ProfilePhoto { get; set; }

        // =====================
        // PERFIL ACADÉMICO
        // =====================
        [Required(ErrorMessage = "La institución es requerida")]
        public Guid InstitutionId { get; set; }

        [Required(ErrorMessage = "El nivel educativo es requerido")]
        [MaxLength(50)]
        public string EducationLevel { get; set; } = string.Empty;

        public int? CurrentSemester { get; set; }

        [MaxLength(20)]
        public string? CurrentGrade { get; set; }

        public int? GraduationYear { get; set; }

        [MaxLength(100)]
        public string? StudyArea { get; set; }

        [Required]
        [MaxLength(30)]
        public string AcademicStatus { get; set; } = "activo";

        // =====================
        // DIRECCIÓN
        // =====================
        [Required(ErrorMessage = "La ciudad es requerida")]
        public Guid CityId { get; set; }

        [Required(ErrorMessage = "La dirección es requerida")]
        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;
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
