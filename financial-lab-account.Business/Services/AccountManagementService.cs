using System.Text.RegularExpressions;
using financial_lab_account.Business.Exceptions;
using financial_lab_account.Business.Interfaces;
using financial_lab_account.Business.Models;

namespace financial_lab_account.Business.Services;

public partial class AccountManagementService(IAccountRepository repository, TimeProvider clock)
    : IAccountManagementService
{
    private const int MaxAttempts = 5;
    private const string DefaultBranch = "0001";
    private const decimal MaxInitialDeposit = 5000.00m;
    private static readonly HashSet<string> Branches = ["0001", "0002"];
    private static readonly Dictionary<string, AccountStatus> Statuses = Enum.GetValues<AccountStatus>()
        .ToDictionary(s => s.ToString(), StringComparer.Ordinal);

    public async Task<Account> OpenAsync(string? name, string? document, string? branch, decimal? initialDeposit,
        CancellationToken ct)
    {
        var normalizedName = NormalizeName(name);
        if (!Cpf.IsValid(document))
            throw new DomainException("CPF inválido.");
        branch ??= DefaultBranch;
        if (branch.Length != 4 || !branch.All(char.IsAsciiDigit))
            throw new DomainException("A agência deve ter 4 dígitos.");
        var deposit = initialDeposit ?? 0m;
        if (deposit is < 0 or > MaxInitialDeposit)
            throw new DomainException("O depósito inicial deve estar entre 0 e 5000.00.");
        if (decimal.Round(deposit, 2) != deposit)
            throw new DomainException("O depósito inicial deve ter no máximo 2 casas decimais.");
        if (!Branches.Contains(branch))
            throw new BusinessRuleException("Agência inexistente.");

        for (var attempt = 1; ; attempt++)
        {
            if (await repository.HasOpenAccountAsync(document!, ct))
                throw new ConflictException("Já existe uma conta não encerrada para este documento.");

            var number = (await repository.GetLastNumberAsync(branch, ct) + 1).ToString("D5");
            var account = Account.Open(normalizedName, document!, branch, number, clock.GetUtcNow().UtcDateTime);
            repository.AddAccount(account);

            if (deposit > 0)
            {
                var entry = account.Credit(deposit, $"OPEN{branch}{number}", "Depósito inicial");
                repository.AddEntry(entry);
                repository.AddProcessedCommand(ProcessedCommand.Applied(entry));
            }

            try
            {
                await repository.SaveChangesAsync(ct);
                return account;
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
            }
        }
    }

    public async Task<Account> GetAsync(Guid accountId, CancellationToken ct)
        => await repository.GetByIdAsync(accountId, ct) ?? throw AccountNotFound();

    public async Task<StatusChange> ChangeStatusAsync(Guid accountId, string? status, string? reason,
        CancellationToken ct)
    {
        if (status is null || !Statuses.TryGetValue(status, out var target))
            throw new DomainException("Status inválido. Valores aceitos: Active, Blocked, Closed.");
        reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (reason is null && target is AccountStatus.Blocked or AccountStatus.Closed)
            throw new DomainException("O motivo é obrigatório para bloquear ou encerrar a conta.");
        if (reason is { Length: < 5 or > 200 })
            throw new DomainException("O motivo deve ter entre 5 e 200 caracteres.");

        for (var attempt = 1; ; attempt++)
        {
            var account = await GetAsync(accountId, ct);
            var previous = account.Status;
            var hasActiveHolds = await repository.CountActiveHoldsAsync(accountId, ct) > 0;

            if (!account.ChangeStatus(target, reason, hasActiveHolds, clock.GetUtcNow().UtcDateTime))
                return new StatusChange(account, previous, false);

            try
            {
                await repository.SaveChangesAsync(ct);
                return new StatusChange(account, previous, true);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
            }
        }
    }

    internal static NotFoundException AccountNotFound() => new("Conta não encontrada.");

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome é obrigatório.");

        var normalized = Whitespace().Replace(name.Trim(), " ");
        if (normalized.Length is < 3 or > 150)
            throw new DomainException("O nome deve ter entre 3 e 150 caracteres.");
        if (!AllowedName().IsMatch(normalized))
            throw new DomainException("O nome deve conter apenas letras, espaços, hífen ou apóstrofo.");
        return normalized;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"^[\p{L} '\-]+$")]
    private static partial Regex AllowedName();
}
