using financial_lab_antifraud.Api.Analyses;
using financial_lab_antifraud.Api.Models;
using financial_lab_antifraud.Api.Rules;
using financial_lab_antifraud.Api.Webhooks;
using Microsoft.Extensions.Options;

namespace financial_lab_antifraud.Api.Endpoints;

public static class AnalysisEndpoints
{
    private const int MaxTake = 500;
    private const int TransactionIdMaxLength = 64;

    public static void MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/analyses");

        group.MapPost("/", (AnalysisRequest body, AnalysisStore store, RuleStore rules, WebhookDispatcher dispatcher,
            IOptions<AntifraudOptions> options) =>
        {
            var errors = Validate(body);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);
            if (!IsAllowedCallback(body.CallbackUrl, options.Value))
                return Results.Problem(title: "Callback não permitido.",
                    detail: $"O host de '{body.CallbackUrl}' não está em AllowedCallbackHosts.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);

            if (store.FindByTransaction(body.TransactionId) is { } existing)
                return Results.Ok(existing.ToResponse());

            var rule = rules.Match(body);
            if (rule is { SubmitFailures: > 0 } && store.ShouldFailSubmit(body.TransactionId, rule.SubmitFailures))
                return Results.Problem(title: "Antifraude indisponível.",
                    detail: $"Falha simulada pela regra {rule.Id}.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            var analysis = Analysis.Create(body, rule, options.Value.DefaultDelayMs);
            var stored = store.Add(analysis);
            if (!ReferenceEquals(stored, analysis))
                return Results.Ok(stored.ToResponse());

            dispatcher.Enqueue(analysis);
            return Results.Accepted($"/analyses/{analysis.Id}", analysis.ToResponse());
        });

        group.MapGet("/{id:guid}", (Guid id, AnalysisStore store) =>
            store.Find(id) is { } analysis ? Results.Ok(analysis.ToResponse()) : Results.NotFound());

        group.MapGet("/", (string? transactionId, int? take, AnalysisStore store) =>
        {
            if (transactionId is not null)
                return Results.Ok(store.FindByTransaction(transactionId) is { } analysis
                    ? [analysis.ToResponse()]
                    : Array.Empty<AnalysisResponse>());

            return Results.Ok(store.List(Math.Clamp(take ?? 50, 1, MaxTake)).Select(a => a.ToResponse()));
        });

        group.MapDelete("/", (AnalysisStore store) => Results.Ok(new { removed = store.Clear() }));
    }

    private static Dictionary<string, string[]> Validate(AnalysisRequest body)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(body.TransactionId))
            errors["transactionId"] = ["O transactionId é obrigatório."];
        else if (body.TransactionId.Length > TransactionIdMaxLength)
            errors["transactionId"] = [$"O transactionId deve ter no máximo {TransactionIdMaxLength} caracteres."];
        if (body.Amount <= 0)
            errors["amount"] = ["O valor deve ser maior que zero."];
        if (body.SourceAccountId == Guid.Empty)
            errors["sourceAccountId"] = ["A conta de origem é obrigatória."];
        if (body.DestinationAccountId == Guid.Empty)
            errors["destinationAccountId"] = ["A conta de destino é obrigatória."];
        if (!Uri.TryCreate(body.CallbackUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            errors["callbackUrl"] = ["O callbackUrl deve ser uma URL http(s) absoluta."];
        return errors;
    }

    private static bool IsAllowedCallback(string callbackUrl, AntifraudOptions options)
        => options.AllowedCallbackHosts.Count == 0
           || options.AllowedCallbackHosts.Contains(new Uri(callbackUrl).Host, StringComparer.OrdinalIgnoreCase);
}
