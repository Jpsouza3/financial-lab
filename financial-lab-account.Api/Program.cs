using System.Text.Json.Serialization;
using financial_lab_account.Api.Consumers;
using financial_lab_account.Api.Endpoints;
using financial_lab_account.Business.Exceptions;
using financial_lab_account.Business.Interfaces;
using financial_lab_account.Business.Services;
using financial_lab_account.Data.Context;
using financial_lab_account.Data.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AccountDb")));
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAccountTransactionService, AccountTransactionService>();

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<DebitAccountConsumer>();
    x.AddConsumer<CreditAccountConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMq = builder.Configuration.GetSection("RabbitMq");
        cfg.Host(rabbitMq["Host"], ushort.Parse(rabbitMq["Port"] ?? "5672"), rabbitMq["VirtualHost"] ?? "/", h =>
        {
            h.Username(rabbitMq["Username"]!);
            h.Password(rabbitMq["Password"]!);
        });

        cfg.UseMessageRetry(r =>
        {
            r.Exponential(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(200));
            r.Ignore<DomainException>();
            r.Ignore<IdempotencyConflictException>();
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapAccountEndpoints();

app.Run();
