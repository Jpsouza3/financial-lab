namespace financial_lab_account.Business.Models;

public class BalanceHold : Entity
{
    public const int ReferenceMaxLength = 32;
    public const int ReasonMaxLength = 200;

    public Guid AccountId { get; private set; }
    public string Reference { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = null!;
    public HoldStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }

    private BalanceHold() { }

    internal BalanceHold(Guid accountId, string reference, decimal amount, string reason, DateTime? expiresAt,
        DateTime now)
    {
        AccountId = accountId;
        Reference = reference;
        Amount = amount;
        Reason = reason;
        Status = HoldStatus.Active;
        CreatedAt = now;
        ExpiresAt = expiresAt;
    }

    internal void Close(HoldStatus status, DateTime now)
    {
        Status = status;
        ReleasedAt = now;
    }
}
