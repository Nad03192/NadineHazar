using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Threading.Tasks;

namespace WebApplication8.Services  // Replace YourNamespace with your actual project namespace
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Your App Name", _configuration["EmailSettings:FromEmail"]));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_configuration["EmailSettings:SmtpServer"],
                                      int.Parse(_configuration["EmailSettings:Port"]), true);
            await client.AuthenticateAsync(_configuration["EmailSettings:Username"],
                                          _configuration["EmailSettings:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
