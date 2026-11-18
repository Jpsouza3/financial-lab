namespace financial_lab_antifraud.Api.Models;

public enum AnalysisStatus
{
    Pending,
    Approved,
    Rejected
}

public enum WebhookStatus
{
    Pending,
    Delivered,
    Failed,
    Skipped
}

public record AnalysisRequest(string TransactionId, decimal Amount, Guid SourceAccountId, Guid DestinationAccountId,
    string? Description, string CallbackUrl);

public record DeliveryAttempt(Guid DeliveryId, int Attempt, AnalysisStatus Decision, int? StatusCode, string? Error,
    DateTime At);

public record WebhookResponse(DeliveryMode Mode, WebhookStatus Status, IReadOnlyList<DeliveryAttempt> Attempts);

public record AnalysisResponse(Guid Id, string TransactionId, AnalysisStatus Status, string? Reason, string? RuleId,
    decimal Amount, Guid SourceAccountId, Guid DestinationAccountId, string? Description, DateTime CreatedAt,
    DateTime? DecidedAt, WebhookResponse Webhook);

public record WebhookPayload(Guid AnalysisId, string TransactionId, AnalysisStatus Decision, string? Reason,
    string? RuleId, DateTime DecidedAt);
