using financial_lab_transfer.Api.Gateways;
using financial_lab_transfer.Api.Messages;
using MassTransit;

namespace financial_lab_transfer.Api.Consumers;

public class AnalyzeTransferConsumer(AntifraudClient client, ILogger<AnalyzeTransferConsumer> logger)
    : IConsumer<AnalyzeTransfer>
{
    public async Task Consume(ConsumeContext<AnalyzeTransfer> context)
    {
        var message = context.Message;
        await client.SubmitAsync(message, context.CancellationToken);
        logger.LogInformation("Transferência {TransferId} ({EndToEndId}) enviada ao antifraude.", message.TransferId,
            message.EndToEndId);
    }
}
