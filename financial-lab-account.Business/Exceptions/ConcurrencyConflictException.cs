namespace financial_lab_account.Business.Exceptions;

public class ConcurrencyConflictException(Guid accountId)
    : Exception($"A conta {accountId} foi alterada por outra operação concorrente.")
{
    public Guid AccountId { get; } = accountId;
}
