using System.Security.Cryptography;
using Rsa = System.Security.Cryptography.RSA;

namespace information_security_toolkit.RSA;

public static class RsaCryptoService
{
    public static int GetMaxPlaintextSize(Rsa key)
    {
        // RSAES-OAEP: mLen <= k - 2*hLen - 2, where k is the modulus length in bytes.
        return GetModulusSizeInBytes(key) - 2 * SHA256.HashSizeInBytes - 2;
    }

    /// <summary>Uses the public RSA operation with OAEP-SHA256. Data must contain 1..max bytes.</summary>
    public static byte[] Encrypt(byte[] data, Rsa publicKey)
    {
        ArgumentNullException.ThrowIfNull(data);
        int maximumSize = GetMaxPlaintextSize(publicKey);
        if (data.Length == 0 || data.Length > maximumSize)
        {
            throw new ArgumentException(
                $"RSA-OAEP-SHA256 plaintext must contain 1 to {maximumSize} bytes "
                + $"for a {publicKey.KeySize}-bit key; received {data.Length} bytes.", nameof(data));
        }

        try
        {
            // RSA.Encrypt performs the public-key operation; no private-key encryption is implemented.
            return publicKey.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException(
                "RSA encryption failed: a valid public key supporting OAEP-SHA256 is required.", ex);
        }
    }

    /// <summary>Uses the private RSA operation. Wrong keys and invalid OAEP ciphertext are rejected.</summary>
    public static byte[] Decrypt(byte[] encryptedData, Rsa privateKey)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        int expectedSize = GetModulusSizeInBytes(privateKey);
        if (encryptedData.Length != expectedSize)
        {
            throw new ArgumentException(
                $"RSA ciphertext must contain exactly {expectedSize} bytes for a {privateKey.KeySize}-bit key; "
                + $"received {encryptedData.Length} bytes.", nameof(encryptedData));
        }

        try
        {
            return privateKey.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException(
                "RSA decryption failed: use the matching private key and unmodified OAEP-SHA256 ciphertext.", ex);
        }
    }

    private static int GetModulusSizeInBytes(Rsa key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.KeySize is not (2048 or 3072 or 4096))
        {
            throw new ArgumentException("Supported RSA key sizes are 2048, 3072 and 4096 bits.", nameof(key));
        }

        return (key.KeySize + 7) / 8;
    }
}
