using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using information_security_toolkit.RSA;
using Rsa = System.Security.Cryptography.RSA;

namespace information_security_toolkit.Benchmark;

public static class RsaBenchmark
{
    private const int InputSizeBytes = 32;
    private const int WarmUpIterations = 10;
    private const string PaddingName = "OAEP-SHA256";
    private const string CsvHeader = "Timestamp,KeySizeBits,Padding,InputSizeBytes,Iterations,"
        + "EncryptionTotalMs,EncryptionAverageMs,EncryptionOpsPerSecond,"
        + "DecryptionTotalMs,DecryptionAverageMs,DecryptionOpsPerSecond,PrivateVsPublicRatio,RoundTripResult";

    public static RsaBenchmarkResult Run(string repositoryRoot, string mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(mode);
        int iterations = mode.ToLowerInvariant() switch
        {
            "quick" => 100,
            "normal" => 1000,
            "extended" => 10000,
            _ => throw new ArgumentException("RSA benchmark mode must be 'quick', 'normal' or 'extended'.", nameof(mode))
        };

        string resultsDirectory = Path.Combine(Path.GetFullPath(repositoryRoot), "results", "benchmark");
        Directory.CreateDirectory(resultsDirectory);
        string csvPath = Path.Combine(resultsDirectory, "rsa-benchmark.csv");

        // Setup is outside the timers. The encryption object contains only public parameters.
        using var privateKey = RsaKeyManager.GenerateKeyPair();
        using var publicKey = Rsa.Create();
        publicKey.ImportParameters(privateKey.ExportParameters(includePrivateParameters: false));
        byte[] data = RandomNumberGenerator.GetBytes(InputSizeBytes);
        Console.WriteLine("----------------------------------------");
        Console.WriteLine($"Key size: {publicKey.KeySize} bits\nPadding: {PaddingName}");
        Console.WriteLine($"Input size: {data.Length} bytes\nIterations: {iterations}");
        Console.WriteLine($"Warm-up: {WarmUpIterations} encrypt/decrypt pairs, excluded from measurements and CSV.");
        for (int index = 0; index < WarmUpIterations; index++)
        {
            byte[] warmUpCiphertext = RsaCryptoService.Encrypt(data, publicKey);
            RsaCryptoService.Decrypt(warmUpCiphertext, privateKey);
        }

        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        byte[] ciphertext = [];
        var encryptionTimer = Stopwatch.StartNew();
        for (int index = 0; index < iterations; index++)
        {
            ciphertext = RsaCryptoService.Encrypt(data, publicKey);
        }
        encryptionTimer.Stop();

        // Reuse the last measured ciphertext; retaining every ciphertext is unnecessary.
        byte[] decrypted = [];
        var decryptionTimer = Stopwatch.StartNew();
        for (int index = 0; index < iterations; index++)
        {
            decrypted = RsaCryptoService.Decrypt(ciphertext, privateKey);
        }
        decryptionTimer.Stop();

        // Verification, CSV and Console output are outside both timed loops.
        bool roundTripPassed = CryptographicOperations.FixedTimeEquals(data, decrypted);
        var result = new RsaBenchmarkResult(timestamp, publicKey.KeySize, PaddingName, data.Length,
            iterations, encryptionTimer.Elapsed, decryptionTimer.Elapsed, roundTripPassed);
        AppendCsv(csvPath, result);
        PrintResult(result);
        Console.WriteLine($"\nResult saved: {csvPath}");
        Console.WriteLine($"RSA benchmark: {(result.Passed ? "PASS" : "FAIL")}");
        return result;
    }

    private static void AppendCsv(string path, RsaBenchmarkResult result)
    {
        using var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        bool needsHeader = file.Length == 0;
        if (!needsHeader)
        {
            using var reader = new StreamReader(file, Encoding.UTF8, true, 1024, leaveOpen: true);
            if (reader.ReadLine() != CsvHeader)
            {
                throw new InvalidDataException("Existing RSA benchmark CSV has an incompatible header; old data was preserved.");
            }
        }

        bool needsNewLine = false;
        if (!needsHeader)
        {
            file.Position = file.Length - 1;
            needsNewLine = file.ReadByte() != '\n';
        }

        file.Position = file.Length;
        using var writer = new StreamWriter(file, new UTF8Encoding(false));
        if (needsHeader)
        {
            writer.WriteLine(CsvHeader);
        }
        else if (needsNewLine)
        {
            writer.WriteLine();
        }

        string Number(double value) => value.ToString("F6", CultureInfo.InvariantCulture);
        string[] fields = [
            result.Timestamp.ToString("O", CultureInfo.InvariantCulture),
            result.KeySizeBits.ToString(CultureInfo.InvariantCulture), result.Padding,
            result.InputSizeBytes.ToString(CultureInfo.InvariantCulture),
            result.Iterations.ToString(CultureInfo.InvariantCulture),
            Number(result.EncryptionTime.TotalMilliseconds), Number(result.EncryptionAverageMs),
            Number(result.EncryptionOpsPerSecond), Number(result.DecryptionTime.TotalMilliseconds),
            Number(result.DecryptionAverageMs), Number(result.DecryptionOpsPerSecond),
            result.PrivateVsPublicRatio is double ratio ? Number(ratio) : "",
            result.RoundTripPassed ? "PASS" : "FAIL"
        ];
        writer.WriteLine(string.Join(",", fields));
    }

    private static void PrintResult(RsaBenchmarkResult result)
    {
        Console.WriteLine($"\nEncryption (public key):\nTotal: {result.EncryptionTime.TotalMilliseconds:F4} ms");
        Console.WriteLine($"Average: {result.EncryptionAverageMs:F6} ms\nOperations/sec: {result.EncryptionOpsPerSecond:F2}");
        Console.WriteLine($"\nDecryption (private key):\nTotal: {result.DecryptionTime.TotalMilliseconds:F4} ms");
        Console.WriteLine($"Average: {result.DecryptionAverageMs:F6} ms\nOperations/sec: {result.DecryptionOpsPerSecond:F2}");
        Console.WriteLine("\nPrivate/Public time ratio:");
        Console.WriteLine(result.PrivateVsPublicRatio is double ratio
            ? $"{ratio:F2}x (private decryption average / public encryption average)"
            : "N/A (both measured times must be positive)");
        Console.WriteLine($"\nRound-trip integrity: {(result.RoundTripPassed ? "PASS" : "FAIL")}");
        Console.WriteLine("Public key: (n, e); e is usually small, commonly 65537. Private key: (n, d); d is much larger.");
        Console.WriteLine("Private exponentiation usually costs more; CRT/provider optimizations and machine load also affect timing.");
        Console.WriteLine("Measured service calls include OAEP, allocations and loop overhead; key generation/setup, warm-up, verification and CSV are excluded.");
    }
}
