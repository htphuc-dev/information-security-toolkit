using System.Security.Cryptography;
using Rsa = System.Security.Cryptography.RSA;

namespace information_security_toolkit.Signature;

public static class DigitalSignatureService
{
    /// <summary>Hashes the data with SHA-256 and creates an RSA-PSS signature. Empty data is valid.</summary>
    public static byte[] SignData(byte[] data, Rsa privateKey)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(privateKey);

        try
        {
            // SignData performs the hashing internally. The provider requires a private key.
            return privateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException(
                "RSA-PSS signing failed: a private key supporting SHA-256 and PSS is required.", ex);
        }
    }

    /// <summary>Verifies an RSA-PSS/SHA-256 signature using public parameters; invalid signatures return false.</summary>
    public static bool VerifyData(byte[] data, byte[] signature, Rsa publicKey)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(publicKey);

        if (signature.Length != (publicKey.KeySize + 7) / 8)
        {
            return false;
        }

        // Configuration/provider errors propagate; a signature mismatch returns false.
        return publicKey.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }
}
