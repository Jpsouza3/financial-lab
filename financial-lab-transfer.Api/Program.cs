using financial_lab_transfer.Business.Models;
using System.Text.Json.Serialization;
using financial_lab_transfer.Api.Consumers;
using financial_lab_transfer.Api.Endpoints;
using financial_lab_transfer.Api.Gateways;
using financial_lab_transfer.Api.Models;
using financial_lab_transfer.Api.Workers;
using financial_lab_transfer.Business.Interfaces;
using financial_lab_transfer.Business.Services;
using financial_lab_transfer.Data.Context;
using financial_lab_transfer.Data.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TransferDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TransferDb")));
builder.Services.AddScoped<ITransferRepository, TransferRepository>();
builder.Services.AddScoped<IAccountGateway, AccountGateway>();
builder.Services.AddScoped<IAntifraudGateway, AntifraudGateway>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<ISimulationService, SimulationService>();
builder.Services.Configure<LabOptions>(builder.Configuration.GetSection("Lab"));
builder.Services.AddOptions<AntifraudOptions>()
    .Bind(builder.Configuration.GetSection("Antifraud"))
    .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "Antifraud:BaseUrl deve ser uma URL absoluta.")
    .Validate(o => Uri.TryCreate(o.CallbackUrl, UriKind.Absolute, out _),
        "Antifraud:CallbackUrl deve ser uma URL absoluta.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.WebhookSecret), "Antifraud:WebhookSecret é obrigatório.")
    .Validate(o => o.ReconcileIntervalSeconds > 0 && o.StaleAfterSeconds > 0 && o.TimeoutSeconds > 0,
        "Os intervalos de Antifraud devem ser maiores que zero.")
    .ValidateOnStart();
builder.Services.AddHttpClient<AntifraudClient>((sp, client) =>
{
    client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<AntifraudOptions>>().Value.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddSingleton<TimeProvider>(UtcClock.Instance);
builder.Services.AddHostedService<ScreeningReconciler>();

builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("transfer", false));
    x.AddConsumer<AccountEventsConsumer>();
    x.AddConsumer<AnalyzeTransferConsumer>();

    x.AddEntityFrameworkOutbox<TransferDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConfigureEndpointsCallback((context, _, cfg) =>
    {
        cfg.UseMessageRetry(r =>
            r.Exponential(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(200)));
        cfg.UseEntityFrameworkOutbox<TransferDbContext>(context);
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMq = builder.Configuration.GetSection("RabbitMq");
        cfg.Host(rabbitMq["Host"], ushort.Parse(rabbitMq["Port"] ?? "5672"), rabbitMq["VirtualHost"] ?? "/", h =>
        {
            h.Username(rabbitMq["Username"]!);
            h.Password(rabbitMq["Password"]!);
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapTransferEndpoints();
app.MapSimulationEndpoints();
app.MapWebhookEndpoints();

app.Run();
