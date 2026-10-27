using financial_lab_account.Contracts;
using financial_lab_transfer.Business.Interfaces;
using financial_lab_transfer.Business.Models;
using MassTransit;

namespace financial_lab_transfer.Api.Consumers;

public class AccountEventsConsumer(ITransferService service, ILogger<AccountEventsConsumer> logger)
    : IConsumer<AccountDebited>, IConsumer<AccountDebitRejected>, IConsumer<AccountCredited>,
        IConsumer<AccountCreditRejected>
{
    public async Task Consume(ConsumeContext<AccountDebited> context)
    {
        var message = context.Message;
        Log(await service.HandleDebitedAsync(message.AccountId, message.EndToEndId, message.EntryId,
            message.OccurredAt, context.CancellationToken), message.EndToEndId);
    }

    public async Task Consume(ConsumeContext<AccountDebitRejected> context)
    {
        var message = context.Message;
        Log(await service.HandleDebitRejectedAsync(message.AccountId, message.EndToEndId, message.Reason,
            context.CancellationToken), message.EndToEndId);
    }

    public async Task Consume(ConsumeContext<AccountCredited> context)
    {
        var message = context.Message;
        Log(await service.HandleCreditedAsync(message.AccountId, message.EndToEndId, message.EntryId,
            message.OccurredAt, context.CancellationToken), message.EndToEndId);
    }

    public async Task Consume(ConsumeContext<AccountCreditRejected> context)
    {
        var message = context.Message;
        Log(await service.HandleCreditRejectedAsync(message.AccountId, message.EndToEndId, message.Reason,
            context.CancellationToken), message.EndToEndId);
    }

    private void Log(Transfer? transfer, string endToEndId)
    {
        if (transfer is null)
        {
            logger.LogDebug("Evento {EndToEndId} ignorado: não pertence a uma transferência deste serviço.",
                endToEndId);
            return;
        }

        var level = transfer.Status is TransferStatus.Failed or TransferStatus.Refunding or TransferStatus.Refunded
            or TransferStatus.RefundFailed
            ? LogLevel.Warning
            : LogLevel.Information;
        logger.Log(level, "Transferência {TransferId} ({EndToEndId}): {Status} {FailureReason}",
            transfer.Id, endToEndId, transfer.Status, transfer.FailureReason);
    }
}
