namespace financial_lab_transfer.Business.Models;

public enum ScreeningOutcome
{
    NotFound,
    Pending,
    Approved,
    Rejected,
    Unavailable
}

public record ScreeningDecision(ScreeningOutcome Outcome, string? Reason = null);
