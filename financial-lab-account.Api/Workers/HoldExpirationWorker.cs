using financial_lab_account.Business.Interfaces;

namespace financial_lab_account.Api.Workers;

public class HoldExpirationWorker(IServiceScopeFactory scopes, ILogger<HoldExpirationWorker> logger)
    : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IBalanceHoldService>();
                var expired = await service.ExpireDueAsync(BatchSize, stoppingToken);

                if (expired > 0)
                    logger.LogInformation("Expiração de bloqueios: {Count} bloqueio(s) expirado(s).", expired);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falha na expiração de bloqueios.");
            }
        }
    }
}
