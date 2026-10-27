using financial_lab_account.Business.Exceptions;
using financial_lab_account.Business.Interfaces;
using financial_lab_account.Contracts;
using MassTransit;

namespace financial_lab_account.Api.Consumers;

public class DebitAccountConsumer(IAccountTransactionService service, ILogger<DebitAccountConsumer> logger)
    : IConsumer<DebitAccount>
{
    public async Task Consume(ConsumeContext<DebitAccount> context)
    {
        var message = context.Message;

        string reason;
        try
        {
            var result = await service.DebitAsync(message.AccountId, message.Amount, message.EndToEndId,
                message.Description, context.CancellationToken);

            if (result.Entry is { } entry)
            {
                await context.Publish(new AccountDebited(entry.AccountId, entry.Id, entry.EndToEndId, entry.Amount,
                    entry.BalanceAfter, entry.CreatedAt));
                return;
            }

            reason = result.Command.RejectionReason!;
        }
        catch (DomainException ex)
        {
            reason = ex.Message;
        }

        logger.LogWarning("Débito {EndToEndId} recusado na conta {AccountId}: {Reason}",
            message.EndToEndId, message.AccountId, reason);

        await context.Publish(new AccountDebitRejected(message.AccountId, message.EndToEndId, message.Amount, reason));
    }
}
