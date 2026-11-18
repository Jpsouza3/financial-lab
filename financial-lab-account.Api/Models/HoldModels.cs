using financial_lab_account.Business.Models;

namespace financial_lab_account.Api.Models;

public record PlaceHoldRequest(string? Reference, decimal? Amount, string? Reason, DateTimeOffset? ExpiresAt);

public record HoldResponse(
    Guid HoldId,
    Guid AccountId,
    string Reference,
    decimal Amount,
    string Reason,
    HoldStatus Status,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    DateTime? ReleasedAt)
{
    public static HoldResponse From(BalanceHold h)
        => new(h.Id, h.AccountId, h.Reference, h.Amount, h.Reason, h.Status, h.CreatedAt, h.ExpiresAt, h.ReleasedAt);
}
