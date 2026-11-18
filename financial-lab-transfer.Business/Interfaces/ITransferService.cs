using financial_lab_transfer.Business.Models;

namespace financial_lab_transfer.Business.Interfaces;

public interface ITransferService
{
    Task<IReadOnlyList<Transfer>> CreateAsync(IReadOnlyList<TransferRequest> requests, CancellationToken ct);
    Task<Transfer?> HandleScreeningDecisionAsync(string endToEndId, bool approved, string? reason,
        CancellationToken ct);
    Task<int> ReconcileScreeningAsync(TimeSpan staleAfter, TimeSpan timeout, int take, CancellationToken ct);
    Task<Transfer?> HandleDebitedAsync(Guid accountId, string endToEndId, Guid entryId, DateTime occurredAt,
        CancellationToken ct);
    Task<Transfer?> HandleDebitRejectedAsync(Guid accountId, string endToEndId, string reason, CancellationToken ct);
    Task<Transfer?> HandleCreditedAsync(Guid accountId, string endToEndId, Guid entryId, DateTime occurredAt,
        CancellationToken ct);
    Task<Transfer?> HandleCreditRejectedAsync(Guid accountId, string endToEndId, string reason, CancellationToken ct);
}
