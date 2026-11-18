using financial_lab_account.Business.Models;

namespace financial_lab_account.Business.Interfaces;

public interface IBalanceHoldService
{
    Task<HoldResult> PlaceAsync(Guid accountId, string? reference, decimal? amount, string? reason,
        DateTimeOffset? expiresAt, CancellationToken ct);

    Task<BalanceHold> ReleaseAsync(Guid accountId, Guid holdId, CancellationToken ct);
    Task<BalanceHold> GetAsync(Guid accountId, Guid holdId, CancellationToken ct);
    Task<IReadOnlyList<BalanceHold>> ListAsync(Guid accountId, string? status, CancellationToken ct);
    Task<int> ExpireDueAsync(int batchSize, CancellationToken ct);
}
