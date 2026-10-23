using System.Security.Cryptography;

namespace information_security_toolkit.AES;

public static class AesFileService
{
    private const int BufferSize = 64 * 1024;
    private const byte FormatVersion = 1;
    private const int SaltSize = 16;
    private const int IvSize = 16;
    private const int MacSize = 32;
    private const int KeySize = 32;
    private const int PrefixSize = 5; // Magic "ISTF" (4 bytes) + version (1 byte).
    private const int HeaderSize = PrefixSize + SaltSize + IvSize;
    private const int CiphertextOffset = HeaderSize + MacSize;

    /// <summary>
    /// Creates a new AES-256-CBC/PKCS7 file authenticated with HMAC-SHA256.
    /// Format v1: magic | version | salt | IV | MAC | ciphertext.
    /// Existing output is replaced only when overwrite is explicitly enabled and writing succeeds.
    /// </summary>
    public static void EncryptFile(string inputPath, string outputPath, byte[] key, bool overwrite = false)
    {
        (inputPath, outputPath) = ValidateArguments(inputPath, outputPath, key, overwrite);
        using var input = OpenInput(inputPath);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] iv = RandomNumberGenerator.GetBytes(IvSize);
        byte[] header = [.. "ISTF"u8, FormatVersion, .. salt, .. iv];
        byte[] encryptionKey = DeriveKey(key, salt, "ISTF/v1/encryption"u8);
        byte[] macKey = DeriveKey(key, salt, "ISTF/v1/authentication"u8);

        try
        {
            WriteOutput(outputPath, overwrite, output =>
            {
                output.Write(header);
                output.Write(new byte[MacSize]); // Reserved until ciphertext is complete.

                using var aes = CreateAes(encryptionKey, iv);
                using var encryptor = aes.CreateEncryptor();
                using (var crypto = new CryptoStream(output, encryptor, CryptoStreamMode.Write, leaveOpen: true))
                {
                    input.CopyTo(crypto, BufferSize);
                    crypto.FlushFinalBlock();
                }

                // Second pass over the output authenticates the header and all ciphertext.
                output.Flush();
                byte[] mac = ComputeMac(output, header, macKey);
                output.Position = HeaderSize;
                output.Write(mac);
            });
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encryptionKey);
            CryptographicOperations.ZeroMemory(macKey);
        }
    }

    /// <summary>
    /// Verifies the entire file before decrypting. Existing output is preserved unless overwrite is enabled.
    /// Wrong keys or modified data raise CryptographicException; bad formats raise InvalidDataException.
    /// </summary>
    public static void DecryptFile(string inputPath, string outputPath, byte[] key, bool overwrite = false)
    {
        (inputPath, outputPath) = ValidateArguments(inputPath, outputPath, key, overwrite);
        // Keep this handle open through verification and decryption, denying writes/deletes on Windows.
        using var input = OpenInput(inputPath);
        long ciphertextLength = input.Length - CiphertextOffset;
        if (ciphertextLength < IvSize || ciphertextLength % IvSize != 0)
        {
            throw new InvalidDataException("Invalid encrypted file length or incomplete AES block.");
        }

        byte[] header = new byte[HeaderSize];
        input.ReadExactly(header);
        if (!header.AsSpan(0, 4).SequenceEqual("ISTF"u8) || header[4] != FormatVersion)
        {
            throw new InvalidDataException("Invalid encrypted file header or unsupported format version.");
        }

        byte[] salt = header[PrefixSize..(PrefixSize + SaltSize)];
        byte[] iv = header[(PrefixSize + SaltSize)..];
        byte[] storedMac = new byte[MacSize];
        input.ReadExactly(storedMac);
        byte[] encryptionKey = DeriveKey(key, salt, "ISTF/v1/encryption"u8);
        byte[] macKey = DeriveKey(key, salt, "ISTF/v1/authentication"u8);

        try
        {
            byte[] computedMac = ComputeMac(input, header, macKey);
            if (!CryptographicOperations.FixedTimeEquals(storedMac, computedMac))
            {
                throw new CryptographicException(
                    "File authentication failed: the key is incorrect or the encrypted file has been modified.");
            }

            // Authenticate first: untrusted ciphertext never reaches the CBC decryptor.
            WriteOutput(outputPath, overwrite, output =>
            {
                input.Position = CiphertextOffset;
                using var aes = CreateAes(encryptionKey, iv);
                using var decryptor = aes.CreateDecryptor();
                using var crypto = new CryptoStream(input, decryptor, CryptoStreamMode.Read, leaveOpen: true);
                crypto.CopyTo(output, BufferSize);
            });
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encryptionKey);
            CryptographicOperations.ZeroMemory(macKey);
        }
    }

    private static byte[] DeriveKey(byte[] key, byte[] salt, ReadOnlySpan<byte> purpose)
    {
        byte[] derivedKey = new byte[KeySize];
        HKDF.DeriveKey(HashAlgorithmName.SHA256, key, derivedKey, salt, purpose);
        return derivedKey;
    }

    private static Aes CreateAes(byte[] key, byte[] iv)
    {
        Aes aes = Aes.Create();
        aes.KeySize = KeySize * 8;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;
        return aes;
    }

    private static byte[] ComputeMac(FileStream stream, byte[] header, byte[] macKey)
    {
        using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, macKey);
        hmac.AppendData(header);
        stream.Position = CiphertextOffset; // Skip the stored MAC itself.

        byte[] buffer = new byte[BufferSize];
        int bytesRead;
        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
        {
            hmac.AppendData(buffer, 0, bytesRead);
        }

        return hmac.GetHashAndReset();
    }

    private static FileStream OpenInput(string path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            BufferSize, FileOptions.SequentialScan);
    }

    private static (string InputPath, string OutputPath) ValidateArguments(
        string inputPath, string outputPath, byte[] key, bool overwrite)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!AesKeyManager.IsValidKey(key))
        {
            throw new ArgumentException("An AES-256 key must contain exactly 32 bytes.", nameof(key));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        inputPath = Path.GetFullPath(inputPath);
        outputPath = Path.GetFullPath(outputPath);
        StringComparison pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (string.Equals(inputPath, outputPath, pathComparison))
        {
            throw new ArgumentException("Input and output paths must be different.", nameof(outputPath));
        }

        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("The input file does not exist.", inputPath);
        }

        if (Directory.Exists(outputPath) || (!overwrite && File.Exists(outputPath)))
        {
            throw new IOException("The output path already exists. Choose a new file name.");
        }

        return (inputPath, outputPath);
    }

    private static void WriteOutput(string outputPath, bool overwrite, Action<FileStream> write)
    {
        // Finish and close the temporary file before replacing an explicitly approved target.
        // The same directory keeps the final move on the same volume; failures preserve the old output.
        string directory = Path.GetDirectoryName(outputPath)!;
        string temporaryPath = Path.Combine(directory, $".aes-{Guid.NewGuid():N}.tmp");
        bool temporaryFileCreated = false;
        try
        {
            using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.None, BufferSize, FileOptions.SequentialScan))
            {
                temporaryFileCreated = true;
                write(output);
            }

            File.Move(temporaryPath, outputPath, overwrite);
        }
        finally
        {
            if (temporaryFileCreated)
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
