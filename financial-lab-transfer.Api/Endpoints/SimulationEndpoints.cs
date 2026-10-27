using financial_lab_transfer.Api.Filters;
using financial_lab_transfer.Api.Models;
using financial_lab_transfer.Business.Interfaces;
using financial_lab_transfer.Business.Models;
using Microsoft.Extensions.Options;

namespace financial_lab_transfer.Api.Endpoints;

public static class SimulationEndpoints
{
    private static readonly TransferStatus[] PendingStatuses =
        [TransferStatus.Debiting, TransferStatus.Crediting, TransferStatus.Refunding];

    public static void MapSimulationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/simulations").AddEndpointFilter<DomainExceptionFilter>();

        group.MapPost("/", async (CreateSimulationRequest body, ISimulationService service,
            IOptions<LabOptions> lab, CancellationToken ct) =>
        {
            var accountIds = body.AccountIds is { Count: > 0 } ids ? ids : lab.Value.AccountIds;
            var simulationId = await service.RunAsync(
                new SimulationRequest(body.Transfers, body.MinAmount, body.MaxAmount, accountIds), ct);
            return Results.Accepted($"/simulations/{simulationId}", new { simulationId });
        });

        group.MapGet("/{id:guid}", async (Guid id, ITransferRepository repository, CancellationToken ct) =>
        {
            var byStatus = await repository.CountByStatusAsync(id, ct);
            if (byStatus.Count == 0)
                return Results.NotFound();

            var finished = !PendingStatuses.Any(byStatus.ContainsKey);
            return Results.Ok(new SimulationSummary(id, byStatus.Values.Sum(), finished, byStatus));
        });
    }
}
