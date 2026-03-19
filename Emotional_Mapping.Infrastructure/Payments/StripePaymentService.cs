using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Emotional_Mapping.Infrastructure.Payments;

public class StripePaymentService
{
    private readonly IConfiguration _config;

    public StripePaymentService(IConfiguration config)
    {
        _config = config;
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
    }

    public async Task<string> CreateCheckoutSession(string userId)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            Mode = "payment",
            SuccessUrl = "https://localhost:5001/payment-success",
            CancelUrl = "https://localhost:5001/payment-cancel",
            Metadata = new Dictionary<string, string>
            {
                { "UserId", userId }
            },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmount = 20, // 0.20€
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

        return session.Url!;
    }
}