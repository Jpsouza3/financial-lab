using System.Collections.Concurrent;

namespace financial_lab_antifraud.Api.Analyses;

public class AnalysisStore
{
    private readonly ConcurrentDictionary<Guid, Analysis> byId = new();
    private readonly ConcurrentDictionary<string, Analysis> byTransaction = new();
    private readonly ConcurrentDictionary<string, int> submitFailures = new();

    public Analysis? Find(Guid id) => byId.GetValueOrDefault(id);

    public Analysis? FindByTransaction(string transactionId) => byTransaction.GetValueOrDefault(transactionId);

    public IReadOnlyList<Analysis> List(int take)
        => byId.Values.OrderByDescending(a => a.CreatedAt).Take(take).ToList();

    public Analysis Add(Analysis analysis)
    {
        var stored = byTransaction.GetOrAdd(analysis.Request.TransactionId, analysis);
        if (ReferenceEquals(stored, analysis))
            byId[analysis.Id] = analysis;
        return stored;
    }

    public bool ShouldFailSubmit(string transactionId, int failures)
    {
        var failed = false;
        submitFailures.AddOrUpdate(transactionId,
            _ =>
            {
                failed = failures > 0;
                return failed ? 1 : 0;
            },
            (_, count) =>
            {
                failed = count < failures;
                return failed ? count + 1 : count;
            });
        return failed;
    }

    public int Clear()
    {
        var count = byId.Count;
        byId.Clear();
        byTransaction.Clear();
        submitFailures.Clear();
        return count;
    }
}
