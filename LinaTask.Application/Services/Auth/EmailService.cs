using LinaTask.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace LinaTask.Application.Services.Auth
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["Email:Username"] ?? "";
            _smtpPassword = _configuration["Email:Password"] ?? "";
            _fromEmail = _configuration["Email:FromEmail"] ?? "noreply@linatask.com";
            _fromName = _configuration["Email:FromName"] ?? "LinaTask";
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string otpCode)
        {
            var subject = "Restablece tu contraseña - LinaTask";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f8f9fa; padding: 30px; }}
                        .otp-code {{ background: white; border: 2px solid #667eea; border-radius: 10px; padding: 20px; text-align: center; margin: 20px 0; }}
                        .otp-code h2 {{ color: #667eea; font-size: 36px; margin: 0; letter-spacing: 8px; }}
                        .footer {{ background: #e9ecef; padding: 20px; text-align: center; font-size: 12px; color: #6c757d; border-radius: 0 0 10px 10px; }}
                        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🔐 Restablece tu Contraseña</h1>
                        </div>
                        <div class='content'>
                            <p>Hola <strong>{userName}</strong>,</p>
                            <p>Recibimos una solicitud para restablecer la contraseña de tu cuenta en LinaTask.</p>
                            <p>Tu código de verificación es:</p>
                            <div class='otp-code'>
                                <h2>{otpCode}</h2>
                            </div>
                            <p><strong>Este código expira en 15 minutos.</strong></p>
                            <div class='warning'>
                                ⚠️ <strong>Importante:</strong> Si no solicitaste este cambio, ignora este correo. Tu contraseña permanecerá segura.
                            </div>
                            <p>Si tienes algún problema, contáctanos en soporte@linatask.com</p>
                        </div>
                        <div class='footer'>
                            <p>© 2026 LinaTask - Plataforma de Tutorías</p>
                            <p>Este es un correo automático, por favor no respondas a este mensaje.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "¡Bienvenido a LinaTask!";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>¡Hola {userName}!</h2>
                    <p>Bienvenido a LinaTask. Estamos emocionados de tenerte con nosotros.</p>
                    <p>Explora nuestra plataforma y comienza a conectar con profesores expertos.</p>
                    <p>Saludos,<br>El equipo de LinaTask</p>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
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
