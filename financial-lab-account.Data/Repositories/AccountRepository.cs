using financial_lab_account.Business.Exceptions;
using financial_lab_account.Business.Interfaces;
using financial_lab_account.Business.Models;
using financial_lab_account.Data.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace financial_lab_account.Data.Repositories;

public class AccountRepository(AccountDbContext context) : IAccountRepository
{
    private static readonly HashSet<string> IdempotencyIndexes =
    [
        "IX_processed_commands_AccountId_EndToEndId",
        "IX_ledger_entries_AccountId_EndToEndId"
    ];

    public Task<Account?> GetByIdAsync(Guid accountId, CancellationToken ct)
        => context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, ct);

    public Task<ProcessedCommand?> GetProcessedCommandAsync(Guid accountId, string endToEndId, CancellationToken ct)
        => context.ProcessedCommands
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.AccountId == accountId && c.EndToEndId == endToEndId, ct);

    public Task<LedgerEntry?> GetEntryAsync(Guid ledgerEntryId, CancellationToken ct)
        => context.LedgerEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == ledgerEntryId, ct);

    public void AddProcessedCommand(ProcessedCommand command) => context.ProcessedCommands.Add(command);

    public void AddEntry(LedgerEntry entry) => context.LedgerEntries.Add(entry);

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            context.ChangeTracker.Clear();
            var account = ex.Entries.Select(e => e.Entity).OfType<Account>().FirstOrDefault();
            throw new ConcurrencyConflictException(account?.Id ?? Guid.Empty);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                           {
                                               SqlState: PostgresErrorCodes.UniqueViolation,
                                               ConstraintName: { } constraint
                                           } && IdempotencyIndexes.Contains(constraint))
        {
            var command = context.ChangeTracker.Entries<ProcessedCommand>().Select(e => e.Entity).FirstOrDefault();
            context.ChangeTracker.Clear();
            throw new DuplicateEntryException(command?.AccountId ?? Guid.Empty, command?.EndToEndId ?? string.Empty);
        }
        catch
        {
            context.ChangeTracker.Clear();
            throw;
        }
    }
}
