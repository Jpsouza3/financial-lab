using financial_lab_account.Api.Models;
using financial_lab_account.Business.Interfaces;

namespace financial_lab_account.Api.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounts");

        group.MapGet("/{id:guid}/balance", async (Guid id, IAccountRepository repository, CancellationToken ct) =>
            await repository.GetByIdAsync(id, ct) is { } account
                ? Results.Ok(AccountBalanceResponse.From(account))
                : Results.NotFound());
    }
}
