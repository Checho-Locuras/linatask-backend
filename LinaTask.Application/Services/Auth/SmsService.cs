using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.Common.Utils;
using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace LinaTask.Application.Services.Auth
{
    public class SmsService : ISmsService
    {
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromPhone;

        public SmsService(IConfiguration configuration)
        {
            _accountSid = configuration["Twilio:AccountSid"]!;
            _authToken = configuration["Twilio:AuthToken"]!;
            _fromPhone = configuration["Twilio:FromPhone"]!;

            TwilioClient.Init(_accountSid, _authToken);
        }

        public async Task SendPasswordResetSmsAsync(string phoneNumber, string otpCode)
        {
            phoneNumber = PhoneHelper.NormalizeColombianPhone(phoneNumber);

            var messageBody =
                $"🔐 LinaTask\n" +
                $"Tu código de verificación es: {otpCode}\n" +
                $"Expira en 15 minutos.\n" +
                $"Si no solicitaste este código, ignora este mensaje.";

            try
            {
                await MessageResource.CreateAsync(
                    body: messageBody,
                    from: new PhoneNumber(_fromPhone),
                    to: new PhoneNumber(phoneNumber)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando SMS: {ex.Message}");
                throw new Exception("No se pudo enviar el SMS.");
            }
        }
    }
}
