using financial_lab_account.Business.Exceptions;
using financial_lab_account.Business.Interfaces;
using financial_lab_account.Business.Models;
using financial_lab_account.Data.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace financial_lab_account.Data.Repositories;

public class AccountRepository(AccountDbContext context) : IAccountRepository
{
    private const string OpenDocumentIndex = "IX_accounts_Document";

    private static readonly HashSet<string> IdempotencyIndexes =
    [
        "IX_processed_commands_AccountId_EndToEndId",
        "IX_ledger_entries_AccountId_EndToEndId"
    ];

    private static readonly HashSet<string> RetryableIndexes =
    [
        "IX_accounts_Branch_Number",
        "IX_balance_holds_AccountId_Reference"
    ];

    public Task<Account?> GetByIdAsync(Guid accountId, CancellationToken ct)
        => context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, ct);

    public Task<bool> HasOpenAccountAsync(string document, CancellationToken ct)
        => context.Accounts.AnyAsync(a => a.Document == document && a.Status != AccountStatus.Closed, ct);

    public async Task<int> GetLastNumberAsync(string branch, CancellationToken ct)
    {
        var numbers = await context.Accounts
            .AsNoTracking()
            .Where(a => a.Branch == branch)
            .Select(a => a.Number)
            .ToListAsync(ct);

        return numbers.Select(n => int.TryParse(n, out var parsed) ? parsed : 0).DefaultIfEmpty(0).Max();
    }

    public Task<ProcessedCommand?> GetProcessedCommandAsync(Guid accountId, string endToEndId, CancellationToken ct)
        => context.ProcessedCommands
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.AccountId == accountId && c.EndToEndId == endToEndId, ct);

    public Task<LedgerEntry?> GetEntryAsync(Guid ledgerEntryId, CancellationToken ct)
        => context.LedgerEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == ledgerEntryId, ct);

    public Task<BalanceHold?> GetHoldAsync(Guid accountId, Guid holdId, CancellationToken ct)
        => context.BalanceHolds.FirstOrDefaultAsync(h => h.Id == holdId && h.AccountId == accountId, ct);

    public Task<BalanceHold?> GetHoldByIdAsync(Guid holdId, CancellationToken ct)
        => context.BalanceHolds.FirstOrDefaultAsync(h => h.Id == holdId, ct);

    public Task<BalanceHold?> GetHoldByReferenceAsync(Guid accountId, string reference, CancellationToken ct)
        => context.BalanceHolds
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.AccountId == accountId && h.Reference == reference, ct);

    public Task<int> CountActiveHoldsAsync(Guid accountId, CancellationToken ct)
        => context.BalanceHolds.CountAsync(h => h.AccountId == accountId && h.Status == HoldStatus.Active, ct);

    public async Task<IReadOnlyList<BalanceHold>> ListHoldsAsync(Guid accountId, HoldStatus? status,
        CancellationToken ct)
        => await context.BalanceHolds
            .AsNoTracking()
            .Where(h => h.AccountId == accountId && (status == null || h.Status == status))
            .OrderBy(h => h.CreatedAt)
            .ThenBy(h => h.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> ListExpiredHoldIdsAsync(DateTime now, int take, CancellationToken ct)
        => await context.BalanceHolds
            .AsNoTracking()
            .Where(h => h.Status == HoldStatus.Active && h.ExpiresAt != null && h.ExpiresAt <= now)
            .OrderBy(h => h.ExpiresAt)
            .Select(h => h.Id)
            .Take(take)
            .ToListAsync(ct);

    public Task<decimal?> GetLastBalanceBeforeAsync(Guid accountId, DateTime before, CancellationToken ct)
        => context.LedgerEntries
            .AsNoTracking()
            .Where(e => e.AccountId == accountId && e.CreatedAt < before)
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Select(e => (decimal?)e.BalanceAfter)
            .FirstOrDefaultAsync(ct);

    public async Task<StatementTotals> GetStatementTotalsAsync(StatementQuery query, CancellationToken ct)
    {
        var totals = await Filter(query)
            .GroupBy(e => e.Type)
            .Select(g => new { Type = g.Key, Count = g.Count(), Sum = g.Sum(e => e.Amount) })
            .ToListAsync(ct);

        return new StatementTotals(
            totals.Sum(t => t.Count),
            totals.Where(t => t.Type == EntryType.Credit).Sum(t => t.Sum),
            totals.Where(t => t.Type == EntryType.Debit).Sum(t => t.Sum));
    }

    public async Task<IReadOnlyList<LedgerEntry>> ListEntriesAsync(StatementQuery query, CancellationToken ct)
    {
        var skip = (long)(query.Page - 1) * query.PageSize;
        if (skip > int.MaxValue)
            return [];

        return await Filter(query)
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Skip((int)skip)
            .Take(query.PageSize)
            .ToListAsync(ct);
    }

    public void AddAccount(Account account) => context.Accounts.Add(account);

    public void AddProcessedCommand(ProcessedCommand command) => context.ProcessedCommands.Add(command);

    public void AddEntry(LedgerEntry entry) => context.LedgerEntries.Add(entry);

    public void AddHold(BalanceHold hold) => context.BalanceHolds.Add(hold);

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
        catch (DbUpdateException ex) when (UniqueViolation(ex) is { } constraint)
        {
            var command = context.ChangeTracker.Entries<ProcessedCommand>().Select(e => e.Entity).FirstOrDefault();
            var account = context.ChangeTracker.Entries<Account>().Select(e => e.Entity).FirstOrDefault();
            context.ChangeTracker.Clear();

            if (IdempotencyIndexes.Contains(constraint))
                throw new DuplicateEntryException(command?.AccountId ?? Guid.Empty, command?.EndToEndId ?? string.Empty);
            if (constraint == OpenDocumentIndex)
                throw new ConflictException("Já existe uma conta não encerrada para este documento.");
            if (RetryableIndexes.Contains(constraint))
                throw new ConcurrencyConflictException(account?.Id ?? Guid.Empty);
            throw;
        }
        catch
        {
            context.ChangeTracker.Clear();
            throw;
        }
    }

    private IQueryable<LedgerEntry> Filter(StatementQuery query)
        => context.LedgerEntries
            .AsNoTracking()
            .Where(e => e.AccountId == query.AccountId && e.CreatedAt >= query.Start && e.CreatedAt < query.End &&
                        (query.Type == null || e.Type == query.Type));

    private static string? UniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: { } constraint
        }
            ? constraint
            : null;
}
