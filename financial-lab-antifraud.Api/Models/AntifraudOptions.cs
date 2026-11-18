namespace financial_lab_antifraud.Api.Models;

public class AntifraudOptions
{
    public string WebhookSecret { get; set; } = "";
    public int DefaultDelayMs { get; set; } = 300;
    public List<string> AllowedCallbackHosts { get; set; } = [];
    public WebhookDeliveryOptions Delivery { get; set; } = new();
    public List<FraudRule> Rules { get; set; } = [];
}

public class WebhookDeliveryOptions
{
    public int MaxAttempts { get; set; } = 5;
    public int BaseBackoffMs { get; set; } = 200;
    public int TimeoutMs { get; set; } = 5000;
}
