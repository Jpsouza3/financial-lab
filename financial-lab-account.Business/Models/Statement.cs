namespace financial_lab_account.Business.Models;

public record Statement(
    Guid AccountId,
    DateOnly From,
    DateOnly To,
    EntryType? Type,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal TotalCredits,
    decimal TotalDebits,
    IReadOnlyList<LedgerEntry> Items);
