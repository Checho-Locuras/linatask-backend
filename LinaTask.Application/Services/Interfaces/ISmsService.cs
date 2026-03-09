namespace LinaTask.Application.Services.Interfaces
{
    public interface ISmsService
    {
        Task SendPasswordResetSmsAsync(string phoneNumber, string otpCode);
    }
}
