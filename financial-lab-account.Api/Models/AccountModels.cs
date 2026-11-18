using financial_lab_account.Business.Models;

namespace financial_lab_account.Api.Models;

public record OpenAccountRequest(string? Name, string? Document, string? Branch, decimal? InitialDeposit);

public record AccountResponse(
    Guid AccountId,
    string Name,
    string Document,
    string Branch,
    string Number,
    AccountStatus Status,
    string? StatusReason,
    decimal Balance,
    decimal BlockedBalance,
    decimal AvailableBalance,
    DateTime OpenedAt)
{
    public static AccountResponse From(Account a)
        => new(a.Id, a.Name, a.Document, a.Branch, a.Number, a.Status, a.StatusReason, a.Balance, a.BlockedBalance,
            a.AvailableBalance, a.OpenedAt);
}

public record ChangeStatusRequest(string? Status, string? Reason);

public record ChangeStatusResponse(
    Guid AccountId,
    AccountStatus PreviousStatus,
    AccountStatus Status,
    string? Reason,
    DateTime? ChangedAt,
    bool Changed)
{
    public static ChangeStatusResponse From(StatusChange c)
        => new(c.Account.Id, c.PreviousStatus, c.Account.Status, c.Account.StatusReason, c.Account.StatusChangedAt,
            c.Changed);
}
