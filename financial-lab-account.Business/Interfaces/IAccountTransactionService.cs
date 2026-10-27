using financial_lab_account.Business.Models;

namespace financial_lab_account.Business.Interfaces;

public interface IAccountTransactionService
{
    Task<CommandResult> CreditAsync(Guid accountId, decimal amount, string endToEndId, string? description, CancellationToken ct);
    Task<CommandResult> DebitAsync(Guid accountId, decimal amount, string endToEndId, string? description, CancellationToken ct);
}
