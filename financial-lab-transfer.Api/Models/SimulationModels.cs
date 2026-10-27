using financial_lab_transfer.Business.Models;

namespace financial_lab_transfer.Api.Models;

public record CreateSimulationRequest(int Transfers, decimal MinAmount, decimal MaxAmount,
    IReadOnlyList<Guid>? AccountIds);

public record SimulationSummary(Guid SimulationId, int Total, bool Finished,
    IReadOnlyDictionary<TransferStatus, int> ByStatus);

public class LabOptions
{
    public List<Guid> AccountIds { get; set; } = [];
}
