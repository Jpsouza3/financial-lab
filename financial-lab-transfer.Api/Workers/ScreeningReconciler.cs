using financial_lab_transfer.Api.Models;
using financial_lab_transfer.Business.Interfaces;
using Microsoft.Extensions.Options;

namespace financial_lab_transfer.Api.Workers;

public class ScreeningReconciler(
    IServiceScopeFactory scopes,
    IOptions<AntifraudOptions> options,
    ILogger<ScreeningReconciler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.ReconcileIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<ITransferService>();
                var reconciled = await service.ReconcileScreeningAsync(
                    TimeSpan.FromSeconds(settings.StaleAfterSeconds), TimeSpan.FromSeconds(settings.TimeoutSeconds),
                    settings.ReconcileBatchSize, stoppingToken);

                if (reconciled > 0)
                    logger.LogInformation("Reconciliação do antifraude: {Count} transferência(s) resolvida(s).",
                        reconciled);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falha na reconciliação do antifraude.");
            }
        }
    }
}
