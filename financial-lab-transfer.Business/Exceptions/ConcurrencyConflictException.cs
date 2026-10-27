namespace financial_lab_transfer.Business.Exceptions;

public class ConcurrencyConflictException(Guid transferId)
    : Exception($"A transferência {transferId} foi alterada por outro evento concorrente.")
{
    public Guid TransferId { get; } = transferId;
}
