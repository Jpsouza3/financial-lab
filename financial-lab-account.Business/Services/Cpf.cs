namespace financial_lab_account.Business.Services;

public static class Cpf
{
    public static bool IsValid(string? value)
    {
        if (value is not { Length: 11 } || !value.All(char.IsAsciiDigit) || value.Distinct().Count() == 1)
            return false;

        var digits = value.Select(c => c - '0').ToArray();
        return digits[9] == CheckDigit(digits, 9) && digits[10] == CheckDigit(digits, 10);
    }

    private static int CheckDigit(int[] digits, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += digits[i] * (length + 1 - i);

        var rest = sum * 10 % 11;
        return rest == 10 ? 0 : rest;
    }
}
