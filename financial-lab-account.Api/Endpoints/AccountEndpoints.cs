using financial_lab_account.Api.Filters;
using financial_lab_account.Api.Models;
using financial_lab_account.Business.Interfaces;

namespace financial_lab_account.Api.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounts").AddEndpointFilter<DomainExceptionFilter>();

        group.MapPost("/", async (OpenAccountRequest request, IAccountManagementService service,
            CancellationToken ct) =>
        {
            var account = await service.OpenAsync(request.Name, request.Document, request.Branch,
                request.InitialDeposit, ct);
            return Results.Created($"/accounts/{account.Id}", AccountResponse.From(account));
        });

        group.MapGet("/{id:guid}", async (Guid id, IAccountManagementService service, CancellationToken ct) =>
            Results.Ok(AccountResponse.From(await service.GetAsync(id, ct))));

        group.MapGet("/{id:guid}/balance", async (Guid id, IAccountRepository repository, CancellationToken ct) =>
            await repository.GetByIdAsync(id, ct) is { } account
                ? Results.Ok(AccountBalanceResponse.From(account))
                : Results.NotFound());

        group.MapPatch("/{id:guid}/status", async (Guid id, ChangeStatusRequest request,
            IAccountManagementService service, CancellationToken ct) =>
            Results.Ok(ChangeStatusResponse.From(await service.ChangeStatusAsync(id, request.Status, request.Reason,
                ct))));

        group.MapGet("/{id:guid}/statement", async (Guid id, string? from, string? to, string? type, string? page,
            string? pageSize, IStatementService service, CancellationToken ct) =>
            Results.Ok(StatementResponse.Create(await service.GetAsync(id, from, to, type, page, pageSize, ct))));
    }
}
