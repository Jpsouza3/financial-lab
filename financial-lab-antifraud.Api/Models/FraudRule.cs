namespace financial_lab_antifraud.Api.Models;

public enum FraudDecision
{
    Approved,
    Rejected
}

public enum DeliveryMode
{
    Normal,
    Duplicate,
    Drop,
    InvalidSignature,
    Flip
}

public class FraudRule
{
    public string Id { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public FraudDecision Decision { get; set; } = FraudDecision.Rejected;
    public string? Reason { get; set; }

    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public List<Guid>? SourceAccountIds { get; set; }
    public List<Guid>? DestinationAccountIds { get; set; }
    public string? DescriptionContains { get; set; }

    public int? DelayMs { get; set; }
    public DeliveryMode Delivery { get; set; } = DeliveryMode.Normal;
    public int SubmitFailures { get; set; }

    public bool Matches(AnalysisRequest request)
        => Enabled
           && (MinAmount is null || request.Amount >= MinAmount)
           && (MaxAmount is null || request.Amount <= MaxAmount)
           && (SourceAccountIds is not { Count: > 0 } || SourceAccountIds.Contains(request.SourceAccountId))
           && (DestinationAccountIds is not { Count: > 0 } || DestinationAccountIds.Contains(request.DestinationAccountId))
           && (string.IsNullOrEmpty(DescriptionContains)
               || (request.Description?.Contains(DescriptionContains, StringComparison.OrdinalIgnoreCase) ?? false));
}
