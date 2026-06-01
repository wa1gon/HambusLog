namespace HamBusLog.Services;

using System.Text;

public static class WeakSecretProtector
{
    private const string Prefix = "enc:";
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("HamBusLog");

    public static string Encrypt(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return string.Empty;

        if (plaintext.StartsWith(Prefix, StringComparison.Ordinal))
            return plaintext;

        var buffer = Encoding.UTF8.GetBytes(plaintext);
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)(buffer[i] ^ Key[i % Key.Length]);

        return Prefix + Convert.ToBase64String(buffer);
    }

    public static string Decrypt(string? ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
            return string.Empty;

        if (!ciphertext.StartsWith(Prefix, StringComparison.Ordinal))
            return ciphertext;

        var payload = ciphertext.Substring(Prefix.Length);
        try
        {
            var buffer = Convert.FromBase64String(payload);
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = (byte)(buffer[i] ^ Key[i % Key.Length]);

            return Encoding.UTF8.GetString(buffer);
        }
        catch
        {
            return string.Empty;
        }
    }
}

