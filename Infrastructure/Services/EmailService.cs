using BE.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace BE.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var smtpServer = _configuration["Email:SmtpServer"];
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var senderEmail = _configuration["Email:SenderEmail"];
        var senderName = _configuration["Email:SenderName"];
        var password = _configuration["Email:Password"];
        var enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");

        using (var client = new SmtpClient(smtpServer, smtpPort))
        {
            client.Credentials = new NetworkCredential(senderEmail, password);
            client.EnableSsl = enableSsl;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail!, senderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            // Gửi email bất đồng bộ
            await client.SendMailAsync(mailMessage);
        }
    }
}
