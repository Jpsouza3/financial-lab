using System.Text.Json;
using System.Text.Json.Serialization;
using financial_lab_transfer.Api.Messages;
using financial_lab_transfer.Api.Models;
using Microsoft.Extensions.Options;

namespace financial_lab_transfer.Api.Gateways;

public class AntifraudClient(HttpClient http, IOptions<AntifraudOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task SubmitAsync(AnalyzeTransfer message, CancellationToken ct)
    {
        var submission = new AnalysisSubmission(message.EndToEndId, message.Amount, message.SourceAccountId,
            message.DestinationAccountId, message.Description, options.Value.CallbackUrl);
        using var response = await http.PostAsJsonAsync("/analyses", submission, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<AnalysisSnapshot?> FindByTransactionAsync(string transactionId, CancellationToken ct)
    {
        var analyses = await http.GetFromJsonAsync<List<AnalysisSnapshot>>(
            $"/analyses?transactionId={Uri.EscapeDataString(transactionId)}", JsonOptions, ct);
        return analyses?.FirstOrDefault();
    }
}
