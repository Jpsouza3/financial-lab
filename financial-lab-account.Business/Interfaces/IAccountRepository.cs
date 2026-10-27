using financial_lab_account.Business.Models;

namespace financial_lab_account.Business.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid accountId, CancellationToken ct);
    Task<ProcessedCommand?> GetProcessedCommandAsync(Guid accountId, string endToEndId, CancellationToken ct);
    Task<LedgerEntry?> GetEntryAsync(Guid ledgerEntryId, CancellationToken ct);
    void AddProcessedCommand(ProcessedCommand command);
    void AddEntry(LedgerEntry entry);

    Task SaveChangesAsync(CancellationToken ct);
}
