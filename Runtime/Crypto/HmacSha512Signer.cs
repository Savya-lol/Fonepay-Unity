using System.Security.Cryptography;
using System.Text;

internal sealed class HmacSha512Signer
{
    private readonly byte[] _keyBytes;

    internal HmacSha512Signer(string secretKey)
    {
        _keyBytes = Encoding.UTF8.GetBytes(secretKey);
    }

    internal string SignQrRequest(
        string amount, string prn, string merchantCode,
        string remarks1, string remarks2)
        => Compute($"{amount},{prn},{merchantCode},{remarks1},{remarks2}");

    internal string SignQrRequestWithTax(
        string amount, string prn, string merchantCode,
        string remarks1, string remarks2,
        string taxAmount, string taxRefund)
        => Compute($"{amount},{prn},{merchantCode},{remarks1},{remarks2},{taxAmount},{taxRefund}");

    internal string SignStatusCheck(string prn, string merchantCode)
        => Compute($"{prn},{merchantCode}");

    internal string SignTaxRefund(
        string fonepayTraceId, string merchantPrn,
        string invoiceNumber, string invoiceDate,
        string transactionAmount, string merchantCode)
        => Compute($"{fonepayTraceId},{merchantPrn},{invoiceNumber},{invoiceDate},{transactionAmount},{merchantCode}");

    private string Compute(string message)
    {
        using var hmac = new HMACSHA512(_keyBytes);
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return BytesToHexLower(hash);
    }

    private static string BytesToHexLower(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}