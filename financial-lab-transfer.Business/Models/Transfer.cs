using financial_lab_transfer.Business.Exceptions;

namespace financial_lab_transfer.Business.Models;

public class Transfer : Entity
{
    public const int EndToEndIdMaxLength = 32;
    public const int DescriptionMaxLength = 140;
    public const int FailureReasonMaxLength = 400;

    public Guid SourceAccountId { get; private set; }
    public Guid DestinationAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }
    public string EndToEndId { get; private set; } = null!;
    public string? RefundEndToEndId { get; private set; }
    public TransferStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public Guid? SimulationId { get; private set; }

    public Guid? DebitEntryId { get; private set; }
    public DateTime? DebitedAt { get; private set; }
    public Guid? CreditEntryId { get; private set; }
    public DateTime? CreditedAt { get; private set; }
    public Guid? RefundEntryId { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public uint Version { get; private set; }

    private Transfer() { }

    public static Transfer Create(TransferRequest request)
    {
        if (request.SourceAccountId == request.DestinationAccountId)
            throw new DomainException("A conta de origem e a de destino devem ser diferentes.");
        if (request.Amount <= 0)
            throw new DomainException("O valor deve ser maior que zero.");
        if (decimal.Round(request.Amount, 2) != request.Amount)
            throw new DomainException("O valor deve ter no máximo 2 casas decimais.");
        if (request.Description?.Length > DescriptionMaxLength)
            throw new DomainException($"A descrição deve ter no máximo {DescriptionMaxLength} caracteres.");

        var now = DateTime.UtcNow;
        return new Transfer
        {
            SourceAccountId = request.SourceAccountId,
            DestinationAccountId = request.DestinationAccountId,
            Amount = request.Amount,
            Description = request.Description,
            EndToEndId = EndToEndIds.NewPayment(),
            Status = TransferStatus.Debiting,
            SimulationId = request.SimulationId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public TransferLeg? LegOf(Guid accountId, string endToEndId)
    {
        if (endToEndId == EndToEndId)
            return accountId == SourceAccountId ? TransferLeg.Debit
                : accountId == DestinationAccountId ? TransferLeg.Credit
                : null;

        return endToEndId == RefundEndToEndId && accountId == SourceAccountId ? TransferLeg.Refund : null;
    }

    public bool ConfirmDebit(Guid entryId, DateTime occurredAt)
    {
        if (!Move(TransferStatus.Debiting, TransferStatus.Crediting))
            return false;

        (DebitEntryId, DebitedAt) = (entryId, occurredAt);
        return true;
    }

    public bool RejectDebit(string reason) => Move(TransferStatus.Debiting, TransferStatus.Failed, reason);

    public bool ConfirmCredit(Guid entryId, DateTime occurredAt)
    {
        if (!Move(TransferStatus.Crediting, TransferStatus.Completed))
            return false;

        (CreditEntryId, CreditedAt) = (entryId, occurredAt);
        return true;
    }

    public bool RejectCredit(string reason)
    {
        if (!Move(TransferStatus.Crediting, TransferStatus.Refunding, reason))
            return false;

        RefundEndToEndId = EndToEndIds.NewRefund();
        return true;
    }

    public bool ConfirmRefund(Guid entryId, DateTime occurredAt)
    {
        if (!Move(TransferStatus.Refunding, TransferStatus.Refunded))
            return false;

        (RefundEntryId, RefundedAt) = (entryId, occurredAt);
        return true;
    }

    public bool RejectRefund(string reason)
        => Move(TransferStatus.Refunding, TransferStatus.RefundFailed, $"{FailureReason} Devolução recusada: {reason}");

    private bool Move(TransferStatus from, TransferStatus to, string? failureReason = null)
    {
        if (Status != from)
            return false;

        Status = to;
        if (failureReason is not null)
            FailureReason = failureReason.Length > FailureReasonMaxLength
                ? failureReason[..FailureReasonMaxLength]
                : failureReason;
        UpdatedAt = DateTime.UtcNow;
        Version++;
        return true;
    }
}
