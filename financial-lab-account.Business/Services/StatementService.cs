using System.Globalization;
using financial_lab_account.Business.Exceptions;
using financial_lab_account.Business.Interfaces;
using financial_lab_account.Business.Models;

namespace financial_lab_account.Business.Services;

public class StatementService(IAccountRepository repository, TimeProvider clock) : IStatementService
{
    private const int MaxPeriodDays = 90;
    private const int DefaultPeriodDays = 30;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private static readonly Dictionary<string, EntryType> Types = Enum.GetValues<EntryType>()
        .ToDictionary(t => t.ToString(), StringComparer.Ordinal);

    public async Task<Statement> GetAsync(Guid accountId, string? from, string? to, string? type, string? page,
        string? pageSize, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var parsedFrom = ParseDate(from);
        var parsedTo = ParseDate(to);

        var end = parsedTo ?? today;
        var start = parsedFrom ?? DateOnly.FromDayNumber(Math.Max(0, end.DayNumber - (DefaultPeriodDays - 1)));
        if (end > today)
            throw new DomainException("A data final não pode estar no futuro.");
        if (start > end)
            throw new DomainException("A data inicial não pode ser posterior à data final.");
        if (end.DayNumber - start.DayNumber + 1 > MaxPeriodDays)
            throw new DomainException($"O período máximo é de {MaxPeriodDays} dias.");

        EntryType? entryType = null;
        if (type is not null)
            entryType = Types.TryGetValue(type, out var parsedType)
                ? parsedType
                : throw new DomainException("Tipo inválido. Valores aceitos: Credit, Debit.");

        var pageNumber = ParseInt(page, 1, 1, int.MaxValue, "A página deve ser maior ou igual a 1.");
        var size = ParseInt(pageSize, DefaultPageSize, 1, MaxPageSize,
            "O tamanho da página deve estar entre 1 e 100.");

        _ = await repository.GetByIdAsync(accountId, ct) ?? throw AccountManagementService.AccountNotFound();

        var query = new StatementQuery(accountId, ToUtc(start), ToUtc(end.AddDays(1)), entryType, pageNumber, size);
        var opening = await repository.GetLastBalanceBeforeAsync(accountId, query.Start, ct) ?? 0m;
        var closing = await repository.GetLastBalanceBeforeAsync(accountId, query.End, ct) ?? opening;
        var totals = await repository.GetStatementTotalsAsync(query, ct);
        var items = await repository.ListEntriesAsync(query, ct);

        return new Statement(accountId, start, end, entryType, pageNumber, size, totals.Count,
            (totals.Count + size - 1) / size, opening, closing, totals.Credits, totals.Debits, items);
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (value is null)
            return null;
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
            out var date)
            ? date
            : throw new DomainException("Data inválida. Use o formato yyyy-MM-dd.");
    }

    private static int ParseInt(string? value, int fallback, int min, int max, string error)
    {
        if (value is null)
            return fallback;
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) &&
               parsed >= min && parsed <= max
            ? parsed
            : throw new DomainException(error);
    }

    private static DateTime ToUtc(DateOnly date) => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
