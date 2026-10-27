using financial_lab_account.Contracts;
using financial_lab_transfer.Business.Interfaces;
using MassTransit;

namespace financial_lab_transfer.Api.Gateways;

public class AccountGateway(ISendEndpointProvider sendEndpointProvider) : IAccountGateway
{
    private static readonly Uri DebitQueue = new("queue:debit-account");
    private static readonly Uri CreditQueue = new("queue:credit-account");

    public async Task RequestDebitAsync(Guid accountId, decimal amount, string endToEndId, string? description,
        CancellationToken ct)
    {
        var endpoint = await sendEndpointProvider.GetSendEndpoint(DebitQueue);
        await endpoint.Send(new DebitAccount(accountId, amount, endToEndId, description), ct);
    }

    public async Task RequestCreditAsync(Guid accountId, decimal amount, string endToEndId, string? description,
        CancellationToken ct)
    {
        var endpoint = await sendEndpointProvider.GetSendEndpoint(CreditQueue);
        await endpoint.Send(new CreditAccount(accountId, amount, endToEndId, description), ct);
    }
}
