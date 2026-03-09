using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.Common.Utils;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;

namespace LinaTask.Application.Services.Auth
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordResetTokenRepository _tokenRepository;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;


        public PasswordResetService(
            IUserRepository userRepository,
            IPasswordResetTokenRepository tokenRepository,
            IOtpService otpService,
            IEmailService emailService,
            IPasswordHasher<User> passwordHasher,
            ISmsService smsService)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _otpService = otpService;
            _emailService = emailService;
            _smsService = smsService;
        }

        public async Task<bool> RequestPasswordResetAsync(RequestPasswordResetDto request, string ipAddress, string userAgent)
        {
            // Buscar usuario por email o teléfono
            User? user = null;
            string deliveryDestination = "";

            if (request.DeliveryMethod == "email")
            {
                user = await _userRepository.GetByEmailAsync(request.EmailOrPhone);
                deliveryDestination = user?.Email ?? "";
            }
            else if (request.DeliveryMethod == "sms")
            {
                var normalizedPhone =
                    PhoneHelper.NormalizeColombianPhone(request.EmailOrPhone);

                user = await _userRepository.GetByPhoneAsync(normalizedPhone);
                deliveryDestination = normalizedPhone;
            }

            if (user == null)
            {
                // Por seguridad, no revelar si el usuario existe o no
                return true;
            }

            // Invalidar tokens anteriores
            await _tokenRepository.InvalidateAllUserTokensAsync(user.Id);

            // Generar nuevo OTP
            var otpCode = _otpService.GenerateOtp(6);

            // Crear token
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = otpCode,
                DeliveryMethod = request.DeliveryMethod,
                DeliveryDestination = deliveryDestination,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15), // 15 minutos de validez
                CreatedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await _tokenRepository.CreateAsync(resetToken);

            // Enviar OTP
            if (request.DeliveryMethod == "email")
            {
                await _emailService.SendPasswordResetEmailAsync(deliveryDestination, user.Name, otpCode);
            }
            else if (request.DeliveryMethod == "sms")
            {
                await _smsService.SendPasswordResetSmsAsync(deliveryDestination, otpCode);
            }

            return true;
        }

        public async Task<bool> VerifyOtpAsync(VerifyOtpDto request)
        {
            var user = await _userRepository.GetByEmailAsync(request.EmailOrPhone);
            if (user == null) return false;

            var validToken = await _tokenRepository.GetValidTokenAsync(user.Id, request.OtpCode);
            return validToken != null;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto request)
        {
            var user = await _userRepository.GetByEmailAsync(request.EmailOrPhone);
            if (user == null) return false;

            var validToken = await _tokenRepository.GetValidTokenAsync(user.Id, request.OtpCode);
            if (validToken == null) return false;

            // Actualizar contraseña
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.CreatedAt = user.CreatedAt.ToUniversalTime();
            await _userRepository.UpdateAsync(user);

            // Marcar token como usado
            validToken.IsUsed = true;
            validToken.UsedAt = DateTime.UtcNow.ToUniversalTime();
            validToken.CreatedAt = validToken.CreatedAt.ToUniversalTime();
            validToken.ExpiresAt = validToken.ExpiresAt.ToUniversalTime();
            await _tokenRepository.UpdateAsync(validToken);

            return true;
        }
    }
}