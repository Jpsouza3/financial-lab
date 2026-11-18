using System.Text.RegularExpressions;
using financial_lab_account.Business.Exceptions;
using financial_lab_account.Business.Interfaces;
using financial_lab_account.Business.Models;

namespace financial_lab_account.Business.Services;

public partial class BalanceHoldService(IAccountRepository repository, TimeProvider clock) : IBalanceHoldService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan MinExpiration = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MaxExpiration = TimeSpan.FromDays(90);
    private static readonly Dictionary<string, HoldStatus> Statuses = Enum.GetValues<HoldStatus>()
        .ToDictionary(s => s.ToString(), StringComparer.Ordinal);

    public async Task<HoldResult> PlaceAsync(Guid accountId, string? reference, decimal? amount, string? reason,
        DateTimeOffset? expiresAt, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        if (reference is null || !ValidReference().IsMatch(reference))
            throw new DomainException(
                "A referência deve ter de 1 a 32 caracteres alfanuméricos, hífen ou sublinhado.");
        if (amount is not > 0)
            throw new DomainException("O valor deve ser maior que zero.");
        if (decimal.Round(amount.Value, 2) != amount.Value)
            throw new DomainException("O valor deve ter no máximo 2 casas decimais.");
        reason = reason?.Trim();
        if (reason is not { Length: >= 5 and <= BalanceHold.ReasonMaxLength })
            throw new DomainException("O motivo deve ter entre 5 e 200 caracteres.");
        if (expiresAt is { } expiry && (expiry - now < MinExpiration || expiry - now > MaxExpiration))
            throw new DomainException("A expiração deve estar entre 10 segundos e 90 dias a partir de agora.");

        for (var attempt = 1; ; attempt++)
        {
            var account = await repository.GetByIdAsync(accountId, ct)
                          ?? throw AccountManagementService.AccountNotFound();

            if (await repository.GetHoldByReferenceAsync(accountId, reference, ct) is { } existing)
                return existing.Amount == amount
                    ? new HoldResult(existing, false)
                    : throw new ConflictException("Referência já utilizada com outro valor.");

            var activeHolds = await repository.CountActiveHoldsAsync(accountId, ct);
            var hold = account.PlaceHold(reference, amount.Value, reason, expiresAt?.UtcDateTime, activeHolds,
                now.UtcDateTime);
            repository.AddHold(hold);

            try
            {
                await repository.SaveChangesAsync(ct);
                return new HoldResult(hold, true);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
            }
        }
    }

    public async Task<BalanceHold> ReleaseAsync(Guid accountId, Guid holdId, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            var account = await repository.GetByIdAsync(accountId, ct)
                          ?? throw AccountManagementService.AccountNotFound();
            var hold = await repository.GetHoldAsync(accountId, holdId, ct) ?? throw HoldNotFound();

            switch (hold.Status)
            {
                case HoldStatus.Released:
                    return hold;
                case HoldStatus.Expired:
                    throw new ConflictException("Bloqueio expirado não pode ser liberado.");
            }

            account.ReleaseHold(hold, HoldStatus.Released, clock.GetUtcNow().UtcDateTime);

            try
            {
                await repository.SaveChangesAsync(ct);
                return hold;
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
            }
        }
    }

    public async Task<BalanceHold> GetAsync(Guid accountId, Guid holdId, CancellationToken ct)
    {
        _ = await repository.GetByIdAsync(accountId, ct) ?? throw AccountManagementService.AccountNotFound();
        return await repository.GetHoldAsync(accountId, holdId, ct) ?? throw HoldNotFound();
    }

    public async Task<IReadOnlyList<BalanceHold>> ListAsync(Guid accountId, string? status, CancellationToken ct)
    {
        HoldStatus? filter = null;
        if (status is not null)
            filter = Statuses.TryGetValue(status, out var parsed)
                ? parsed
                : throw new DomainException("Status inválido. Valores aceitos: Active, Released, Expired.");

        _ = await repository.GetByIdAsync(accountId, ct) ?? throw AccountManagementService.AccountNotFound();
        return await repository.ListHoldsAsync(accountId, filter, ct);
    }

    public async Task<int> ExpireDueAsync(int batchSize, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var expired = 0;

        foreach (var holdId in await repository.ListExpiredHoldIdsAsync(now, batchSize, ct))
        {
            var hold = await repository.GetHoldByIdAsync(holdId, ct);
            if (hold is not { Status: HoldStatus.Active } ||
                await repository.GetByIdAsync(hold.AccountId, ct) is not { } account)
                continue;

            account.ReleaseHold(hold, HoldStatus.Expired, clock.GetUtcNow().UtcDateTime);
            try
            {
                await repository.SaveChangesAsync(ct);
                expired++;
            }
            catch (ConcurrencyConflictException)
            {
            }
        }

        return expired;
    }

    private static NotFoundException HoldNotFound() => new("Bloqueio não encontrado.");

    [GeneratedRegex("^[A-Za-z0-9_-]{1,32}$")]
    private static partial Regex ValidReference();
}
