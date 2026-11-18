namespace financial_lab_account.Business.Exceptions;

public class NotFoundException(string message) : DomainException(message);
