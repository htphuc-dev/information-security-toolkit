using System.Security.Cryptography;
using System.Text;

namespace information_security_toolkit.AES;

public static class AesTextService
{
    private const int NonceSizeInBytes = 12;
    private const int TagSizeInBytes = 16;
    private const int HeaderSizeInBytes = NonceSizeInBytes + TagSizeInBytes;
    private static readonly Encoding TextEncoding = new UTF8Encoding(false, true);

    /// <summary>
    /// Encrypts text as Base64(nonce[12] | tag[16] | ciphertext).
    /// Empty text is supported; null is rejected.
    /// </summary>
    public static string Encrypt(string plaintext, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ValidateKey(key);

        byte[] plaintextBytes = TextEncoding.GetBytes(plaintext);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeInBytes);
        byte[] tag = new byte[TagSizeInBytes];
        byte[] ciphertext = new byte[plaintextBytes.Length];

        using var aes = new AesGcm(key, TagSizeInBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        byte[] payload = new byte[HeaderSizeInBytes + ciphertext.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSizeInBytes);
        ciphertext.CopyTo(payload, HeaderSizeInBytes);

        return Convert.ToBase64String(payload);
    }

    /// <summary>
    /// Decrypts Base64(nonce[12] | tag[16] | ciphertext) into UTF-8 text.
    /// Invalid input, an incorrect key or modified data raises an exception.
    /// </summary>
    public static string Decrypt(string encryptedData, byte[] key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedData);
        ValidateKey(key);

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(encryptedData);
        }
        catch (FormatException ex)
        {
            throw new FormatException("Encrypted data must be a valid Base64 string.", ex);
        }

        if (payload.Length < HeaderSizeInBytes)
        {
            throw new FormatException("Encrypted data must contain a 12-byte nonce and a 16-byte tag.");
        }

        byte[] nonce = payload[..NonceSizeInBytes];
        byte[] tag = payload[NonceSizeInBytes..HeaderSizeInBytes];
        byte[] ciphertext = payload[HeaderSizeInBytes..];
        byte[] plaintextBytes = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSizeInBytes);
        try
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);
        }
        catch (AuthenticationTagMismatchException ex)
        {
            throw new CryptographicException(
                "Unable to decrypt: the key is incorrect or the encrypted data has been modified.", ex);
        }

        return TextEncoding.GetString(plaintextBytes);
    }

    private static void ValidateKey(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!AesKeyManager.IsValidKey(key))
        {
            throw new ArgumentException("An AES-256 key must contain exactly 32 bytes.", nameof(key));
        }
    }
}
