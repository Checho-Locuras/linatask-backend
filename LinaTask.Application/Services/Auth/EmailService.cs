using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace LinaTask.Application.Services.Auth
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailTemplateRepository _templateRepo;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration, IEmailTemplateRepository templateRepo)
        {
            _configuration = configuration;
            _templateRepo = templateRepo;
            _smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["Email:Username"] ?? "";
            _smtpPassword = _configuration["Email:Password"] ?? "";
            _fromEmail = _configuration["Email:FromEmail"] ?? "noreply@linatask.com";
            _fromName = _configuration["Email:FromName"] ?? "LinaTask";
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string otpCode)
        {
            await SendFromTemplateAsync(toEmail, "password_reset", new()
            {
                ["UserName"] = userName,
                ["OtpCode"] = otpCode
            });
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            await SendFromTemplateAsync(toEmail, "welcome", new()
            {
                ["UserName"] = userName,
                ["ActionUrl"] = "https://tudominio.com/auth/login"
            });
        }

        public async Task SendFromTemplateAsync(string toEmail, string templateKey, Dictionary<string, string> variables)
        {
            var template = await _templateRepo.GetByKeyAsync(templateKey)
                ?? throw new InvalidOperationException(
                    $"Email template '{templateKey}' not found or inactive.");

            // Sustituir variables: {{UserName}} → valor real
            var subject = ReplaceVariables(template.Subject, variables);
            var body = ReplaceVariables(template.HtmlBody, variables);

            await SendEmailAsync(toEmail, subject, body);
        }

        private static string ReplaceVariables(string template, Dictionary<string, string> variables)
        {
            foreach (var (key, value) in variables)
                template = template.Replace($"{{{{{key}}}}}", value);
            return template;
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
                };


                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log el error (implementar logging apropiado)
                Console.WriteLine($"Error enviando email: {ex.Message}");
                throw new Exception("No se pudo enviar el correo electrónico. Intenta nuevamente.");
            }
        }
    }
}
