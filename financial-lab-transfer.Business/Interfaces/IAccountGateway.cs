namespace financial_lab_transfer.Business.Interfaces;

public interface IAccountGateway
{
    Task RequestDebitAsync(Guid accountId, decimal amount, string endToEndId, string? description, CancellationToken ct);
    Task RequestCreditAsync(Guid accountId, decimal amount, string endToEndId, string? description, CancellationToken ct);
}
