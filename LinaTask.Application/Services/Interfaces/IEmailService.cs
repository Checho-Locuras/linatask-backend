using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string userName, string otpCode);
        Task SendWelcomeEmailAsync(string toEmail, string userName);
        Task SendFromTemplateAsync(string toEmail, string templateKey,
            Dictionary<string, string> variables);
    }
}
