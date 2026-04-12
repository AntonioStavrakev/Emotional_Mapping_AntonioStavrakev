using System.Threading;
using System.Threading.Tasks;

using System.Threading;
using System.Threading.Tasks;

namespace Emotional_Mapping.Web.Services;

public interface IUserOnboardingService
{
    Task HandleNewRegistrationAsync(string email, string displayName, CancellationToken ct = default);
}
