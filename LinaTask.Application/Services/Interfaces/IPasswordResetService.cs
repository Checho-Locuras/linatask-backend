using LinaTask.Domain.DTOs;

namespace LinaTask.Application.Services.Interfaces
{
    public interface IPasswordResetService
    {
        Task<bool> RequestPasswordResetAsync(RequestPasswordResetDto request, string ipAddress, string userAgent);
        Task<bool> VerifyOtpAsync(VerifyOtpDto request);
        Task<bool> ResetPasswordAsync(ResetPasswordDto request);
    }
}
