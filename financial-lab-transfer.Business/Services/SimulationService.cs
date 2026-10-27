using financial_lab_transfer.Business.Exceptions;
using financial_lab_transfer.Business.Interfaces;
using financial_lab_transfer.Business.Models;

namespace financial_lab_transfer.Business.Services;

public class SimulationService(ITransferService transfers) : ISimulationService
{
    public const int MaxTransfers = 1000;

    public async Task<Guid> RunAsync(SimulationRequest request, CancellationToken ct)
    {
        var accountIds = request.AccountIds.Distinct().ToList();
        if (request.Transfers is < 1 or > MaxTransfers)
            throw new DomainException($"A quantidade de transferências deve estar entre 1 e {MaxTransfers}.");
        if (request.MinAmount < 0.01m || request.MaxAmount < request.MinAmount)
            throw new DomainException("O intervalo de valores deve ter mínimo >= 0,01 e máximo >= mínimo.");
        if (accountIds.Count < 2)
            throw new DomainException("A simulação precisa de pelo menos duas contas.");

        var simulationId = Guid.NewGuid();
        var requests = Enumerable.Range(1, request.Transfers).Select(i =>
        {
            var source = accountIds[Random.Shared.Next(accountIds.Count)];
            var destination = accountIds.Where(id => id != source).ElementAt(Random.Shared.Next(accountIds.Count - 1));
            return new TransferRequest(source, destination, RandomAmount(request.MinAmount, request.MaxAmount),
                $"Simulação {i}/{request.Transfers}", simulationId);
        }).ToList();

        await transfers.CreateAsync(requests, ct);
        return simulationId;
    }

    private static decimal RandomAmount(decimal min, decimal max)
    {
        var amount = min + (max - min) * (decimal)Random.Shared.NextDouble();
        return Math.Clamp(decimal.Round(amount, 2), min, max);
    }
}
