using financial_lab_account.Business.Models;

namespace financial_lab_account.Api.Models;

public record StatementSummary(decimal OpeningBalance, decimal ClosingBalance, decimal TotalCredits,
    decimal TotalDebits, decimal Net);

public record StatementItem(Guid EntryId, EntryType Type, decimal Amount, decimal BalanceAfter, string EndToEndId,
    string? Description, DateTime CreatedAt);

public record StatementResponse(
    Guid AccountId,
    DateOnly From,
    DateOnly To,
    EntryType? Type,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    StatementSummary Summary,
    IReadOnlyList<StatementItem> Items)
{
    public static StatementResponse Create(Statement s)
        => new(s.AccountId, s.From, s.To, s.Type, s.Page, s.PageSize, s.TotalItems, s.TotalPages,
            new StatementSummary(s.OpeningBalance, s.ClosingBalance, s.TotalCredits, s.TotalDebits,
                s.TotalCredits - s.TotalDebits),
            s.Items.Select(e => new StatementItem(e.Id, e.Type, e.Amount, e.BalanceAfter, e.EndToEndId,
                e.Description, e.CreatedAt)).ToList());
}
