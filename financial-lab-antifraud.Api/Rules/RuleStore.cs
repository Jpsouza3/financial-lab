using System.Text.Json;
using financial_lab_antifraud.Api.Models;
using Microsoft.Extensions.Options;

namespace financial_lab_antifraud.Api.Rules;

public class RuleStore
{
    private readonly Lock gate = new();
    private readonly IReadOnlyList<FraudRule> configured;
    private List<FraudRule> rules;

    public RuleStore(IOptions<AntifraudOptions> options)
    {
        configured = Clone(options.Value.Rules);
        rules = Clone(configured);
    }

    public IReadOnlyList<FraudRule> All
    {
        get
        {
            lock (gate)
                return Clone(rules);
        }
    }

    public FraudRule? Match(AnalysisRequest request)
    {
        lock (gate)
            return rules.FirstOrDefault(r => r.Matches(request)) is { } rule ? Clone(rule) : null;
    }

    public FraudRule? Find(string id)
    {
        lock (gate)
            return rules.FirstOrDefault(r => SameId(r, id)) is { } rule ? Clone(rule) : null;
    }

    public void Replace(IReadOnlyList<FraudRule> newRules)
    {
        lock (gate)
            rules = Clone(newRules);
    }

    public bool Add(FraudRule rule)
    {
        lock (gate)
        {
            if (rules.Any(r => SameId(r, rule.Id)))
                return false;

            rules.Add(Clone(rule));
            return true;
        }
    }

    public bool Upsert(FraudRule rule)
    {
        lock (gate)
        {
            var index = rules.FindIndex(r => SameId(r, rule.Id));
            if (index < 0)
            {
                rules.Add(Clone(rule));
                return true;
            }

            rules[index] = Clone(rule);
            return false;
        }
    }

    public bool Remove(string id)
    {
        lock (gate)
            return rules.RemoveAll(r => SameId(r, id)) > 0;
    }

    public IReadOnlyList<FraudRule> Reset()
    {
        lock (gate)
        {
            rules = Clone(configured);
            return Clone(rules);
        }
    }

    private static bool SameId(FraudRule rule, string id) => string.Equals(rule.Id, id, StringComparison.OrdinalIgnoreCase);

    private static FraudRule Clone(FraudRule rule)
        => JsonSerializer.Deserialize<FraudRule>(JsonSerializer.Serialize(rule))!;

    private static List<FraudRule> Clone(IEnumerable<FraudRule> source) => source.Select(Clone).ToList();
}
