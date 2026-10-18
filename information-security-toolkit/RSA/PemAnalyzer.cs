using System.Numerics;
using System.Security.Cryptography;
using Rsa = System.Security.Cryptography.RSA;

namespace information_security_toolkit.RSA;

public static class PemAnalyzer
{
    /// <summary>Returns RSA parameters; private parameter arrays must be kept secret by the caller.</summary>
    public static RSAParameters GetParameters(Rsa key, bool includePrivateParameters = false)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key.ExportParameters(includePrivateParameters);
    }

    public static void PrintParameters(Rsa key, bool includePrivateParameters = false)
    {
        RSAParameters parameters = GetParameters(key, includePrivateParameters);
        try
        {
            Console.WriteLine(includePrivateParameters ? "\nPrivate Key Parameters" : "\nPublic Key Parameters");
            PrintParameter("Modulus (n)", parameters.Modulus);
            PrintParameter("Exponent (e)", parameters.Exponent);
            Console.WriteLine($"Public exponent (decimal): {new BigInteger(parameters.Exponent!, isUnsigned: true, isBigEndian: true)}");
            if (includePrivateParameters)
            {
                PrintParameter("Private exponent (d)", parameters.D);
                PrintParameter("Prime (p)", parameters.P);
                PrintParameter("Prime (q)", parameters.Q);
                PrintParameter("DP (dp)", parameters.DP);
                PrintParameter("DQ (dq)", parameters.DQ);
                PrintParameter("InverseQ (inverseQ)", parameters.InverseQ);
            }
        }
        finally
        {
            // ExportParameters creates copies; clear the private arrays used for Console inspection.
            Clear(parameters.D);
            Clear(parameters.P);
            Clear(parameters.Q);
            Clear(parameters.DP);
            Clear(parameters.DQ);
            Clear(parameters.InverseQ);
        }
    }

    internal static byte[] DecodePem(string pem, string expectedLabel)
    {
        ReadOnlySpan<char> text = pem.AsSpan().Trim();
        if (!PemEncoding.TryFind(text, out PemFields fields))
        {
            throw new FormatException($"Expected a valid PEM block labeled '{expectedLabel}'.");
        }

        (int offset, int length) = fields.Location.GetOffsetAndLength(text.Length);
        if (offset != 0 || length != text.Length)
        {
            throw new FormatException("The key file must contain exactly one PEM block and no extra non-whitespace data.");
        }

        if (!text[fields.Label].SequenceEqual(expectedLabel.AsSpan()))
        {
            throw new FormatException($"Expected '{expectedLabel}' PEM; the supplied PEM key type is different.");
        }

        return Convert.FromBase64String(text[fields.Base64Data].ToString());
    }

    private static void PrintParameter(string name, byte[]? value)
    {
        if (value is null || value.Length == 0)
        {
            throw new CryptographicException($"The RSA key is missing parameter {name}.");
        }

        Console.WriteLine($"{name}:\nLength: {value.Length} bytes\nBase64: {Convert.ToBase64String(value)}");
    }

    private static void Clear(byte[]? value)
    {
        if (value is not null)
        {
            CryptographicOperations.ZeroMemory(value);
        }
    }
}
