using System.Security.Cryptography;

namespace information_security_toolkit.AES;

public static class AesKeyManager
{
    private const int KeySizeInBytes = 32;

    public static byte[] GenerateKey()
    {
        return RandomNumberGenerator.GetBytes(KeySizeInBytes);
    }

    public static void SaveKey(byte[] key, string filePath)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!IsValidKey(key))
        {
            throw new ArgumentException("An AES-256 key must contain exactly 32 bytes.", nameof(key));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        // Base64 encodes the key; it does not encrypt or protect it.
        File.WriteAllText(filePath, Convert.ToBase64String(key));
    }

    public static byte[] LoadKey(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string base64Key = File.ReadAllText(filePath);
        byte[] key = Convert.FromBase64String(base64Key);

        if (!IsValidKey(key))
        {
            throw new InvalidDataException("The key file must contain a 32-byte AES-256 key.");
        }

        return key;
    }

    public static bool IsValidKey(byte[]? key)
    {
        return key is { Length: KeySizeInBytes };
    }
}
