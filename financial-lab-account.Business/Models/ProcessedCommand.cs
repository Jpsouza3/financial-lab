namespace financial_lab_account.Business.Models;

public class ProcessedCommand : Entity
{
    public Guid AccountId { get; private set; }
    public string EndToEndId { get; private set; } = null!;
    public EntryType Type { get; private set; }
    public decimal Amount { get; private set; }
    public CommandStatus Status { get; private set; }
    public Guid? LedgerEntryId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ProcessedCommand() { }

    private ProcessedCommand(Guid accountId, string endToEndId, EntryType type, decimal amount,
        CommandStatus status, Guid? ledgerEntryId, string? rejectionReason)
    {
        AccountId = accountId;
        EndToEndId = endToEndId;
        Type = type;
        Amount = amount;
        Status = status;
        LedgerEntryId = ledgerEntryId;
        RejectionReason = rejectionReason;
        CreatedAt = DateTime.UtcNow;
    }

    public static ProcessedCommand Applied(LedgerEntry entry)
        => new(entry.AccountId, entry.EndToEndId, entry.Type, entry.Amount, CommandStatus.Applied, entry.Id, null);

    public static ProcessedCommand Rejected(Guid accountId, string endToEndId, EntryType type, decimal amount,
        string reason)
        => new(accountId, endToEndId, type, amount, CommandStatus.Rejected, null, reason);
}
