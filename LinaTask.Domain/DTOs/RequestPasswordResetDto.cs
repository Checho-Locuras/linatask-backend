using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LinaTask.Domain.DTOs
{
    public class RequestPasswordResetDto
    {
        [Required(ErrorMessage = "El email o teléfono es requerido")]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "El método de envío es requerido")]
        [RegularExpression("^(email|sms)$", ErrorMessage = "El método debe ser 'email' o 'sms'")]
        public string DeliveryMethod { get; set; } = "email"; // "email" o "sms"
    }

    public class VerifyOtpDto
    {
        [Required(ErrorMessage = "El email o teléfono es requerido")]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código OTP es requerido")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener 6 dígitos")]
        public string OtpCode { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "El email o teléfono es requerido")]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código OTP es requerido")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener 6 dígitos")]
        public string OtpCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
