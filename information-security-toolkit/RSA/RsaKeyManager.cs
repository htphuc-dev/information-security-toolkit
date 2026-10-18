using System.Security.Cryptography;
using System.Text;
using Rsa = System.Security.Cryptography.RSA;

namespace information_security_toolkit.RSA;

public static class RsaKeyManager
{
    /// <summary>Creates a key pair. The caller owns the returned RSA object and must dispose it.</summary>
    public static Rsa GenerateKeyPair(int keySize = 2048)
    {
        ValidateKeySize(keySize);
        return Rsa.Create(keySize);
    }

    public static void ExportPublicKey(Rsa key, string filePath)
    {
        SavePem(key, filePath, includePrivate: false);
    }

    /// <summary>Exports unencrypted PKCS#8 PEM. Keep the file private.</summary>
    public static void ExportPrivateKey(Rsa key, string filePath)
    {
        SavePem(key, filePath, includePrivate: true);
    }

    /// <summary>Loads SPKI PUBLIC KEY PEM. The caller must dispose the returned RSA object.</summary>
    public static Rsa LoadPublicKey(string filePath)
    {
        return LoadKey(filePath, includePrivate: false);
    }

    /// <summary>Loads unencrypted PKCS#8 PRIVATE KEY PEM. The caller must dispose the result.</summary>
    public static Rsa LoadPrivateKey(string filePath)
    {
        return LoadKey(filePath, includePrivate: true);
    }

    private static void SavePem(Rsa key, string filePath, bool includePrivate)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ValidateKeySize(key.KeySize);
        string pem = includePrivate ? key.ExportPkcs8PrivateKeyPem() : key.ExportSubjectPublicKeyInfoPem();
        // CreateNew preserves existing keys instead of silently replacing them.
        using var file = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(file, new UTF8Encoding(false));
        writer.WriteLine(pem);
    }

    private static Rsa LoadKey(string filePath, bool includePrivate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The PEM key file does not exist.", filePath);
        }

        string label = includePrivate ? "PRIVATE KEY" : "PUBLIC KEY";
        byte[] encodedKey = PemAnalyzer.DecodePem(File.ReadAllText(filePath), label);
        Rsa? key = null;
        try
        {
            key = Rsa.Create();
            int bytesRead;
            if (includePrivate)
            {
                key.ImportPkcs8PrivateKey(encodedKey, out bytesRead);
            }
            else
            {
                key.ImportSubjectPublicKeyInfo(encodedKey, out bytesRead);
            }

            if (bytesRead != encodedKey.Length)
            {
                throw new CryptographicException("Unexpected data after the encoded RSA key.");
            }

            ValidateKeySize(key.KeySize);
            return key;
        }
        catch (CryptographicException ex)
        {
            key?.Dispose();
            throw new CryptographicException($"Cannot import RSA {label} PEM: invalid or unsupported key data.", ex);
        }
        catch
        {
            key?.Dispose();
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encodedKey);
        }
    }

    private static void ValidateKeySize(int keySize)
    {
        if (keySize is not (2048 or 3072 or 4096))
        {
            throw new ArgumentOutOfRangeException(nameof(keySize), keySize,
                "Supported RSA key sizes are 2048, 3072 and 4096 bits.");
        }
    }
}
