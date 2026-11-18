using financial_lab_antifraud.Api.Models;
using financial_lab_antifraud.Api.Rules;

namespace financial_lab_antifraud.Api.Endpoints;

public static class RuleEndpoints
{
    public static void MapRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rules");

        group.MapGet("/", (RuleStore rules) => Results.Ok(rules.All));

        group.MapGet("/{id}", (string id, RuleStore rules) =>
            rules.Find(id) is { } rule ? Results.Ok(rule) : Results.NotFound());

        group.MapPut("/", (List<FraudRule> body, RuleStore rules) =>
        {
            var errors = RuleValidator.Validate(body);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            rules.Replace(body);
            return Results.Ok(rules.All);
        });

        group.MapPost("/", (FraudRule body, RuleStore rules) =>
        {
            var errors = RuleValidator.Validate(body);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            return rules.Add(body)
                ? Results.Created($"/rules/{body.Id}", rules.Find(body.Id))
                : Results.Problem(title: "Regra já existe.", detail: $"Já existe uma regra com o id '{body.Id}'.",
                    statusCode: StatusCodes.Status409Conflict);
        });

        group.MapPut("/{id}", (string id, FraudRule body, RuleStore rules) =>
        {
            body.Id = id;
            var errors = RuleValidator.Validate(body);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            return rules.Upsert(body)
                ? Results.Created($"/rules/{body.Id}", rules.Find(body.Id))
                : Results.Ok(rules.Find(body.Id));
        });

        group.MapDelete("/{id}", (string id, RuleStore rules) =>
            rules.Remove(id) ? Results.NoContent() : Results.NotFound());

        group.MapPost("/reset", (RuleStore rules) => Results.Ok(rules.Reset()));
    }
}
