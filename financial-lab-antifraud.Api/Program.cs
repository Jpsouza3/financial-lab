using System.Text.Json.Serialization;
using financial_lab_antifraud.Api.Analyses;
using financial_lab_antifraud.Api.Endpoints;
using financial_lab_antifraud.Api.Models;
using financial_lab_antifraud.Api.Rules;
using financial_lab_antifraud.Api.Webhooks;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<AntifraudOptions>()
    .Bind(builder.Configuration.GetSection("Antifraud"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.WebhookSecret), "Antifraud:WebhookSecret é obrigatório.")
    .Validate(o => RuleValidator.Validate(o.Rules).Count == 0, "Antifraud:Rules contém regras inválidas.")
    .Validate(o => o.Delivery.MaxAttempts >= 1, "Antifraud:Delivery:MaxAttempts deve ser >= 1.")
    .ValidateOnStart();

builder.Services.AddSingleton<RuleStore>();
builder.Services.AddSingleton<AnalysisStore>();
builder.Services.AddSingleton<WebhookSigner>();
builder.Services.AddSingleton<WebhookDispatcher>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<WebhookDispatcher>());
builder.Services.AddHttpClient(WebhookDispatcher.HttpClientName, (sp, client) =>
    client.Timeout = TimeSpan.FromMilliseconds(
        sp.GetRequiredService<IOptions<AntifraudOptions>>().Value.Delivery.TimeoutMs));

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapAnalysisEndpoints();
app.MapRuleEndpoints();

app.Run();
