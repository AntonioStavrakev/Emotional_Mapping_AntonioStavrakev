using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Emotional_Mapping.Web.Services;

public class SmtpContactEmailService : IContactEmailService
{
    private readonly IConfiguration _config;

    public SmtpContactEmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string fromName, string fromEmail, string subject, string message)
    {
        var host = _config["Email:SmtpHost"];
        var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var username = _config["Email:Username"];
        var password = _config["Email:Password"];
        var to = _config["Email:To"];
        var enableSsl = bool.Parse(_config["Email:EnableSsl"] ?? "true");

        using var client = new SmtpClient(host!, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(username!, "GEOFEEL Contact"),
            Subject = string.IsNullOrWhiteSpace(subject) ? "Ново съобщение от GEOFEEL" : subject,
            Body =
                $@"Ново съобщение от контакт формата:

                    Име: {fromName}
                    Имейл: {fromEmail}

                    Съобщение:
                    {message}",
            IsBodyHtml = false
        };

        mail.To.Add(to!);
        mail.ReplyToList.Add(new MailAddress(fromEmail, fromName));

        await client.SendMailAsync(mail);
    }

    public async Task SendSystemEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var host = _config["Email:SmtpHost"];
        var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var username = _config["Email:Username"];
        var password = _config["Email:Password"];
        var enableSsl = bool.Parse(_config["Email:EnableSsl"] ?? "true");

        using var client = new SmtpClient(host!, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(username!, "GEOFEEL"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        mail.To.Add(toEmail);

        await client.SendMailAsync(mail);
    }
}