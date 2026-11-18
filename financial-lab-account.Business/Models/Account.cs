using financial_lab_account.Business.Exceptions;

namespace financial_lab_account.Business.Models;

public class Account : Entity
{
    public const int MaxActiveHolds = 5;

    public required string Name { get; init; }
    public required string Document { get; init; }
    public required string Branch { get; init; }
    public required string Number { get; init; }
    public AccountStatus Status { get; private set; } = AccountStatus.Active;
    public string? StatusReason { get; private set; }
    public DateTime? StatusChangedAt { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public decimal Balance { get; private set; }
    public decimal BlockedBalance { get; private set; }
    public decimal AvailableBalance => Balance - BlockedBalance;
    public uint Version { get; private set; }

    public static Account Open(string name, string document, string branch, string number, DateTime now)
        => new() { Name = name, Document = document, Branch = branch, Number = number, OpenedAt = now };

    public LedgerEntry Credit(decimal amount, string endToEndId, string? description = null)
    {
        ValidateMovement(amount, endToEndId, description);
        if (Status == AccountStatus.Closed)
            throw new DomainException("Conta Closed: movimentação não permitida.");

        Balance += amount;
        Version++;
        return new LedgerEntry(Id, EntryType.Credit, amount, Balance, endToEndId, description);
    }

    public LedgerEntry Debit(decimal amount, string endToEndId, string? description = null)
    {
        ValidateMovement(amount, endToEndId, description);
        if (Status == AccountStatus.Closed)
            throw new DomainException("Conta Closed: movimentação não permitida.");
        if (Status == AccountStatus.Blocked)
            throw new DomainException("Conta Blocked: débito não permitido.");
        if (amount > AvailableBalance)
            throw new DomainException("Saldo insuficiente.");

        Balance -= amount;
        Version++;
        return new LedgerEntry(Id, EntryType.Debit, amount, Balance, endToEndId, description);
    }

    public bool ChangeStatus(AccountStatus target, string? reason, bool hasActiveHolds, DateTime now)
    {
        if (Status == AccountStatus.Closed)
            throw new ConflictException("Conta encerrada não pode ter o status alterado.");
        if (target == Status)
            return false;
        if (target == AccountStatus.Closed)
        {
            if (hasActiveHolds)
                throw new BusinessRuleException("A conta possui bloqueios de saldo ativos.");
            if (Balance != 0)
                throw new BusinessRuleException("A conta precisa estar com saldo zerado para ser encerrada.");
        }

        Status = target;
        StatusReason = reason;
        StatusChangedAt = now;
        Version++;
        return true;
    }

    public BalanceHold PlaceHold(string reference, decimal amount, string reason, DateTime? expiresAt,
        int activeHolds, DateTime now)
    {
        if (Status == AccountStatus.Closed)
            throw new BusinessRuleException("Conta encerrada não aceita bloqueio de saldo.");
        if (activeHolds >= MaxActiveHolds)
            throw new BusinessRuleException($"Limite de {MaxActiveHolds} bloqueios ativos atingido.");
        if (amount > AvailableBalance)
            throw new BusinessRuleException("Saldo disponível insuficiente para o bloqueio.");

        BlockedBalance += amount;
        Version++;
        return new BalanceHold(Id, reference, amount, reason, expiresAt, now);
    }

    public void ReleaseHold(BalanceHold hold, HoldStatus outcome, DateTime now)
    {
        if (hold.AccountId != Id || hold.Status != HoldStatus.Active)
            throw new InvalidOperationException($"Bloqueio {hold.Id} não está ativo na conta {Id}.");

        hold.Close(outcome, now);
        BlockedBalance -= hold.Amount;
        Version++;
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
}
