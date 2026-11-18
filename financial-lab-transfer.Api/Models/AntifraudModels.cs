namespace financial_lab_transfer.Api.Models;

public class AntifraudOptions
{
    public string BaseUrl { get; set; } = "";
    public string CallbackUrl { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public int SignatureToleranceSeconds { get; set; } = 300;
    public int ReconcileIntervalSeconds { get; set; } = 15;
    public int StaleAfterSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 120;
    public int ReconcileBatchSize { get; set; } = 100;
}

public enum AntifraudDecision
{
    Pending,
    Approved,
    Rejected
}

public record AnalysisSubmission(string TransactionId, decimal Amount, Guid SourceAccountId, Guid DestinationAccountId,
    string? Description, string CallbackUrl);

public record AnalysisSnapshot(Guid Id, string TransactionId, AntifraudDecision Status, string? Reason);

public record AntifraudWebhook(Guid AnalysisId, string TransactionId, AntifraudDecision Decision, string? Reason,
    string? RuleId, DateTime DecidedAt);
