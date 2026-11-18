using financial_lab_transfer.Business.Models;

namespace financial_lab_transfer.Business.Interfaces;

public interface IAntifraudGateway
{
    Task RequestAnalysisAsync(Transfer transfer, CancellationToken ct);
    Task<ScreeningDecision> FindDecisionAsync(string endToEndId, CancellationToken ct);
}
