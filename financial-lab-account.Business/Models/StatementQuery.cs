namespace financial_lab_account.Business.Models;

public record StatementQuery(Guid AccountId, DateTime Start, DateTime End, EntryType? Type, int Page, int PageSize);

public record StatementTotals(int Count, decimal Credits, decimal Debits);
