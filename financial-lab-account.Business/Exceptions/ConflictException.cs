namespace financial_lab_account.Business.Exceptions;

public class ConflictException(string message) : DomainException(message);
