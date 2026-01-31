using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordResetController : ControllerBase
    {
        private readonly IPasswordResetService _passwordResetService;

        public PasswordResetController(IPasswordResetService passwordResetService)
        {
            _passwordResetService = passwordResetService;
        }

        [HttpPost("requestResetPassword")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetDto request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var result = await _passwordResetService.RequestPasswordResetAsync(request, ipAddress, userAgent);

            if (result)
            {
                return Ok(new
                {
                    message = $"Si el usuario existe, recibirás un código en tu {request.DeliveryMethod}",
                    success = true
                });
            }

            return BadRequest(new { message = "Error al procesar la solicitud", success = false });
        }

        [HttpPost("verifyOtp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto request)
        {
            var result = await _passwordResetService.VerifyOtpAsync(request);

            if (result)
            {
                return Ok(new { message = "Código verificado correctamente", success = true });
            }

            return BadRequest(new { message = "Código inválido o expirado", success = false });
        }

        [HttpPost("resetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            var result = await _passwordResetService.ResetPasswordAsync(request);

            if (result)
            {
                return Ok(new { message = "Contraseña restablecida correctamente", success = true });
            }

            return BadRequest(new { message = "Error al restablecer la contraseña. Verifica el código OTP", success = false });
        }
    }
}