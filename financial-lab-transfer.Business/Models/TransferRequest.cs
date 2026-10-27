namespace financial_lab_transfer.Business.Models;

public record TransferRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount, string? Description,
    Guid? SimulationId = null);
