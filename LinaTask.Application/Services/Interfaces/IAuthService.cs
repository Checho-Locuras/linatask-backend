using LinaTask.Domain.Models.Login;

namespace LinaTask.Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<bool> RevokeTokenAsync(string userId);
    }
}