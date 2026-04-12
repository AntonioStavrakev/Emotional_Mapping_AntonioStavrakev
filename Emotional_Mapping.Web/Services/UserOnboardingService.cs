using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Emotional_Mapping.Web.Services;

public class UserOnboardingService : IUserOnboardingService
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    private readonly IContactEmailService _emailService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UserOnboardingService> _logger;

    public UserOnboardingService(
        IContactEmailService emailService,
        IWebHostEnvironment environment,
        ILogger<UserOnboardingService> logger)
    {
        _emailService = emailService;
        _environment = environment;
        _logger = logger;
    }

    public async Task HandleNewRegistrationAsync(string email, string displayName, CancellationToken ct = default)
    {
        await SaveRegisteredUserAsync(email, displayName, ct);
        await SendWelcomeEmailSafeAsync(email, displayName);
    }

    private async Task SaveRegisteredUserAsync(string email, string displayName, CancellationToken ct)
    {
        var directory = Path.Combine(_environment.ContentRootPath, "App_Data");
        var filePath = Path.Combine(directory, "registered-users.json");

        Directory.CreateDirectory(directory);

        await FileLock.WaitAsync(ct);
        try
        {
            List<RegisteredUserEntry> users = new();

            if (File.Exists(filePath))
            {
                await using var readStream = File.OpenRead(filePath);
                users = await JsonSerializer.DeserializeAsync<List<RegisteredUserEntry>>(readStream, cancellationToken: ct)
                    ?? new List<RegisteredUserEntry>();
            }

            var existing = users.FirstOrDefault(x =>
                x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                users.Add(new RegisteredUserEntry
                {
                    Email = email,
                    DisplayName = displayName,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                existing.DisplayName = displayName;
            }

            await using var writeStream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(writeStream, users.OrderBy(x => x.DisplayName).ToList(),
                new JsonSerializerOptions { WriteIndented = true }, ct);
        }
        finally
        {
            FileLock.Release();
        }
    }

    private async Task SendWelcomeEmailSafeAsync(string email, string displayName)
    {
        try
        {
            var subject = "Добре дошъл в GEOFEEL";
            var htmlBody = $"""
<div style="font-family:Inter,Arial,sans-serif;max-width:620px;margin:0 auto;padding:32px;background:#f6f8f3;border-radius:24px;border:1px solid rgba(137,217,87,.18);">
  <div style="display:inline-block;padding:8px 14px;border-radius:999px;background:rgba(137,217,87,.16);color:#4f6d2e;font-size:12px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;">Welcome to GEOFEEL</div>
  <h1 style="margin:18px 0 10px;color:#223127;font-size:34px;line-height:1.05;">Здравей, {System.Net.WebUtility.HtmlEncode(displayName)}!</h1>
  <p style="margin:0 0 14px;color:#62705d;font-size:16px;line-height:1.7;">
    Профилът ти беше създаден успешно. Вече можеш да генерираш емоционални карти, да запазваш резултатите си и да откриваш места според настроението си.
  </p>
  <div style="margin:28px 0;">
    <a href="http://localhost:5202/Map/Generate" style="display:inline-block;padding:14px 24px;border-radius:14px;background:linear-gradient(90deg,#89d957,#c9e265);color:#1f2f15;text-decoration:none;font-weight:700;">
      Генерирай първата си карта
    </a>
  </div>
  <div style="padding:18px;border-radius:18px;background:#ffffff;border:1px solid rgba(81,104,43,.08);">
    <div style="font-weight:700;color:#223127;margin-bottom:6px;">Твоят профил</div>
    <div style="color:#62705d;font-size:14px;line-height:1.6;">
      Име: {System.Net.WebUtility.HtmlEncode(displayName)}<br/>
      Имейл: {System.Net.WebUtility.HtmlEncode(email)}
    </div>
  </div>
  <p style="margin:22px 0 0;color:#8a9486;font-size:13px;line-height:1.6;">
    Ако не си създавал този профил, просто отговори на този имейл.
  </p>
</div>
""";

            await _emailService.SendSystemEmailAsync(email, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Welcome email could not be sent to {Email}", email);
        }
    }

    private sealed class RegisteredUserEntry
    {
        public string Email { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
    }
}
