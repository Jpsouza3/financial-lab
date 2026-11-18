using financial_lab_antifraud.Api.Models;

namespace financial_lab_antifraud.Api.Rules;

public static class RuleValidator
{
    public const int MaxIdLength = 64;
    public const int MaxDelayMs = 120_000;
    public const int MaxSubmitFailures = 20;

    public static Dictionary<string, string[]> Validate(IReadOnlyList<FraudRule> rules)
    {
        var errors = new Dictionary<string, string[]>();
        for (var i = 0; i < rules.Count; i++)
            foreach (var (key, messages) in Validate(rules[i], $"rules[{i}]"))
                errors[key] = messages;

        var duplicated = rules.GroupBy(r => r.Id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicated.Count > 0)
            errors["rules"] = [$"Ids de regra repetidos: {string.Join(", ", duplicated)}."];

        return errors;
    }

    public static Dictionary<string, string[]> Validate(FraudRule rule, string prefix = "rule")
    {
        var errors = new Dictionary<string, List<string>>();

        void Add(string field, string message)
        {
            var key = $"{prefix}.{field}";
            if (!errors.TryGetValue(key, out var list))
                errors[key] = list = [];
            list.Add(message);
        }

        if (string.IsNullOrWhiteSpace(rule.Id))
            Add("id", "O id é obrigatório.");
        else if (rule.Id.Length > MaxIdLength)
            Add("id", $"O id deve ter no máximo {MaxIdLength} caracteres.");
        if (!Enum.IsDefined(rule.Decision))
            Add("decision", "Decisão inválida.");
        if (!Enum.IsDefined(rule.Delivery))
            Add("delivery", "Modo de entrega inválido.");
        if (rule.MinAmount < 0)
            Add("minAmount", "O valor mínimo não pode ser negativo.");
        if (rule.MaxAmount < 0)
            Add("maxAmount", "O valor máximo não pode ser negativo.");
        if (rule.MinAmount > rule.MaxAmount)
            Add("minAmount", "O valor mínimo deve ser menor ou igual ao máximo.");
        if (rule.DelayMs is < 0 or > MaxDelayMs)
            Add("delayMs", $"O atraso deve estar entre 0 e {MaxDelayMs} ms.");
        if (rule.SubmitFailures is < 0 or > MaxSubmitFailures)
            Add("submitFailures", $"As falhas de envio devem estar entre 0 e {MaxSubmitFailures}.");

        return errors.ToDictionary(e => e.Key, e => e.Value.ToArray());
    }
}
