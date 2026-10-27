namespace financial_lab_account.Business.Models;

public record CommandResult(ProcessedCommand Command, LedgerEntry? Entry)
{
    public bool IsApplied => Command.Status == CommandStatus.Applied;
}
