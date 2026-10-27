namespace financial_lab_account.Contracts;

public record AccountDebited(Guid AccountId, Guid EntryId, string EndToEndId, decimal Amount, decimal BalanceAfter,
    DateTime OccurredAt);

public record AccountCredited(Guid AccountId, Guid EntryId, string EndToEndId, decimal Amount, decimal BalanceAfter,
    DateTime OccurredAt);

public record AccountDebitRejected(Guid AccountId, string EndToEndId, decimal Amount, string Reason);

public record AccountCreditRejected(Guid AccountId, string EndToEndId, decimal Amount, string Reason);
