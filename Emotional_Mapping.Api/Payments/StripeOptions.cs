namespace Emotional_Mapping.Api.Payments;

public class StripeOptions
{
    public string SecretKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public long PriceCents { get; set; } = 20;
    public string Currency { get; set; } = "eur";
    public string SuccessUrl { get; set; } = "";
    public string CancelUrl { get; set; } = "";
}