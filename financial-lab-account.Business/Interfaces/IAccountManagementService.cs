using financial_lab_account.Business.Models;

namespace financial_lab_account.Business.Interfaces;

public interface IAccountManagementService
{
    Task<Account> OpenAsync(string? name, string? document, string? branch, decimal? initialDeposit,
        CancellationToken ct);

    Task<Account> GetAsync(Guid accountId, CancellationToken ct);
    Task<StatusChange> ChangeStatusAsync(Guid accountId, string? status, string? reason, CancellationToken ct);
}
