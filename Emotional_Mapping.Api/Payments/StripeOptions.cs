namespace Emotional_Mapping.Api.Payments;

public class StripeOptions
{
    public string SecretKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public long PremiumPriceCents { get; set; } = 50;
    public long Bundle10PriceCents { get; set; } = 20;
    public int Bundle10Credits { get; set; } = 10;
    public int Bundle10ValidDays { get; set; } = 5;
    public string Currency { get; set; } = "eur";
    public string SuccessUrl { get; set; } = "";
    public string CancelUrl { get; set; } = "";
}
