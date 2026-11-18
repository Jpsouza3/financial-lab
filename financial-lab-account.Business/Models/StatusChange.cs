namespace financial_lab_account.Business.Models;

public record StatusChange(Account Account, AccountStatus PreviousStatus, bool Changed);
