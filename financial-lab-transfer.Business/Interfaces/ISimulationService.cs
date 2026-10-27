using financial_lab_transfer.Business.Models;

namespace financial_lab_transfer.Business.Interfaces;

public interface ISimulationService
{
    Task<Guid> RunAsync(SimulationRequest request, CancellationToken ct);
}
