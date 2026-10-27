namespace financial_lab_account.Business.Exceptions;

public class DuplicateEntryException(Guid accountId, string endToEndId)
    : Exception($"Já existe lançamento com EndToEndId {endToEndId} na conta {accountId}.")
{
    public Guid AccountId { get; } = accountId;
    public string EndToEndId { get; } = endToEndId;
}
