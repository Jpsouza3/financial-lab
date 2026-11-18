namespace financial_lab_transfer.Api.Messages;

public record AnalyzeTransfer(Guid TransferId, string EndToEndId, Guid SourceAccountId, Guid DestinationAccountId,
    decimal Amount, string? Description);
