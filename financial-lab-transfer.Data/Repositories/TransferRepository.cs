using financial_lab_transfer.Business.Exceptions;
using financial_lab_transfer.Business.Interfaces;
using financial_lab_transfer.Business.Models;
using financial_lab_transfer.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace financial_lab_transfer.Data.Repositories;

public class TransferRepository(TransferDbContext context) : ITransferRepository
{
    public Task<Transfer?> GetByIdAsync(Guid transferId, CancellationToken ct)
        => context.Transfers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transferId, ct);

    public Task<Transfer?> GetByEndToEndIdAsync(string endToEndId, CancellationToken ct)
        => context.Transfers.FirstOrDefaultAsync(t => t.EndToEndId == endToEndId || t.RefundEndToEndId == endToEndId,
            ct);

    public async Task<IReadOnlyList<Transfer>> ListAsync(Guid? simulationId, TransferStatus? status, int take,
        CancellationToken ct)
    {
        var query = context.Transfers.AsNoTracking();
        if (simulationId is not null)
            query = query.Where(t => t.SimulationId == simulationId);
        if (status is not null)
            query = query.Where(t => t.Status == status);

        return await query.OrderByDescending(t => t.CreatedAt).Take(take).ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<TransferStatus, int>> CountByStatusAsync(Guid simulationId,
        CancellationToken ct)
        => await context.Transfers
            .Where(t => t.SimulationId == simulationId)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, ct);

    public void Add(Transfer transfer) => context.Transfers.Add(transfer);

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            context.ChangeTracker.Clear();
            var transfer = ex.Entries.Select(e => e.Entity).OfType<Transfer>().FirstOrDefault();
            throw new ConcurrencyConflictException(transfer?.Id ?? Guid.Empty);
        }
        catch
        {
            context.ChangeTracker.Clear();
            throw;
        }
    }
}
