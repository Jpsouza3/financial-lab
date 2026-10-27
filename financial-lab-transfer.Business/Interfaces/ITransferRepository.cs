using financial_lab_transfer.Business.Models;

namespace financial_lab_transfer.Business.Interfaces;

public interface ITransferRepository
{
    Task<Transfer?> GetByIdAsync(Guid transferId, CancellationToken ct);

    Task<Transfer?> GetByEndToEndIdAsync(string endToEndId, CancellationToken ct);
    Task<IReadOnlyList<Transfer>> ListAsync(Guid? simulationId, TransferStatus? status, int take, CancellationToken ct);
    Task<IReadOnlyDictionary<TransferStatus, int>> CountByStatusAsync(Guid simulationId, CancellationToken ct);
    void Add(Transfer transfer);

    Task SaveChangesAsync(CancellationToken ct);
}
