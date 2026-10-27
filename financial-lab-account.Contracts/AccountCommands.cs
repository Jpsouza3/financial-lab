namespace financial_lab_account.Contracts;

public record DebitAccount(Guid AccountId, decimal Amount, string EndToEndId, string? Description);

public record CreditAccount(Guid AccountId, decimal Amount, string EndToEndId, string? Description);
