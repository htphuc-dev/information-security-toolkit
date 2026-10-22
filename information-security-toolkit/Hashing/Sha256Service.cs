using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace information_security_toolkit.Hashing;

public static class Sha256Service
{
    public const int HashSizeInBytes = SHA256.HashSizeInBytes;
    public const int HashSizeInBits = HashSizeInBytes * 8;
    private static readonly Encoding TextEncoding = new UTF8Encoding(false, true);

    /// <summary>Hashes UTF-8 text as uppercase Hex. Empty text is valid; null is rejected.</summary>
    public static string ComputeHash(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return ComputeHash(TextEncoding.GetBytes(input));
    }

    /// <summary>Hashes bytes as uppercase Hex. An empty array is valid; null is rejected.</summary>
    public static string ComputeHash(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Convert.ToHexString(SHA256.HashData(data));
    }

    /// <summary>Hamming distance: the number of differing bits at matching positions in two SHA-256 hashes.</summary>
    public static int CountDifferentBits(byte[] hashA, byte[] hashB)
    {
        ArgumentNullException.ThrowIfNull(hashA);
        ArgumentNullException.ThrowIfNull(hashB);
        if (hashA.Length != HashSizeInBytes)
        {
            throw new ArgumentException($"A SHA-256 hash must contain exactly {HashSizeInBytes} bytes.", nameof(hashA));
        }

        if (hashB.Length != HashSizeInBytes)
        {
            throw new ArgumentException($"A SHA-256 hash must contain exactly {HashSizeInBytes} bytes.", nameof(hashB));
        }

        int differentBits = 0;
        for (int index = 0; index < HashSizeInBytes; index++)
        {
            // XOR sets a bit to 1 exactly where the two input bits differ.
            differentBits += BitOperations.PopCount((uint)(hashA[index] ^ hashB[index]));
        }

        return differentBits;
    }
}
