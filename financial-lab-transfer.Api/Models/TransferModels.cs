using financial_lab_transfer.Business.Models;

namespace financial_lab_transfer.Api.Models;

public record CreateTransferRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount,
    string? Description);

public record TransferResponse(Guid Id, Guid SourceAccountId, Guid DestinationAccountId, decimal Amount,
    string? Description, string EndToEndId, string? RefundEndToEndId, TransferStatus Status, string? FailureReason,
    Guid? SimulationId, DateTime CreatedAt, DateTime UpdatedAt, TransferLegResponse? Debit,
    TransferLegResponse? Credit, TransferLegResponse? Refund)
{
    public static TransferResponse From(Transfer t)
        => new(t.Id, t.SourceAccountId, t.DestinationAccountId, t.Amount, t.Description, t.EndToEndId,
            t.RefundEndToEndId, t.Status, t.FailureReason, t.SimulationId, t.CreatedAt, t.UpdatedAt,
            TransferLegResponse.From(t.SourceAccountId, t.EndToEndId, t.DebitEntryId, t.DebitedAt),
            TransferLegResponse.From(t.DestinationAccountId, t.EndToEndId, t.CreditEntryId, t.CreditedAt),
            TransferLegResponse.From(t.SourceAccountId, t.RefundEndToEndId, t.RefundEntryId, t.RefundedAt));
}

public record TransferLegResponse(Guid AccountId, string EndToEndId, Guid EntryId, DateTime OccurredAt)
{
    public static TransferLegResponse? From(Guid accountId, string? endToEndId, Guid? entryId, DateTime? occurredAt)
        => endToEndId is not null && entryId is { } id && occurredAt is { } at
            ? new TransferLegResponse(accountId, endToEndId, id, at)
            : null;
}
