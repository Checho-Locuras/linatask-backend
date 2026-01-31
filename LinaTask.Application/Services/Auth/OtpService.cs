using LinaTask.Domain.Interfaces;
using System.Security.Cryptography;

namespace LinaTask.Application.Services.Auth
{
    public class OtpService : IOtpService
    {
        public string GenerateOtp(int length = 6)
        {
            const string chars = "0123456789";
            var otp = new char[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[length];
                rng.GetBytes(bytes);

                for (int i = 0; i < length; i++)
                {
                    otp[i] = chars[bytes[i] % chars.Length];
                }
            }

            return new string(otp);
        }

        public bool ValidateOtp(string providedOtp, string storedOtp)
        {
            return !string.IsNullOrWhiteSpace(providedOtp)
                && !string.IsNullOrWhiteSpace(storedOtp)
                && providedOtp.Trim() == storedOtp.Trim();
        }
    }
}
