using System.Text;
using System.Text.Json;
using financial_lab_transfer.Api.Models;
using financial_lab_transfer.Api.Webhooks;
using financial_lab_transfer.Business.Exceptions;
using financial_lab_transfer.Business.Interfaces;
using Microsoft.Extensions.Options;

namespace financial_lab_transfer.Api.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/webhooks");

        group.MapPost("/antifraud", async (HttpRequest request, ITransferService service,
            IOptions<AntifraudOptions> antifraud, IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> json,
            TimeProvider clock, ILoggerFactory loggers, CancellationToken ct) =>
        {
            var logger = loggers.CreateLogger("AntifraudWebhook");
            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync(ct);
            var deliveryId = request.Headers[WebhookSignature.DeliveryHeader].ToString();

            if (!WebhookSignature.IsValid(body, request.Headers[WebhookSignature.TimestampHeader],
                    request.Headers[WebhookSignature.SignatureHeader], antifraud.Value.WebhookSecret,
                    TimeSpan.FromSeconds(antifraud.Value.SignatureToleranceSeconds), clock.GetUtcNow()))
            {
                logger.LogWarning("Webhook {DeliveryId} recusado: assinatura inválida ou expirada.", deliveryId);
                return Results.Problem(title: "Assinatura inválida.", statusCode: StatusCodes.Status401Unauthorized);
            }

            AntifraudWebhook? payload;
            try
            {
                payload = JsonSerializer.Deserialize<AntifraudWebhook>(body, json.Value.SerializerOptions);
            }
            catch (JsonException)
            {
                payload = null;
            }

            if (payload is null || string.IsNullOrWhiteSpace(payload.TransactionId)
                                || payload.Decision is not (AntifraudDecision.Approved or AntifraudDecision.Rejected))
                return Results.Problem(title: "Payload inválido.", statusCode: StatusCodes.Status400BadRequest);

            try
            {
                var transfer = await service.HandleScreeningDecisionAsync(payload.TransactionId,
                    payload.Decision == AntifraudDecision.Approved, payload.Reason, ct);
                if (transfer is null)
                    return Results.NotFound();

                logger.LogInformation("Webhook {DeliveryId}: {Decision} para {EndToEndId}, transferência {Status}.",
                    deliveryId, payload.Decision, payload.TransactionId, transfer.Status);
                return Results.Ok(new { transferId = transfer.Id, status = transfer.Status });
            }
            catch (ConcurrencyConflictException)
            {
                return Results.Conflict();
            }
        });
    }
}
