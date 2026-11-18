using financial_lab_antifraud.Api.Models;

namespace financial_lab_antifraud.Api.Analyses;

public class Analysis
{
    private readonly Lock gate = new();
    private readonly List<DeliveryAttempt> attempts = [];
    private readonly AnalysisStatus decision;

    private Analysis(AnalysisRequest request, FraudRule? rule, int defaultDelayMs)
    {
        Id = Guid.NewGuid();
        Request = request;
        RuleId = rule?.Id;
        decision = rule?.Decision == FraudDecision.Rejected ? AnalysisStatus.Rejected : AnalysisStatus.Approved;
        Reason = rule?.Reason ?? (decision == AnalysisStatus.Rejected ? $"Recusada pela regra {rule!.Id}." : null);
        DelayMs = rule?.DelayMs ?? defaultDelayMs;
        Delivery = rule?.Delivery ?? DeliveryMode.Normal;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; }
    public AnalysisRequest Request { get; }
    public string? RuleId { get; }
    public string? Reason { get; }
    public int DelayMs { get; }
    public DeliveryMode Delivery { get; }
    public DateTime CreatedAt { get; }
    public AnalysisStatus Status { get; private set; } = AnalysisStatus.Pending;
    public DateTime? DecidedAt { get; private set; }
    public WebhookStatus WebhookStatus { get; private set; } = WebhookStatus.Pending;

    public static Analysis Create(AnalysisRequest request, FraudRule? rule, int defaultDelayMs)
        => new(request, rule, defaultDelayMs);

    public void Decide()
    {
        lock (gate)
        {
            Status = decision;
            DecidedAt = DateTime.UtcNow;
        }
    }

    public WebhookPayload ToPayload(AnalysisStatus status, string? reason)
        => new(Id, Request.TransactionId, status, reason, RuleId, DecidedAt ?? DateTime.UtcNow);

    public void RecordAttempt(DeliveryAttempt attempt)
    {
        lock (gate)
            attempts.Add(attempt);
    }

    public void CompleteWebhook(WebhookStatus status)
    {
        lock (gate)
            WebhookStatus = status;
    }

    public AnalysisResponse ToResponse()
    {
        lock (gate)
        {
            var decided = Status != AnalysisStatus.Pending;
            return new AnalysisResponse(Id, Request.TransactionId, Status, decided ? Reason : null,
                decided ? RuleId : null, Request.Amount, Request.SourceAccountId, Request.DestinationAccountId,
                Request.Description, CreatedAt, DecidedAt,
                new WebhookResponse(Delivery, WebhookStatus, attempts.ToList()));
        }
    }
}
