using financial_lab_account.Api.Filters;
using financial_lab_account.Api.Models;
using financial_lab_account.Business.Interfaces;

namespace financial_lab_account.Api.Endpoints;

public static class HoldEndpoints
{
    public static void MapHoldEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounts/{accountId:guid}/holds").AddEndpointFilter<DomainExceptionFilter>();

        group.MapPost("/", async (Guid accountId, PlaceHoldRequest request, IBalanceHoldService service,
            CancellationToken ct) =>
        {
            var result = await service.PlaceAsync(accountId, request.Reference, request.Amount, request.Reason,
                request.ExpiresAt, ct);
            var body = HoldResponse.From(result.Hold);
            return result.Created
                ? Results.Created($"/accounts/{accountId}/holds/{result.Hold.Id}", body)
                : Results.Ok(body);
        });

        group.MapGet("/", async (Guid accountId, string? status, IBalanceHoldService service, CancellationToken ct) =>
            Results.Ok((await service.ListAsync(accountId, status, ct)).Select(HoldResponse.From)));

        group.MapGet("/{holdId:guid}", async (Guid accountId, Guid holdId, IBalanceHoldService service,
            CancellationToken ct) =>
            Results.Ok(HoldResponse.From(await service.GetAsync(accountId, holdId, ct))));

        group.MapDelete("/{holdId:guid}", async (Guid accountId, Guid holdId, IBalanceHoldService service,
            CancellationToken ct) =>
            Results.Ok(HoldResponse.From(await service.ReleaseAsync(accountId, holdId, ct))));
    }
}
