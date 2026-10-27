namespace financial_lab_account.Business.Models;

public class LedgerEntry : Entity
{
    public const int EndToEndIdMaxLength = 32;
    public const int DescriptionMaxLength = 140;

    public Guid AccountId { get; private set; }
    public EntryType Type { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public string EndToEndId { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private LedgerEntry() { }

    internal LedgerEntry(Guid accountId, EntryType type, decimal amount, decimal balanceAfter,
        string endToEndId, string? description)
    {
        AccountId = accountId;
        Type = type;
        Amount = amount;
        BalanceAfter = balanceAfter;
        EndToEndId = endToEndId;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }
}
