using System.Security.Cryptography;
using System.Text;
using financial_lab_antifraud.Api.Models;
using Microsoft.Extensions.Options;

namespace financial_lab_antifraud.Api.Webhooks;

public class WebhookSigner(IOptions<AntifraudOptions> options)
{
    public string Sign(string timestamp, string body)
        => "sha256=" + Convert.ToHexStringLower(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(options.Value.WebhookSecret),
            Encoding.UTF8.GetBytes($"{timestamp}.{body}")));
}
