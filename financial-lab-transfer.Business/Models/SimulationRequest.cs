namespace financial_lab_transfer.Business.Models;

public record SimulationRequest(int Transfers, decimal MinAmount, decimal MaxAmount, IReadOnlyList<Guid> AccountIds);
