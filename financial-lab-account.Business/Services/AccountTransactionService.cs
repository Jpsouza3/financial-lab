using financial_lab_account.Business.Exceptions;
using financial_lab_account.Business.Interfaces;
using financial_lab_account.Business.Models;

namespace financial_lab_account.Business.Services;

public class AccountTransactionService(IAccountRepository repository) : IAccountTransactionService
{
    private const int MaxAttempts = 3;

    public Task<CommandResult> CreditAsync(Guid accountId, decimal amount, string endToEndId, string? description,
        CancellationToken ct)
        => ExecuteAsync(accountId, EntryType.Credit, amount, endToEndId, description,
            account => account.Credit(amount, endToEndId, description), ct);

    public Task<CommandResult> DebitAsync(Guid accountId, decimal amount, string endToEndId, string? description,
        CancellationToken ct)
        => ExecuteAsync(accountId, EntryType.Debit, amount, endToEndId, description,
            account => account.Debit(amount, endToEndId, description), ct);

    private async Task<CommandResult> ExecuteAsync(Guid accountId, EntryType type, decimal amount,
        string endToEndId, string? description, Func<Account, LedgerEntry> movement, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            var existing = await repository.GetProcessedCommandAsync(accountId, endToEndId, ct);
            if (existing is not null)
                return await ToResultAsync(existing, type, amount, ct);

            Account.ValidateMovement(amount, endToEndId, description);

            var account = await repository.GetByIdAsync(accountId, ct);
            var result = Apply(account, accountId, type, amount, endToEndId, movement);

            try
            {
                await repository.SaveChangesAsync(ct);
                return result;
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
            }
            catch (DuplicateEntryException)
            {
                var winner = await repository.GetProcessedCommandAsync(accountId, endToEndId, ct)
                             ?? throw new InvalidOperationException(
                                 $"Operação {endToEndId} da conta {accountId} não encontrada após violação de unicidade.");
                return await ToResultAsync(winner, type, amount, ct);
            }
        }
    }

    private CommandResult Apply(Account? account, Guid accountId, EntryType type, decimal amount,
        string endToEndId, Func<Account, LedgerEntry> movement)
    {
        if (account is null)
            return Reject($"Conta {accountId} não encontrada.");

        LedgerEntry entry;
        try
        {
            entry = movement(account);
        }
        catch (DomainException ex)
        {
            return Reject(ex.Message);
        }

        var applied = ProcessedCommand.Applied(entry);
        repository.AddEntry(entry);
        repository.AddProcessedCommand(applied);
        return new CommandResult(applied, entry);

        CommandResult Reject(string reason)
        {
            var rejected = ProcessedCommand.Rejected(accountId, endToEndId, type, amount, reason);
            repository.AddProcessedCommand(rejected);
            return new CommandResult(rejected, null);
        }
    }

    private async Task<CommandResult> ToResultAsync(ProcessedCommand existing, EntryType type, decimal amount,
        CancellationToken ct)
    {
        if (existing.Type != type || existing.Amount != amount)
            throw new IdempotencyConflictException(existing.EndToEndId, $"{existing.Type} de {existing.Amount}");

        if (existing.LedgerEntryId is not { } entryId)
            return new CommandResult(existing, null);

        var entry = await repository.GetEntryAsync(entryId, ct)
                    ?? throw new InvalidOperationException(
                        $"Lançamento {entryId} da operação {existing.EndToEndId} não encontrado.");
        return new CommandResult(existing, entry);
    }
}
