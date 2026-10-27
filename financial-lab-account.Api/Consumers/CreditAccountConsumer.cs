using financial_lab_account.Business.Exceptions;
using financial_lab_account.Business.Interfaces;
using financial_lab_account.Contracts;
using MassTransit;

namespace financial_lab_account.Api.Consumers;

public class CreditAccountConsumer(IAccountTransactionService service, ILogger<CreditAccountConsumer> logger)
    : IConsumer<CreditAccount>
{
    public async Task Consume(ConsumeContext<CreditAccount> context)
    {
        var message = context.Message;

        string reason;
        try
        {
            var result = await service.CreditAsync(message.AccountId, message.Amount, message.EndToEndId,
                message.Description, context.CancellationToken);

            if (result.Entry is { } entry)
            {
                await context.Publish(new AccountCredited(entry.AccountId, entry.Id, entry.EndToEndId, entry.Amount,
                    entry.BalanceAfter, entry.CreatedAt));
                return;
            }

            reason = result.Command.RejectionReason!;
        }
        catch (DomainException ex)
        {
            reason = ex.Message;
        }

        logger.LogWarning("Crédito {EndToEndId} recusado na conta {AccountId}: {Reason}",
            message.EndToEndId, message.AccountId, reason);

        await context.Publish(new AccountCreditRejected(message.AccountId, message.EndToEndId, message.Amount, reason));
    }
}
