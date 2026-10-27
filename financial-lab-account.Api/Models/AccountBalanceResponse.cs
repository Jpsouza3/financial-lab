using financial_lab_account.Business.Models;

namespace financial_lab_account.Api.Models;

public record AccountBalanceResponse(Guid AccountId, AccountStatus Status, decimal Balance, decimal BlockedBalance,
    decimal AvailableBalance, uint Version)
{
    public static AccountBalanceResponse From(Account a)
        => new(a.Id, a.Status, a.Balance, a.BlockedBalance, a.AvailableBalance, a.Version);
}
