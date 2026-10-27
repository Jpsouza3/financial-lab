using System.Security.Cryptography;

namespace financial_lab_transfer.Business.Models;

public static class EndToEndIds
{
    public const string LabIspb = "99999999";
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string NewPayment() => New('E');

    public static string NewRefund() => New('D');

    private static string New(char type)
        => $"{type}{LabIspb}{DateTime.UtcNow:yyyyMMddHHmm}{RandomNumberGenerator.GetString(Alphabet, 11)}";
}
