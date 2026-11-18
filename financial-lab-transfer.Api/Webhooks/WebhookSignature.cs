using System.Security.Cryptography;
using System.Text;

namespace financial_lab_transfer.Api.Webhooks;

public static class WebhookSignature
{
    public const string DeliveryHeader = "X-Antifraud-Delivery";
    public const string TimestampHeader = "X-Antifraud-Timestamp";
    public const string SignatureHeader = "X-Antifraud-Signature";

    public static bool IsValid(string body, string? timestamp, string? signature, string secret, TimeSpan tolerance,
        DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signature) || !long.TryParse(timestamp, out var unix))
            return false;
        if (Math.Abs(now.ToUnixTimeSeconds() - unix) > tolerance.TotalSeconds)
            return false;

        var expected = "sha256=" + Convert.ToHexStringLower(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{body}")));
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }
}
