using financial_lab_account.Business.Models;

namespace financial_lab_account.Business.Interfaces;

public interface IStatementService
{
    Task<Statement> GetAsync(Guid accountId, string? from, string? to, string? type, string? page,
        string? pageSize, CancellationToken ct);
}
