using financial_lab_transfer.Api.Filters;
using financial_lab_transfer.Api.Models;
using financial_lab_transfer.Business.Interfaces;
using financial_lab_transfer.Business.Models;

namespace financial_lab_transfer.Api.Endpoints;

public static class TransferEndpoints
{
    private const int MaxTake = 500;

    public static void MapTransferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/transfers").AddEndpointFilter<DomainExceptionFilter>();

        group.MapPost("/", async (CreateTransferRequest body, ITransferService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(
                [new TransferRequest(body.SourceAccountId, body.DestinationAccountId, body.Amount, body.Description)],
                ct);
            var transfer = created[0];
            return Results.Accepted($"/transfers/{transfer.Id}", TransferResponse.From(transfer));
        });

        group.MapGet("/{id:guid}", async (Guid id, ITransferRepository repository, CancellationToken ct) =>
            await repository.GetByIdAsync(id, ct) is { } transfer
                ? Results.Ok(TransferResponse.From(transfer))
                : Results.NotFound());

        group.MapGet("/", async (Guid? simulationId, TransferStatus? status, int? take,
            ITransferRepository repository, CancellationToken ct) =>
        {
            var transfers = await repository.ListAsync(simulationId, status, Math.Clamp(take ?? 50, 1, MaxTake), ct);
            return Results.Ok(transfers.Select(TransferResponse.From));
        });
    }
}
