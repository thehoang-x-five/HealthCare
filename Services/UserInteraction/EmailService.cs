using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using HealthCare.DTOs;
using Microsoft.Extensions.Options;

namespace HealthCare.Services.UserInteraction
{
    public class EmailService(IOptions<SmtpOptions> opt) : IEmailService
    {
        private readonly SmtpOptions _opt = opt.Value;

        public async Task SendEmailAsync(
            string to,
            string subject,
            string body,
            CancellationToken ct = default)
        {
            using var smtp = new SmtpClient(_opt.Host, _opt.Port)
            {
                Credentials = new NetworkCredential(_opt.User, _opt.Password),
                EnableSsl = _opt.EnableSsl
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(_opt.From, _opt.DisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(to);

            await smtp.SendMailAsync(mail, ct);
        }
    }
}
