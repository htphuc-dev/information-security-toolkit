using System.Security.Cryptography;
using System.Text;
using information_security_toolkit.AES;
using information_security_toolkit.Benchmark;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("Information Security Toolkit");
bool benchmarkRequested = args.Length == 2
    && string.Equals(args[0], "benchmark", StringComparison.OrdinalIgnoreCase)
    && (string.Equals(args[1], "quick", StringComparison.OrdinalIgnoreCase)
        || string.Equals(args[1], "full", StringComparison.OrdinalIgnoreCase));
if (args.Length != 0 && !benchmarkRequested)
{
    Console.Error.WriteLine("Usage: dotnet run --project information-security-toolkit/information-security-toolkit.csproj -- [benchmark quick|full]");
    return 1;
}

Console.WriteLine(benchmarkRequested ? "AES FILE BENCHMARK" : "AES File Encryption Demo");

try
{
    string repositoryRoot = FindRepositoryRoot();
    string keyDirectory = Path.Combine(AppContext.BaseDirectory, "keys");
    Directory.CreateDirectory(keyDirectory);
    string keyFilePath = Path.Combine(keyDirectory, "aes-demo.key");

    byte[] key;
    if (File.Exists(keyFilePath))
    {
        key = AesKeyManager.LoadKey(keyFilePath);
        Console.WriteLine($"Loaded AES-256 key from: {keyFilePath}");
    }
    else
    {
        key = AesKeyManager.GenerateKey();
        AesKeyManager.SaveKey(key, keyFilePath);
        Console.WriteLine($"Created AES-256 key at: {keyFilePath}");
    }

    if (benchmarkRequested)
    {
        var benchmarkResults = AesFileBenchmark.Run(repositoryRoot, key, args[1]);
        return benchmarkResults.All(result => result.Passed) ? 0 : 1;
    }

    string testDataDirectory = Path.Combine(repositoryRoot, "test-data");
    string resultsDirectory = Path.Combine(repositoryRoot, "results");
    Directory.CreateDirectory(testDataDirectory);
    Directory.CreateDirectory(resultsDirectory);
    string textPath = Path.Combine(testDataDirectory, "sample.txt");
    if (!File.Exists(textPath))
    {
        File.WriteAllText(textPath, "AES file encryption demo.\nLine two.\n", Encoding.UTF8);
    }

    string unicodePath = Path.Combine(testDataDirectory, "sample-unicode.txt");
    if (!File.Exists(unicodePath))
    {
        using var writer = new StreamWriter(unicodePath, false, new UTF8Encoding(false));
        for (int line = 1; line <= 32; line++)
        {
            writer.WriteLine($"Dòng {line}: Xin chào! Bài tập An toàn và bảo mật thông tin.");
            writer.WriteLine("Unicode: Tiếng Việt — 日本語 — 🔐🌍");
        }
    }

    string emptyPath = Path.Combine(testDataDirectory, "sample-empty.bin");
    if (!File.Exists(emptyPath))
    {
        using var empty = new FileStream(emptyPath, FileMode.CreateNew, FileAccess.Write);
    }

    string binaryPath = Path.Combine(testDataDirectory, "sample-binary.bin");
    if (!File.Exists(binaryPath))
    {
        using var binary = new FileStream(binaryPath, FileMode.CreateNew, FileAccess.Write);
        binary.Write(RandomNumberGenerator.GetBytes(4096));
    }

    // New output names on every run preserve all existing files.
    string outputDirectory = Path.Combine(resultsDirectory, $"run-{Guid.NewGuid():N}");
    Directory.CreateDirectory(outputDirectory);
    Console.WriteLine($"Test data directory: {testDataDirectory}");
    Console.WriteLine($"Results directory: {outputDirectory}");
    var inputPaths = Directory.EnumerateFiles(testDataDirectory)
        .Where(path => !string.Equals(Path.GetFileName(path), "README.md", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Path.GetFileName(path), ".gitkeep", StringComparison.OrdinalIgnoreCase))
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
    foreach (string inputPath in inputPaths)
    {
        RunRoundTrip(Path.GetFileName(inputPath), inputPath, outputDirectory, key);
    }

    Console.WriteLine("\nFailure handling checks");
    string checksDirectory = Path.Combine(outputDirectory, "checks");
    Directory.CreateDirectory(checksDirectory);
    string encryptedUnicodePath = Path.Combine(outputDirectory, Path.GetFileName(unicodePath) + ".enc");
    string wrongKeyOutput = Path.Combine(checksDirectory, "wrong-key.decrypted");
    ExpectFailure<CryptographicException>("Wrong key rejected",
        () => AesFileService.DecryptFile(encryptedUnicodePath, wrongKeyOutput, AesKeyManager.GenerateKey()),
        wrongKeyOutput);

    string tamperedPath = Path.Combine(checksDirectory, "tampered.enc");
    File.Copy(encryptedUnicodePath, tamperedPath);
    using (var tampered = new FileStream(tamperedPath, FileMode.Open, FileAccess.ReadWrite))
    {
        tampered.Seek(-1, SeekOrigin.End);
        int lastByte = tampered.ReadByte();
        tampered.Seek(-1, SeekOrigin.End);
        tampered.WriteByte((byte)(lastByte ^ 1));
    }

    string tamperedOutput = Path.Combine(checksDirectory, "tampered.decrypted");
    ExpectFailure<CryptographicException>("Modified file rejected",
        () => AesFileService.DecryptFile(tamperedPath, tamperedOutput, key), tamperedOutput);

    string missingOutput = Path.Combine(checksDirectory, "missing.enc");
    ExpectFailure<FileNotFoundException>("Missing input rejected",
        () => AesFileService.EncryptFile(Path.Combine(checksDirectory, "does-not-exist.bin"), missingOutput, key),
        missingOutput);

    string invalidOutput = Path.Combine(checksDirectory, "missing-directory", "output.enc");
    ExpectFailure<DirectoryNotFoundException>("Invalid output path rejected",
        () => AesFileService.EncryptFile(unicodePath, invalidOutput, key), invalidOutput);

    string invalidFormatOutput = Path.Combine(checksDirectory, "invalid-format.decrypted");
    ExpectFailure<InvalidDataException>("Invalid file format rejected",
        () => AesFileService.DecryptFile(unicodePath, invalidFormatOutput, key), invalidFormatOutput);

    if (Directory.EnumerateFiles(outputDirectory, ".aes-*.tmp", SearchOption.AllDirectories).Any())
    {
        throw new InvalidOperationException("Temporary output files were not cleaned up.");
    }

    Console.WriteLine("\nAES file demo: PASS");
    return 0;
}
catch (Exception ex) when (ex is ArgumentException or FormatException or CryptographicException
    or IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException)
{
    Console.Error.WriteLine($"{(benchmarkRequested ? "AES benchmark" : "AES file demo")}: FAIL — {ex.Message}");
    return 1;
}

static string FindRepositoryRoot()
{
    for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
    {
        if (File.Exists(Path.Combine(directory.FullName, "information-security-toolkit.slnx")))
        {
            return directory.FullName;
        }
    }

    throw new DirectoryNotFoundException("Cannot find the repository root containing information-security-toolkit.slnx.");
}

static string RunRoundTrip(string label, string inputPath, string outputDirectory, byte[] key)
{
    string encryptedPath = Path.Combine(outputDirectory, Path.GetFileName(inputPath) + ".enc");
    string decryptedPath = Path.Combine(outputDirectory, Path.GetFileName(inputPath) + ".decrypted");
    Console.WriteLine($"\n{label}");
    Console.WriteLine($"Input file: {inputPath}");
    Console.WriteLine($"Encrypted file: {encryptedPath}");
    Console.WriteLine($"Decrypted file: {decryptedPath}");

    AesFileService.EncryptFile(inputPath, encryptedPath, key);
    Console.WriteLine("Encryption: PASS");
    AesFileService.DecryptFile(encryptedPath, decryptedPath, key);
    Console.WriteLine("Decryption: PASS");
    if (!FilesMatch(inputPath, decryptedPath))
    {
        throw new InvalidOperationException($"{label}: file comparison failed.");
    }

    Console.WriteLine("File comparison: PASS");
    return encryptedPath;
}

static bool FilesMatch(string firstPath, string secondPath)
{
    const int ComparisonBufferSize = 64 * 1024;
    using var first = new FileStream(firstPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    using var second = new FileStream(secondPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    if (first.Length != second.Length)
    {
        return false;
    }

    byte[] firstBuffer = new byte[ComparisonBufferSize];
    byte[] secondBuffer = new byte[ComparisonBufferSize];
    int bytesRead;
    while ((bytesRead = first.Read(firstBuffer, 0, firstBuffer.Length)) != 0)
    {
        second.ReadExactly(secondBuffer.AsSpan(0, bytesRead));
        if (!firstBuffer.AsSpan(0, bytesRead).SequenceEqual(secondBuffer.AsSpan(0, bytesRead)))
        {
            return false;
        }
    }

    return true;
}

static void ExpectFailure<T>(string label, Action action, string rejectedOutputPath) where T : Exception
{
    bool rejected = false;
    try
    {
        action();
    }
    catch (T ex)
    {
        rejected = true;
        Console.WriteLine($"{label}: PASS — {ex.Message}");
    }

    if (!rejected || File.Exists(rejectedOutputPath))
    {
        throw new InvalidOperationException($"{label}: expected an error and no output file.");
    }
}
