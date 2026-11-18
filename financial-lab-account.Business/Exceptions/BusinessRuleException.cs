namespace financial_lab_account.Business.Exceptions;

public class BusinessRuleException(string message) : DomainException(message);
