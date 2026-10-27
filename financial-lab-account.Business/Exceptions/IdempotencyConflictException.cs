namespace financial_lab_account.Business.Exceptions;

public class IdempotencyConflictException(string endToEndId, string existingOperation)
    : Exception($"EndToEndId {endToEndId} já foi usado em outra operação ({existingOperation}).")
{
    public string EndToEndId { get; } = endToEndId;
}
