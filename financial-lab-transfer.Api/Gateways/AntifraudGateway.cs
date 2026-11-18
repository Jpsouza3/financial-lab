using financial_lab_transfer.Api.Messages;
using financial_lab_transfer.Api.Models;
using financial_lab_transfer.Business.Interfaces;
using financial_lab_transfer.Business.Models;
using MassTransit;

namespace financial_lab_transfer.Api.Gateways;

public class AntifraudGateway(IPublishEndpoint publishEndpoint, AntifraudClient client, ILogger<AntifraudGateway> logger)
    : IAntifraudGateway
{
    public Task RequestAnalysisAsync(Transfer transfer, CancellationToken ct)
        => publishEndpoint.Publish(new AnalyzeTransfer(transfer.Id, transfer.EndToEndId, transfer.SourceAccountId,
            transfer.DestinationAccountId, transfer.Amount, transfer.Description), ct);

    public async Task<ScreeningDecision> FindDecisionAsync(string endToEndId, CancellationToken ct)
    {
        try
        {
            var analysis = await client.FindByTransactionAsync(endToEndId, ct);
            return analysis switch
            {
                null => new ScreeningDecision(ScreeningOutcome.NotFound),
                { Status: AntifraudDecision.Approved } => new ScreeningDecision(ScreeningOutcome.Approved),
                { Status: AntifraudDecision.Rejected } => new ScreeningDecision(ScreeningOutcome.Rejected,
                    analysis.Reason),
                _ => new ScreeningDecision(ScreeningOutcome.Pending)
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Antifraude indisponível ao consultar a análise de {EndToEndId}.", endToEndId);
            return new ScreeningDecision(ScreeningOutcome.Unavailable);
        }
    }
}
