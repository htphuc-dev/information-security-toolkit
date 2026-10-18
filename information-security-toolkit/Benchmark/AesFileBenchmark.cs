using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using information_security_toolkit.AES;

namespace information_security_toolkit.Benchmark;

public static class AesFileBenchmark
{
    private const int BufferSize = 1024 * 1024;
    private const int WarmUpSize = 64 * 1024;
    private const long DiskSpaceMargin = 16 * 1024 * 1024;
    private const string CsvHeader = "Timestamp,FileName,FileSizeBytes,FileSizeMB,EncryptionTimeMs,"
        + "DecryptionTimeMs,EncryptionMBps,DecryptionMBps,SourceSHA256,DecryptedSHA256,IntegrityResult,Result,Mode,RunId";

    public static IReadOnlyList<AesBenchmarkResult> Run(string repositoryRoot, byte[] key, string mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(mode);
        if (!AesKeyManager.IsValidKey(key))
        {
            throw new ArgumentException("An AES-256 key must contain exactly 32 bytes.", nameof(key));
        }

        mode = mode.ToLowerInvariant();
        (string Name, long SizeMB)[] cases = mode switch
        {
            "quick" => [("aes-1mb.bin", 1), ("aes-10mb.bin", 10)],
            "full" => [("aes-100mb.bin", 100), ("aes-500mb.bin", 500), ("aes-1gb.bin", 1000)],
            _ => throw new ArgumentException("Benchmark mode must be 'quick' or 'full'.", nameof(mode))
        };

        repositoryRoot = Path.GetFullPath(repositoryRoot);
        string inputDirectory = Path.Combine(repositoryRoot, "test-data", "benchmark");
        string resultsDirectory = Path.Combine(repositoryRoot, "results", "benchmark");
        Directory.CreateDirectory(inputDirectory);
        Directory.CreateDirectory(resultsDirectory);
        Console.WriteLine($"Mode: {mode}");
        Console.WriteLine("Units: 1 MB = 1,000,000 bytes; 1 GB = 1,000 MB.");
        if (mode == "full")
        {
            Console.WriteLine("Full mode can retain over 4.8 GB of sources and outputs on its first run.");
        }

        long requiredSpace = DiskSpaceMargin + WarmUpSize * 3;
        foreach (var testCase in cases)
        {
            long sizeBytes = testCase.SizeMB * AesBenchmarkResult.BytesPerMegabyte;
            string inputPath = Path.Combine(inputDirectory, testCase.Name);
            if (File.Exists(inputPath))
            {
                ValidateFileSize(inputPath, sizeBytes);
            }
            else
            {
                requiredSpace += sizeBytes;
            }

            requiredSpace += sizeBytes * 2 + 1024; // Encrypted + decrypted, plus padding/header allowance.
        }

        CheckDiskSpace(repositoryRoot, requiredSpace);
        string runId = Guid.NewGuid().ToString("N");
        string outputDirectory = Path.Combine(resultsDirectory, $"run-{runId}");
        Directory.CreateDirectory(outputDirectory);
        string csvPath = Path.Combine(resultsDirectory, "aes-benchmark.csv");
        Console.WriteLine($"Output directory: {outputDirectory}");
        WarmUp(outputDirectory, key);

        var results = new List<AesBenchmarkResult>();
        foreach (var testCase in cases)
        {
            string inputPath = Path.Combine(inputDirectory, testCase.Name);
            EnsureTestFile(inputPath, testCase.SizeMB * AesBenchmarkResult.BytesPerMegabyte);
            AesBenchmarkResult result = MeasureFile(inputPath, outputDirectory, key);
            AppendCsv(csvPath, result, mode, runId);
            results.Add(result);
            PrintResult(result);
        }

        Console.WriteLine($"\nResult saved to: {csvPath}");
        Console.WriteLine($"AES benchmark: {(results.All(result => result.Passed) ? "PASS" : "FAIL")}");
        return results;
    }

    private static AesBenchmarkResult MeasureFile(string inputPath, string outputDirectory, byte[] key)
    {
        string fileName = Path.GetFileName(inputPath);
        string outputName = Path.GetFileNameWithoutExtension(inputPath);
        string encryptedPath = Path.Combine(outputDirectory, outputName + ".enc");
        string decryptedPath = Path.Combine(outputDirectory, outputName + ".dec");
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        // Hold the source open so its bytes cannot change between timing and verification on Windows.
        using var source = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            BufferSize, FileOptions.SequentialScan);
        long sourceSize = source.Length;
        Console.WriteLine($"\nFile: {fileName}\nSize: {sourceSize / (double)AesBenchmarkResult.BytesPerMegabyte:F2} MB");

        Console.WriteLine("Encrypting...");
        var encryptionTimer = Stopwatch.StartNew();
        AesFileService.EncryptFile(inputPath, encryptedPath, key);
        encryptionTimer.Stop();
        Console.WriteLine("Encryption: PASS");

        Console.WriteLine("Decrypting...");
        var decryptionTimer = Stopwatch.StartNew();
        AesFileService.DecryptFile(encryptedPath, decryptedPath, key);
        decryptionTimer.Stop();
        Console.WriteLine("Decryption: PASS");

        // Generation, hashing, CSV writing and Console output are outside both timers.
        string sourceHash = Convert.ToHexString(SHA256.HashData(source));
        using var decrypted = new FileStream(decryptedPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            BufferSize, FileOptions.SequentialScan);
        bool sizesEqual = sourceSize == decrypted.Length;
        string decryptedHash = Convert.ToHexString(SHA256.HashData(decrypted));
        return new AesBenchmarkResult(timestamp, fileName, sourceSize, encryptionTimer.Elapsed,
            decryptionTimer.Elapsed, sourceHash, decryptedHash, sizesEqual);
    }

    private static void EnsureTestFile(string path, long sizeBytes)
    {
        if (File.Exists(path))
        {
            ValidateFileSize(path, sizeBytes);
            Console.WriteLine($"Reusing test file: {path}");
            return;
        }

        Console.WriteLine($"Creating test file: {sizeBytes / AesBenchmarkResult.BytesPerMegabyte} MB — {path}");
        string temporaryPath = path + $".{Guid.NewGuid():N}.tmp";
        bool created = false;
        try
        {
            using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, BufferSize, FileOptions.SequentialScan))
            {
                created = true;
                // Repeating a random 1 MiB block provides fast patterned binary data with bounded memory.
                byte[] buffer = RandomNumberGenerator.GetBytes(BufferSize);
                long remaining = sizeBytes;
                while (remaining > 0)
                {
                    int count = (int)Math.Min(remaining, buffer.Length);
                    output.Write(buffer, 0, count);
                    remaining -= count;
                }
            }

            File.Move(temporaryPath, path);
            Console.WriteLine("Test file created successfully.");
        }
        finally
        {
            if (created)
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void ValidateFileSize(string path, long expectedSize)
    {
        if (new FileInfo(path).Length != expectedSize)
        {
            throw new InvalidDataException($"Existing benchmark file has the wrong size: {path}. "
                + $"Expected {expectedSize} bytes. Rename or remove it before retrying.");
        }
    }

    private static void WarmUp(string directory, byte[] key)
    {
        string input = Path.Combine(directory, "warmup.bin");
        string encrypted = Path.Combine(directory, "warmup.enc");
        string decrypted = Path.Combine(directory, "warmup.dec");
        Console.WriteLine("Warm-up: 64 KiB, excluded from measurements and CSV.");
        try
        {
            using (var file = new FileStream(input, FileMode.CreateNew, FileAccess.Write))
            {
                file.Write(RandomNumberGenerator.GetBytes(WarmUpSize));
            }

            AesFileService.EncryptFile(input, encrypted, key);
            AesFileService.DecryptFile(encrypted, decrypted, key);
        }
        finally
        {
            File.Delete(input);
            File.Delete(encrypted);
            File.Delete(decrypted);
        }
    }

    private static void CheckDiskSpace(string repositoryRoot, long requiredBytes)
    {
        long available;
        try
        {
            available = new DriveInfo(Path.GetPathRoot(repositoryRoot)!).AvailableFreeSpace;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Console.WriteLine($"Cannot query free disk space: {ex.Message}");
            Console.WriteLine($"Ensure at least {requiredBytes / (double)AesBenchmarkResult.BytesPerMegabyte:F2} MB is free.");
            return;
        }

        if (available < requiredBytes)
        {
            throw new IOException($"Not enough free disk space. Required approximately {requiredBytes} bytes; "
                + $"available {available} bytes. Previous benchmark outputs are retained.");
        }
    }

    private static void AppendCsv(string path, AesBenchmarkResult result, string mode, string runId)
    {
        using var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        bool needsHeader = file.Length == 0;
        if (!needsHeader)
        {
            using var reader = new StreamReader(file, Encoding.UTF8, true, 1024, leaveOpen: true);
            if (reader.ReadLine() != CsvHeader)
            {
                throw new InvalidDataException("Existing benchmark CSV has an incompatible header; old data was preserved.");
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
            result.Timestamp.ToString("O", CultureInfo.InvariantCulture), result.FileName,
            result.FileSizeBytes.ToString(CultureInfo.InvariantCulture), Number(result.FileSizeMB),
            Number(result.EncryptionTime.TotalMilliseconds), Number(result.DecryptionTime.TotalMilliseconds),
            Number(result.EncryptionMBps), Number(result.DecryptionMBps), result.SourceSHA256, result.DecryptedSHA256,
            result.IntegrityPassed ? "PASS" : "FAIL", result.Passed ? "PASS" : "FAIL", mode, runId
        ];
        writer.WriteLine(string.Join(",", fields.Select(value => "\"" + value.Replace("\"", "\"\"") + "\"")));
    }

    private static void PrintResult(AesBenchmarkResult result)
    {
        Console.WriteLine($"Encryption time: {result.EncryptionTime.TotalMilliseconds:F3} ms");
        Console.WriteLine($"Encryption throughput: {result.EncryptionMBps:F2} MB/s");
        Console.WriteLine($"Decryption time: {result.DecryptionTime.TotalMilliseconds:F3} ms");
        Console.WriteLine($"Decryption throughput: {result.DecryptionMBps:F2} MB/s");
        Console.WriteLine($"Source SHA-256: {result.SourceSHA256}");
        Console.WriteLine($"Decrypted SHA-256: {result.DecryptedSHA256}");
        Console.WriteLine($"File size equal: {result.FileSizesEqual}");
        Console.WriteLine($"Integrity: {(result.IntegrityPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"Result: {(result.Passed ? "PASS" : "FAIL")}");
    }
}
