using financial_lab_account.Business.Exceptions;

namespace financial_lab_account.Business.Models;

public class Account : Entity
{
    public required string Name { get; init; }
    public required string Document { get; init; }
    public required string Branch { get; init; }
    public required string Number { get; init; }
    public AccountStatus Status { get; private set; } = AccountStatus.Active;
    public decimal Balance { get; private set; }
    public decimal BlockedBalance { get; private set; }
    public decimal AvailableBalance => Balance - BlockedBalance;
    public uint Version { get; private set; }

    public LedgerEntry Credit(decimal amount, string endToEndId, string? description = null)
    {
        EnsureCanMove(amount, endToEndId, description);

        Balance += amount;
        Version++;
        return new LedgerEntry(Id, EntryType.Credit, amount, Balance, endToEndId, description);
    }

    public LedgerEntry Debit(decimal amount, string endToEndId, string? description = null)
    {
        EnsureCanMove(amount, endToEndId, description);
        if (amount > AvailableBalance)
            throw new DomainException("Saldo insuficiente.");

        Balance -= amount;
        Version++;
        return new LedgerEntry(Id, EntryType.Debit, amount, Balance, endToEndId, description);
    }

    public static void ValidateMovement(decimal amount, string endToEndId, string? description)
    {
        if (amount <= 0)
            throw new DomainException("O valor deve ser maior que zero.");
        if (decimal.Round(amount, 2) != amount)
            throw new DomainException("O valor deve ter no máximo 2 casas decimais.");
        if (string.IsNullOrWhiteSpace(endToEndId))
            throw new DomainException("EndToEndId é obrigatório.");
        if (endToEndId.Length > LedgerEntry.EndToEndIdMaxLength)
            throw new DomainException($"EndToEndId deve ter no máximo {LedgerEntry.EndToEndIdMaxLength} caracteres.");
        if (description?.Length > LedgerEntry.DescriptionMaxLength)
            throw new DomainException($"A descrição deve ter no máximo {LedgerEntry.DescriptionMaxLength} caracteres.");
    }

    private void EnsureCanMove(decimal amount, string endToEndId, string? description)
    {
        ValidateMovement(amount, endToEndId, description);
        if (Status != AccountStatus.Active)
            throw new DomainException($"Conta {Status}: movimentação não permitida.");
    }
}