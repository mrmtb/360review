using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace reviews.Services
{

    public interface IEmailService
    {
        Task SendEmailWithFromAsync(string recipient, string fromEmail, string subject, string textMessage, string htmlMessage, bool isTest);
    }

    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly bool IsDevelopment;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
            IsDevelopment = _configuration.GetValue<bool>("EmailSettings:IsDevelopment", true);
            _smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "default-smtp-server";
            _smtpPort = _configuration.GetValue<int>("EmailSettings:SmtpPort", 587);
            _smtpUser = _configuration["EmailSettings:SmtpUser"] ?? "default-user";
            _smtpPass = _configuration["EmailSettings:SmtpPass"] ?? "default-password";
        }

        public Task SendEmailAsync(string recipient, string subject, string htmlMessage)
        {
            if (IsDevelopment)
            {
                // Log email details in development mode
                Console.WriteLine($"Email sent to {recipient} with subject: {subject} and message: {htmlMessage}");
            }
            else
            {
                // Use SMTP to send the email in production
                var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(recipient);

                return client.SendMailAsync(mailMessage);
            }

            return Task.CompletedTask;
        }
    }

    public class EmailFromSender : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly bool IsDevelopment;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;

        public EmailFromSender(IConfiguration configuration)
        {
            _configuration = configuration;
            IsDevelopment = _configuration.GetValue<bool>("EmailSettings:IsDevelopment", true);
            _smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "default-smtp-server";
            _smtpPort = _configuration.GetValue<int>("EmailSettings:SmtpPort", 587);
            _smtpUser = _configuration["EmailSettings:SmtpUser"] ?? "default-user";
            _smtpPass = _configuration["EmailSettings:SmtpPass"] ?? "default-password";
        }

        public Task SendEmailWithFromAsync(string recipient, string fromEmail, string subject, string textMessage, string htmlMessage, bool isTest)
        {
            if (IsDevelopment)
            {
                // Log email details in development mode
                Console.WriteLine($"Email sent to {recipient} from {fromEmail} with subject: {subject} and message: {htmlMessage}");
            }
            else
            {
                // Use SMTP to send the email in production
                var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail), // Use the provided fromEmail
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(recipient);

                return client.SendMailAsync(mailMessage);
            }

            return Task.CompletedTask;
        }
    }
}