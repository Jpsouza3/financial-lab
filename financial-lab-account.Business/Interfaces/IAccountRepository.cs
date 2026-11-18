using financial_lab_account.Business.Models;

namespace financial_lab_account.Business.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid accountId, CancellationToken ct);
    Task<bool> HasOpenAccountAsync(string document, CancellationToken ct);
    Task<int> GetLastNumberAsync(string branch, CancellationToken ct);
    Task<ProcessedCommand?> GetProcessedCommandAsync(Guid accountId, string endToEndId, CancellationToken ct);
    Task<LedgerEntry?> GetEntryAsync(Guid ledgerEntryId, CancellationToken ct);
    Task<BalanceHold?> GetHoldAsync(Guid accountId, Guid holdId, CancellationToken ct);
    Task<BalanceHold?> GetHoldByReferenceAsync(Guid accountId, string reference, CancellationToken ct);
    Task<int> CountActiveHoldsAsync(Guid accountId, CancellationToken ct);
    Task<IReadOnlyList<BalanceHold>> ListHoldsAsync(Guid accountId, HoldStatus? status, CancellationToken ct);
    Task<IReadOnlyList<Guid>> ListExpiredHoldIdsAsync(DateTime now, int take, CancellationToken ct);
    Task<BalanceHold?> GetHoldByIdAsync(Guid holdId, CancellationToken ct);
    Task<decimal?> GetLastBalanceBeforeAsync(Guid accountId, DateTime before, CancellationToken ct);
    Task<StatementTotals> GetStatementTotalsAsync(StatementQuery query, CancellationToken ct);
    Task<IReadOnlyList<LedgerEntry>> ListEntriesAsync(StatementQuery query, CancellationToken ct);
    void AddAccount(Account account);
    void AddProcessedCommand(ProcessedCommand command);
    void AddEntry(LedgerEntry entry);
    void AddHold(BalanceHold hold);

    Task SaveChangesAsync(CancellationToken ct);
}
