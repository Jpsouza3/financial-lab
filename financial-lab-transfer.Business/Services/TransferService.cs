using financial_lab_transfer.Business.Exceptions;
using financial_lab_transfer.Business.Interfaces;
using financial_lab_transfer.Business.Models;

namespace financial_lab_transfer.Business.Services;

public class TransferService(ITransferRepository repository, IAccountGateway accounts, IAntifraudGateway antifraud)
    : ITransferService
{
    public const string RejectedByAntifraudReason = "Recusada pelo antifraude.";
    public const string ScreeningTimeoutReason = "O antifraude não respondeu a tempo.";

    public async Task<IReadOnlyList<Transfer>> CreateAsync(IReadOnlyList<TransferRequest> requests,
        CancellationToken ct)
    {
        var transfers = requests.Select(Transfer.Create).ToList();
        foreach (var transfer in transfers)
        {
            repository.Add(transfer);
            await antifraud.RequestAnalysisAsync(transfer, ct);
        }

        await repository.SaveChangesAsync(ct);
        return transfers;
    }

    public async Task<Transfer?> HandleScreeningDecisionAsync(string endToEndId, bool approved, string? reason,
        CancellationToken ct)
    {
        var transfer = await repository.GetByEndToEndIdAsync(endToEndId, ct);
        if (transfer is null || transfer.EndToEndId != endToEndId)
            return null;

        if (approved)
        {
            if (transfer.ApproveScreening())
                await accounts.RequestDebitAsync(transfer.SourceAccountId, transfer.Amount, transfer.EndToEndId,
                    transfer.Description, ct);
        }
        else
        {
            transfer.RejectScreening(string.IsNullOrWhiteSpace(reason) ? RejectedByAntifraudReason : reason);
        }

        await repository.SaveChangesAsync(ct);
        return transfer;
    }

    public async Task<int> ReconcileScreeningAsync(TimeSpan staleAfter, TimeSpan timeout, int take,
        CancellationToken ct)
    {
        var now = UtcClock.Now;
        var stale = await repository.ListByStatusCreatedBeforeAsync(TransferStatus.Screening, now - staleAfter, take,
            ct);
        var reconciled = 0;

        foreach (var transfer in stale)
        {
            var decision = await antifraud.FindDecisionAsync(transfer.EndToEndId, ct);
            try
            {
                switch (decision.Outcome)
                {
                    case ScreeningOutcome.Approved or ScreeningOutcome.Rejected:
                        await HandleScreeningDecisionAsync(transfer.EndToEndId,
                            decision.Outcome == ScreeningOutcome.Approved, decision.Reason, ct);
                        reconciled++;
                        break;
                    case var _ when transfer.CreatedAt < now - timeout:
                        await HandleScreeningDecisionAsync(transfer.EndToEndId, false, ScreeningTimeoutReason, ct);
                        reconciled++;
                        break;
                    case ScreeningOutcome.NotFound:
                        await antifraud.RequestAnalysisAsync(transfer, ct);
                        await repository.SaveChangesAsync(ct);
                        break;
                }
            }
            catch (ConcurrencyConflictException)
            {
            }
        }

        return reconciled;
    }

    public Task<Transfer?> HandleDebitedAsync(Guid accountId, string endToEndId, Guid entryId, DateTime occurredAt,
        CancellationToken ct)
        => HandleAsync(accountId, endToEndId, async (transfer, leg) =>
        {
            if (leg == TransferLeg.Debit && transfer.ConfirmDebit(entryId, occurredAt))
                await accounts.RequestCreditAsync(transfer.DestinationAccountId, transfer.Amount,
                    transfer.EndToEndId, transfer.Description, ct);
        }, ct);

    public Task<Transfer?> HandleDebitRejectedAsync(Guid accountId, string endToEndId, string reason,
        CancellationToken ct)
        => HandleAsync(accountId, endToEndId, (transfer, leg) =>
        {
            if (leg == TransferLeg.Debit)
                transfer.RejectDebit(reason);
            return Task.CompletedTask;
        }, ct);

    public Task<Transfer?> HandleCreditedAsync(Guid accountId, string endToEndId, Guid entryId, DateTime occurredAt,
        CancellationToken ct)
        => HandleAsync(accountId, endToEndId, (transfer, leg) =>
        {
            if (leg == TransferLeg.Credit)
                transfer.ConfirmCredit(entryId, occurredAt);
            else if (leg == TransferLeg.Refund)
                transfer.ConfirmRefund(entryId, occurredAt);
            return Task.CompletedTask;
        }, ct);

    public Task<Transfer?> HandleCreditRejectedAsync(Guid accountId, string endToEndId, string reason,
        CancellationToken ct)
        => HandleAsync(accountId, endToEndId, async (transfer, leg) =>
        {
            if (leg == TransferLeg.Credit && transfer.RejectCredit(reason))
                await accounts.RequestCreditAsync(transfer.SourceAccountId, transfer.Amount,
                    transfer.RefundEndToEndId!, $"Devolução da transferência {transfer.Id}", ct);
            else if (leg == TransferLeg.Refund)
                transfer.RejectRefund(reason);
        }, ct);

    private async Task<Transfer?> HandleAsync(Guid accountId, string endToEndId,
        Func<Transfer, TransferLeg, Task> apply, CancellationToken ct)
    {
        var transfer = await repository.GetByEndToEndIdAsync(endToEndId, ct);
        if (transfer?.LegOf(accountId, endToEndId) is not { } leg)
            return null;

        await apply(transfer, leg);
        await repository.SaveChangesAsync(ct);
        return transfer;
    }
}
