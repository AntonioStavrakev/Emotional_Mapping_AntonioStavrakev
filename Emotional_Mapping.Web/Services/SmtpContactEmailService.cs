using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Emotional_Mapping.Web.Services;

public class SmtpContactEmailService : IContactEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpContactEmailService> _logger;

    public SmtpContactEmailService(IConfiguration config, ILogger<SmtpContactEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string fromName, string fromEmail, string subject, string message)
    {
        var settings = GetSettings();
        var normalizedSubject = string.IsNullOrWhiteSpace(subject) ? "Ново съобщение от GEOFEEL" : subject.Trim();
        var normalizedName = string.IsNullOrWhiteSpace(fromName) ? "Анонимен потребител" : fromName.Trim();
        var normalizedMessage = string.IsNullOrWhiteSpace(message) ? "(празно съобщение)" : message.Trim();

        using var client = CreateClient(settings);
        using var mail = new MailMessage
        {
            From = new MailAddress(settings.Username, "GEOFEEL Contact"),
            Subject = normalizedSubject,
            Body =
                $@"Ново съобщение от контакт формата:

Име: {normalizedName}
Имейл: {fromEmail}

Съобщение:
{normalizedMessage}",
            IsBodyHtml = false
        };

        mail.To.Add(settings.To);
        mail.ReplyToList.Add(new MailAddress(fromEmail.Trim(), normalizedName));

        try
        {
            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed for contact form message to {Recipient}.", settings.To);
            throw;
        }
    }

    public async Task SendSystemEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var settings = GetSettings();

        using var client = CreateClient(settings);
        using var mail = new MailMessage
        {
            From = new MailAddress(settings.Username, "GEOFEEL"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        mail.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed for system email to {Recipient}.", toEmail);
            throw;
        }
    }

    private SmtpClient CreateClient(ContactEmailSettings settings)
    {
        return new SmtpClient(settings.Host, settings.Port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(settings.Username, settings.Password),
            EnableSsl = settings.EnableSsl
        };
    }

    private ContactEmailSettings GetSettings()
    {
        var host = GetRequiredSetting("Email:SmtpHost");
        var username = GetRequiredSetting("Email:Username");
        var password = GetRequiredSetting("Email:Password");
        var to = GetRequiredSetting("Email:To");

        var portValue = _config["Email:SmtpPort"];
        if (!int.TryParse(portValue, out var port))
        {
            port = 587;
        }

        var enableSslValue = _config["Email:EnableSsl"];
        var enableSsl = !bool.TryParse(enableSslValue, out var parsedEnableSsl) || parsedEnableSsl;

        return new ContactEmailSettings(host, port, username, password, to, enableSsl);
    }

    private string GetRequiredSetting(string key)
    {
        var value = _config[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        throw new InvalidOperationException($"Липсва задължителна email настройка: {key}");
    }

    private sealed record ContactEmailSettings(
        string Host,
        int Port,
        string Username,
        string Password,
        string To,
        bool EnableSsl);
}
