using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using financial_lab_antifraud.Api.Analyses;
using financial_lab_antifraud.Api.Models;
using Microsoft.Extensions.Options;

namespace financial_lab_antifraud.Api.Webhooks;

public class WebhookDispatcher(
    IHttpClientFactory httpClientFactory,
    WebhookSigner signer,
    IOptions<AntifraudOptions> options,
    ILogger<WebhookDispatcher> logger) : BackgroundService
{
    public const string HttpClientName = "webhooks";
    public const string DeliveryHeader = "X-Antifraud-Delivery";
    public const string TimestampHeader = "X-Antifraud-Timestamp";
    public const string SignatureHeader = "X-Antifraud-Signature";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Channel<Analysis> queue = Channel.CreateUnbounded<Analysis>();

    public void Enqueue(Analysis analysis) => queue.Writer.TryWrite(analysis);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var analysis in queue.Reader.ReadAllAsync(stoppingToken))
            _ = ProcessAsync(analysis, stoppingToken);
    }

    private async Task ProcessAsync(Analysis analysis, CancellationToken ct)
    {
        try
        {
            await Task.Delay(analysis.DelayMs, ct);
            analysis.Decide();

            if (analysis.Delivery == DeliveryMode.Drop)
            {
                analysis.CompleteWebhook(WebhookStatus.Skipped);
                logger.LogInformation("Análise {AnalysisId} ({TransactionId}): {Status}, webhook descartado.",
                    analysis.Id, analysis.Request.TransactionId, analysis.Status);
                return;
            }

            var deliveryId = Guid.NewGuid();
            var payload = analysis.ToPayload(analysis.Status, analysis.Reason);
            var delivered = await DeliverAsync(analysis, payload, deliveryId,
                analysis.Delivery == DeliveryMode.InvalidSignature, ct);

            if (analysis.Delivery == DeliveryMode.Duplicate)
                await DeliverAsync(analysis, payload, deliveryId, false, ct);

            if (analysis.Delivery == DeliveryMode.Flip)
            {
                var flipped = analysis.Status == AnalysisStatus.Approved
                    ? AnalysisStatus.Rejected
                    : AnalysisStatus.Approved;
                await DeliverAsync(analysis, analysis.ToPayload(flipped, "Decisão revista."), Guid.NewGuid(), false,
                    ct);
            }

            analysis.CompleteWebhook(delivered ? WebhookStatus.Delivered : WebhookStatus.Failed);
            logger.LogInformation("Análise {AnalysisId} ({TransactionId}): {Status}, webhook {WebhookStatus}.",
                analysis.Id, analysis.Request.TransactionId, analysis.Status, analysis.WebhookStatus);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            analysis.CompleteWebhook(WebhookStatus.Failed);
            logger.LogError(ex, "Falha ao processar a análise {AnalysisId}.", analysis.Id);
        }
    }

    private async Task<bool> DeliverAsync(Analysis analysis, WebhookPayload payload, Guid deliveryId,
        bool forgeSignature, CancellationToken ct)
    {
        var delivery = options.Value.Delivery;
        var client = httpClientFactory.CreateClient(HttpClientName);
        var body = JsonSerializer.Serialize(payload, JsonOptions);

        for (var attempt = 1; attempt <= delivery.MaxAttempts; attempt++)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            using var request = new HttpRequestMessage(HttpMethod.Post, analysis.Request.CallbackUrl);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            request.Headers.Add(DeliveryHeader, deliveryId.ToString());
            request.Headers.Add(TimestampHeader, timestamp);
            request.Headers.Add(SignatureHeader, forgeSignature ? "sha256=" + new string('0', 64) : signer.Sign(timestamp, body));

            int? statusCode = null;
            string? error = null;
            try
            {
                using var response = await client.SendAsync(request, ct);
                statusCode = (int)response.StatusCode;
                if (response.IsSuccessStatusCode)
                {
                    analysis.RecordAttempt(new DeliveryAttempt(deliveryId, attempt, payload.Decision, statusCode, null,
                        DateTime.UtcNow));
                    return true;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                error = ex.Message;
            }

            analysis.RecordAttempt(new DeliveryAttempt(deliveryId, attempt, payload.Decision, statusCode, error,
                DateTime.UtcNow));

            if (statusCode is { } code && !IsRetryable((HttpStatusCode)code))
                return false;

            if (attempt < delivery.MaxAttempts)
                await Task.Delay(TimeSpan.FromMilliseconds(delivery.BaseBackoffMs * Math.Pow(2, attempt - 1)), ct);
        }

        return false;
    }

    private static bool IsRetryable(HttpStatusCode code)
        => code is HttpStatusCode.RequestTimeout or HttpStatusCode.Conflict or (HttpStatusCode)425
            or HttpStatusCode.TooManyRequests || (int)code >= 500;
}
