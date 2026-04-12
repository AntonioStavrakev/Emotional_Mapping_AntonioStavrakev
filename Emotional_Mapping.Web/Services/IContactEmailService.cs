using System.Threading.Tasks;

namespace Emotional_Mapping.Web.Services;

public interface IContactEmailService
{
    Task SendAsync(string fromName, string fromEmail, string subject, string message);
    Task SendSystemEmailAsync(string toEmail, string subject, string htmlBody);
}
