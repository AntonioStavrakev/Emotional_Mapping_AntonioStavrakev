using Emotional_Mapping.Api.Payments;
using Emotional_Mapping.Application.Interfaces.Repositories;
using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Emotional_Mapping.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly StripeOptions _stripeOptions;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAiCreditPackRepository _creditPacks;
    private readonly IUnitOfWork _uow;

    public PaymentController(
        IOptions<StripeOptions> stripeOptions,
        UserManager<ApplicationUser> userManager,
        IAiCreditPackRepository creditPacks,
        IUnitOfWork uow)
    {
        _stripeOptions = stripeOptions.Value;
        _userManager = userManager;
        _creditPacks = creditPacks;
        _uow = uow;
    }

    [Authorize]
    [HttpPost("create-checkout")]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutRequest? request)
    {
        EnsureStripeConfigured();

        StripeConfiguration.ApiKey = _stripeOptions.SecretKey;

        var appBaseUrl = GetAppBaseUrl();

        var userId = User.FindFirst("sub")?.Value 
                     ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Не е разпознат текущият потребител." });

        var productType = NormalizeProductType(request?.ProductType);
        var lineItem = BuildLineItem(productType);

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            Mode = "payment",
            SuccessUrl = ResolveReturnUrl(_stripeOptions.SuccessUrl,
                $"{appBaseUrl}/Home/PaymentSuccess?sessionId={{CHECKOUT_SESSION_ID}}"),
            CancelUrl = ResolveReturnUrl(_stripeOptions.CancelUrl,
                $"{appBaseUrl}/Home/PaymentCancel"),

            Metadata = new Dictionary<string, string>
            {
                { "userId", userId! },
                { "productType", productType }
            },

            LineItems = new List<SessionLineItemOptions>
            {
                lineItem
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return Ok(new { url = session.Url });
    }

    [Authorize]
    [HttpPost("confirm-session")]
    public async Task<IActionResult> ConfirmCheckout([FromQuery] string sessionId)
    {
        EnsureStripeConfigured();

        if (string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(new { message = "Липсва Stripe sessionId." });

        StripeConfiguration.ApiKey = _stripeOptions.SecretKey;

        var service = new SessionService();
        var session = await service.GetAsync(sessionId.Trim());

        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(session.Status, "complete", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Плащането все още не е потвърдено от Stripe." });
        }

        var userId = session.Metadata != null && session.Metadata.TryGetValue("userId", out var metadataUserId)
            ? metadataUserId
            : User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { message = "Stripe сесията не съдържа userId." });

        var result = await GrantPurchasedAccessAsync(session);
        return StatusCode(result.StatusCode, result.Payload);
    }

    // Stripe webhook за автоматично активиране при checkout.session.completed
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        EnsureStripeConfigured();

        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            StripeConfiguration.ApiKey = _stripeOptions.SecretKey;

            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripeOptions.WebhookSecret
            );

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null)
                    await GrantPurchasedAccessAsync(session);
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private void EnsureStripeConfigured()
    {
        if (string.IsNullOrWhiteSpace(_stripeOptions.SecretKey))
            throw new InvalidOperationException("Stripe SecretKey липсва. Добави Stripe:SecretKey в appsettings.json или в environment variable.");

        if (string.IsNullOrWhiteSpace(_stripeOptions.WebhookSecret))
            throw new InvalidOperationException("Stripe WebhookSecret липсва. Добави Stripe:WebhookSecret в appsettings.json или в environment variable.");
    }

    private string GetAppBaseUrl()
    {
        var origin = Request.Headers.Origin.ToString();
        if (!string.IsNullOrWhiteSpace(origin))
            return origin.TrimEnd('/');

        var referer = Request.Headers["Referer"].ToString();
        if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            return $"{refererUri.Scheme}://{refererUri.Authority}";

        return $"{Request.Scheme}://{Request.Host}";
    }

    private static string ResolveReturnUrl(string? configuredUrl, string fallbackUrl)
    {
        return string.IsNullOrWhiteSpace(configuredUrl)
            ? fallbackUrl
            : configuredUrl.Trim();
    }

    private string NormalizeProductType(string? productType)
    {
        return string.Equals(productType, "bundle10", StringComparison.OrdinalIgnoreCase)
            ? "bundle10"
            : "premium";
    }

    private SessionLineItemOptions BuildLineItem(string productType)
    {
        var currency = string.IsNullOrWhiteSpace(_stripeOptions.Currency)
            ? "eur"
            : _stripeOptions.Currency.Trim().ToLowerInvariant();

        var productName = productType == "bundle10"
            ? $"GEOFEEL 10 AI Requests ({_stripeOptions.Bundle10ValidDays} days)"
            : "GEOFEEL Premium";
        var unitAmount = productType == "bundle10"
            ? Math.Max(_stripeOptions.Bundle10PriceCents, 20)
            : Math.Max(_stripeOptions.PremiumPriceCents, 50);

        return new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = currency,
                UnitAmount = unitAmount,
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = productName
                }
            },
            Quantity = 1
        };
    }

    private async Task<(int StatusCode, object Payload)> GrantPurchasedAccessAsync(Session session)
    {
        var userId = session.Metadata != null && session.Metadata.TryGetValue("userId", out var metadataUserId)
            ? metadataUserId
            : null;

        if (string.IsNullOrWhiteSpace(userId))
            return (400, new { message = "Stripe сесията не съдържа userId." });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (404, new { message = "Потребителят от Stripe сесията не е намерен." });

        var productType = NormalizeProductType(session.Metadata != null && session.Metadata.TryGetValue("productType", out var metadataProductType)
            ? metadataProductType
            : null);

        if (productType == "bundle10")
        {
            if (!string.IsNullOrWhiteSpace(session.Id))
            {
                var existingPack = await _creditPacks.GetByStripeSessionIdAsync(session.Id, CancellationToken.None);
                if (existingPack != null)
                {
                    return (200, new
                    {
                        message = $"Пакетът с {existingPack.TotalCredits} AI заявки вече е активиран.",
                        productType,
                        extraCredits = existingPack.RemainingCredits,
                        expiresAtUtc = existingPack.ExpiresAtUtc
                    });
                }
            }

            var expiresAtUtc = DateTime.UtcNow.AddDays(Math.Max(_stripeOptions.Bundle10ValidDays, 1));
            var pack = new AiCreditPack(
                userId: user.Id,
                packageCode: "bundle10",
                totalCredits: Math.Max(_stripeOptions.Bundle10Credits, 1),
                expiresAtUtc: expiresAtUtc,
                source: "stripe",
                stripeSessionId: session.Id);

            await _creditPacks.AddAsync(pack, CancellationToken.None);
            await _uow.SaveChangesAsync(CancellationToken.None);

            return (200, new
            {
                message = $"Пакетът с {pack.TotalCredits} AI заявки е активиран до {pack.ExpiresAtUtc:dd.MM.yyyy}.",
                productType,
                extraCredits = pack.RemainingCredits,
                expiresAtUtc = pack.ExpiresAtUtc
            });
        }

        if (!await _userManager.IsInRoleAsync(user, "SuperUser"))
        {
            var result = await _userManager.AddToRoleAsync(user, "SuperUser");
            if (!result.Succeeded)
            {
                return (400, new
                {
                    message = string.Join(" ", result.Errors.Select(x => x.Description))
                });
            }
        }

        return (200, new { message = "Premium статусът е активиран.", productType, isSuperUser = true });
    }

    public sealed class CreateCheckoutRequest
    {
        public string? ProductType { get; set; }
    }
}
