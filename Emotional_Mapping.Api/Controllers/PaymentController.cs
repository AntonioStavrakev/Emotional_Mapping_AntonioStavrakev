using Emotional_Mapping.Api.Payments;
using Emotional_Mapping.Application.Interfaces;
using Emotional_Mapping.Infrastructure.Identity;
using Emotional_Mapping.Infrastructure.Payments;
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
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;

    public PaymentController(IConfiguration config, UserManager<ApplicationUser> userManager)
    {
        _config = config;
        _userManager = userManager;
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
    }

    [Authorize]
    [HttpPost("create-checkout")]
    public async Task<IActionResult> CreateCheckout()
    {
        var domain = "http://localhost:5173"; // фронтенд URL

        var userId = User.FindFirst("sub")?.Value 
                     ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            Mode = "payment",
            SuccessUrl = domain + "/success",
            CancelUrl = domain + "/cancel",

            Metadata = new Dictionary<string, string>
            {
                { "userId", userId! }
            },

            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmount = 20,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "SuperUser Upgrade"
                        }
                    },
                    Quantity = 1
                }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return Ok(new { url = session.Url });
    }

    // 🔥 WEBHOOK (тук беше проблема ти с Events)
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        var secret = _config["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                secret
            );

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;

                var userId = session?.Metadata?["userId"];

                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _userManager.FindByIdAsync(userId);

                    if (user != null)
                    {
                        await _userManager.AddToRoleAsync(user, "SuperUser");
                    }
                }
            }

            return Ok();
        }
        catch
        {
            return BadRequest();
        }
    }
}