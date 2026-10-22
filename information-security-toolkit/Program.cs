using System.Security.Cryptography;
using System.Text;
using information_security_toolkit.AES;
using information_security_toolkit.Benchmark;
using information_security_toolkit.Hashing;
using information_security_toolkit.RSA;
using information_security_toolkit.Signature;
using Rsa = System.Security.Cryptography.RSA;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("Information Security Toolkit");
bool benchmarkRequested = args.Length == 2
    && string.Equals(args[0], "benchmark", StringComparison.OrdinalIgnoreCase)
    && (string.Equals(args[1], "quick", StringComparison.OrdinalIgnoreCase)
        || string.Equals(args[1], "full", StringComparison.OrdinalIgnoreCase));
bool rsaKeysRequested = args.Length == 2
    && string.Equals(args[0], "rsa", StringComparison.OrdinalIgnoreCase)
    && string.Equals(args[1], "keys", StringComparison.OrdinalIgnoreCase);
bool rsaCryptoRequested = args.Length == 2
    && string.Equals(args[0], "rsa", StringComparison.OrdinalIgnoreCase)
    && string.Equals(args[1], "crypto", StringComparison.OrdinalIgnoreCase);
bool rsaBenchmarkRequested = args.Length == 3
    && string.Equals(args[0], "rsa", StringComparison.OrdinalIgnoreCase)
    && string.Equals(args[1], "benchmark", StringComparison.OrdinalIgnoreCase)
    && (string.Equals(args[2], "quick", StringComparison.OrdinalIgnoreCase)
        || string.Equals(args[2], "normal", StringComparison.OrdinalIgnoreCase)
        || string.Equals(args[2], "extended", StringComparison.OrdinalIgnoreCase));
bool sha256Requested = args.Length == 2
    && string.Equals(args[0], "hash", StringComparison.OrdinalIgnoreCase)
    && string.Equals(args[1], "sha256", StringComparison.OrdinalIgnoreCase);
bool signatureRequested = args.Length == 2
    && string.Equals(args[0], "signature", StringComparison.OrdinalIgnoreCase)
    && string.Equals(args[1], "demo", StringComparison.OrdinalIgnoreCase);
if (args.Length != 0 && !benchmarkRequested && !rsaKeysRequested && !rsaCryptoRequested
    && !rsaBenchmarkRequested && !sha256Requested && !signatureRequested)
{
    Console.Error.WriteLine("Usage: dotnet run --project information-security-toolkit/information-security-toolkit.csproj -- [benchmark quick|full | rsa keys|crypto | rsa benchmark quick|normal|extended | hash sha256 | signature demo]");
    return 1;
}

Console.WriteLine(signatureRequested ? "DIGITAL SIGNATURE — RSA-PSS"
    : sha256Requested ? "SHA-256 DEMO"
    : rsaBenchmarkRequested ? "RSA PERFORMANCE BENCHMARK"
    : rsaCryptoRequested ? "RSA ENCRYPT / DECRYPT DEMO"
    : rsaKeysRequested ? "RSA KEY MANAGEMENT DEMO"
    : benchmarkRequested ? "AES FILE BENCHMARK" : "AES File Encryption Demo");

try
{
    if (signatureRequested)
    {
        return RunDigitalSignatureDemo();
    }

    if (sha256Requested)
    {
        return RunSha256Demo();
    }

    string repositoryRoot = FindRepositoryRoot();
    if (rsaBenchmarkRequested)
    {
        RsaBenchmarkResult result = RsaBenchmark.Run(repositoryRoot, args[2]);
        return result.Passed ? 0 : 1;
    }

    if (rsaCryptoRequested)
    {
        return RunRsaCryptoDemo(repositoryRoot);
    }

    if (rsaKeysRequested)
    {
        return RunRsaKeyDemo(repositoryRoot);
    }

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
    string operation = signatureRequested ? "Digital signature demo"
        : sha256Requested ? "SHA-256 avalanche demo"
        : rsaBenchmarkRequested ? "RSA benchmark"
        : rsaCryptoRequested ? "RSA encryption demo"
        : rsaKeysRequested ? "RSA key management" : benchmarkRequested ? "AES benchmark" : "AES file demo";
    Console.Error.WriteLine($"{operation}: FAIL — {ex.Message}");
    return 1;
}

static int RunDigitalSignatureDemo()
{
    using var privateKey = RsaKeyManager.GenerateKeyPair(2048);
    using var publicKey = Rsa.Create();
    publicKey.ImportParameters(privateKey.ExportParameters(includePrivateParameters: false));
    Console.WriteLine("----------------------------------------");
    Console.WriteLine($"RSA key size: {privateKey.KeySize} bits\nHash: SHA-256\nPadding: RSA-PSS");

    string originalText = "Information Security Toolkit";
    byte[] data = Encoding.UTF8.GetBytes(originalText);
    Console.WriteLine($"\nOriginal data:\n{originalText}");
    byte[] signature = DigitalSignatureService.SignData(data, privateKey);
    Check(signature.Length == privateKey.KeySize / 8, "Signature created");
    Console.WriteLine($"Signature:\n{Convert.ToBase64String(signature)}");
    Check(DigitalSignatureService.VerifyData(data, signature, publicKey), "Verify original");

    byte[] modifiedData = Encoding.UTF8.GetBytes("Information Security Toolkiv");
    Check(!DigitalSignatureService.VerifyData(modifiedData, signature, publicKey), "Modified data rejected");

    using var otherKeyPair = RsaKeyManager.GenerateKeyPair(2048);
    using var wrongPublicKey = Rsa.Create();
    wrongPublicKey.ImportParameters(otherKeyPair.ExportParameters(includePrivateParameters: false));
    Check(!DigitalSignatureService.VerifyData(data, signature, wrongPublicKey), "Wrong public key rejected");

    byte[] tamperedSignature = (byte[])signature.Clone();
    tamperedSignature[tamperedSignature.Length / 2] ^= 1;
    Check(!DigitalSignatureService.VerifyData(data, tamperedSignature, publicKey), "Tampered signature rejected");

    byte[] emptyData = [];
    byte[] emptySignature = DigitalSignatureService.SignData(emptyData, privateKey);
    Check(DigitalSignatureService.VerifyData(emptyData, emptySignature, publicKey), "Empty data signature");
    string unicodeText = "An toàn và bảo mật thông tin ✓";
    byte[] unicodeData = Encoding.UTF8.GetBytes(unicodeText);
    byte[] unicodeSignature = DigitalSignatureService.SignData(unicodeData, privateKey);
    Console.WriteLine($"\nUnicode data: {unicodeText}");
    Check(DigitalSignatureService.VerifyData(unicodeData, unicodeSignature, publicKey), "Unicode signature");

    Console.WriteLine("\nRANDOMIZED SIGNATURE TEST\n----------------------------------------");
    Console.WriteLine("Same data: YES\nSame private key: YES");
    // Both signatures use the exact same data array and private-key object.
    byte[] secondSignature = DigitalSignatureService.SignData(data, privateKey);
    Console.WriteLine($"\nSignature #1:\n{Convert.ToBase64String(signature)}");
    Console.WriteLine($"\nSignature #2:\n{Convert.ToBase64String(secondSignature)}");
    Check(!signature.AsSpan().SequenceEqual(secondSignature), "Signatures different");
    Check(DigitalSignatureService.VerifyData(data, signature, publicKey), "Signature #1 verify");
    Check(DigitalSignatureService.VerifyData(data, secondSignature, publicKey), "Signature #2 verify");
    Console.WriteLine("RSA-PSS randomized signature test: PASS");
    Console.WriteLine("RSA-PSS uses a random salt: the same data and private key can produce different signatures.");
    Console.WriteLine("Both valid signatures can be verified using the corresponding public key.");

    Console.WriteLine("\nInput validation checks");
    ExpectRejected<CryptographicException>("Public-only key cannot sign",
        () => DigitalSignatureService.SignData(data, publicKey));
    Check(!DigitalSignatureService.VerifyData(data, [], publicKey), "Empty signature rejected");
    Check(!DigitalSignatureService.VerifyData(data, signature[..^1], publicKey), "Truncated signature rejected");
    ExpectRejected<ArgumentNullException>("Null signing data rejected",
        () => DigitalSignatureService.SignData(null!, privateKey));
    ExpectRejected<ArgumentNullException>("Null private key rejected",
        () => DigitalSignatureService.SignData(data, null!));
    ExpectRejected<ArgumentNullException>("Null verification data rejected",
        () => DigitalSignatureService.VerifyData(null!, signature, publicKey));
    ExpectRejected<ArgumentNullException>("Null signature rejected",
        () => DigitalSignatureService.VerifyData(data, null!, publicKey));
    ExpectRejected<ArgumentNullException>("Null public key rejected",
        () => DigitalSignatureService.VerifyData(data, signature, null!));

    Console.WriteLine("\nDigital signature demo: PASS");
    return 0;

    static void Check(bool passed, string label)
    {
        Console.WriteLine($"{label}: {(passed ? "PASS" : "FAIL")}");
        if (!passed)
        {
            throw new InvalidOperationException($"{label} failed.");
        }
    }
}

static int RunSha256Demo()
{
    string input = "hello";
    string hash = Sha256Service.ComputeHash(input);
    byte[] hashBytes = Convert.FromHexString(hash);
    Console.WriteLine($"Input: {input}\nSHA-256: {hash}");
    Console.WriteLine($"Hash length: {hashBytes.Length * 8} bits / {hashBytes.Length} bytes / {hash.Length} hex characters");
    Check(hashBytes.Length == Sha256Service.HashSizeInBytes
        && hash.Length == Sha256Service.HashSizeInBytes * 2, "Hash length test");
    Check(string.Equals(hash, Sha256Service.ComputeHash(input), StringComparison.Ordinal), "Deterministic hash test");

    string emptyHash = Sha256Service.ComputeHash(string.Empty);
    Console.WriteLine($"\nEmpty string SHA-256: {emptyHash}");
    Check(emptyHash.Length == Sha256Service.HashSizeInBytes * 2
        && Convert.FromHexString(emptyHash).Length == Sha256Service.HashSizeInBytes, "Empty string test");
    Check(string.Equals(emptyHash, Sha256Service.ComputeHash(Array.Empty<byte>()), StringComparison.Ordinal),
        "Empty byte array test");

    string unicode = "Xin chào! Tiếng Việt — 日本語 — 🔐🌍";
    string unicodeHash = Sha256Service.ComputeHash(unicode);
    Console.WriteLine($"\nUnicode input: {unicode}\nSHA-256: {unicodeHash}");
    Check(unicodeHash.Length == Sha256Service.HashSizeInBytes * 2
        && string.Equals(unicodeHash, Sha256Service.ComputeHash(Encoding.UTF8.GetBytes(unicode)), StringComparison.Ordinal),
        "Unicode UTF-8 string/byte hash test");

    Console.WriteLine("\nAVALANCHE EFFECT TEST");
    (string InputA, string InputB)[] comparisons = [
        ("hello", "Hello"),
        ("Hello world", "Hello worle"),
        ("abc", "abd"),
        ("An toàn và bảo mật thông tin", "An toàn và bảo mật thông tio")
    ];
    for (int index = 0; index < comparisons.Length; index++)
    {
        ShowComparison($"Test {index + 1}", comparisons[index].InputA, comparisons[index].InputB);
    }

    ShowComparison("Identical inputs", "hello", "hello");
    ExpectRejected<ArgumentNullException>("Null string rejected",
        () => Sha256Service.ComputeHash((string)null!));
    ExpectRejected<ArgumentNullException>("Null byte array rejected",
        () => Sha256Service.ComputeHash((byte[])null!));
    ExpectRejected<ArgumentException>("Invalid SHA-256 hash length rejected",
        () => Sha256Service.CountDifferentBits(new byte[Sha256Service.HashSizeInBytes - 1], hashBytes));
    Console.WriteLine("\nSHA-256 avalanche demo: PASS");
    return 0;

    static void ShowComparison(string label, string inputA, string inputB)
    {
        string hashA = Sha256Service.ComputeHash(inputA);
        string hashB = Sha256Service.ComputeHash(inputB);
        int differentBits = Sha256Service.CountDifferentBits(Convert.FromHexString(hashA), Convert.FromHexString(hashB));
        double differencePercentage = differentBits / (double)Sha256Service.HashSizeInBits * 100;
        bool identicalInputs = string.Equals(inputA, inputB, StringComparison.Ordinal);
        bool hashesEqual = string.Equals(hashA, hashB, StringComparison.Ordinal);
        Console.WriteLine($"\n{label}\nInput A: {inputA}\nInput B: {inputB}");
        Console.WriteLine($"Hash A: {hashA}\nHash B: {hashB}");
        Console.WriteLine($"Different bits (Hamming distance): {differentBits} / {Sha256Service.HashSizeInBits}");
        Console.WriteLine($"Difference: {differencePercentage:F2}%");
        // Avalanche examples require different hashes; the percentage is measured, not forced to 50%.
        bool passed = identicalInputs ? hashesEqual && differentBits == 0 && differencePercentage == 0
            : !hashesEqual && differentBits > 0;
        Check(passed, identicalInputs ? "Identical inputs test" : "Hashes are different");
    }

    static void Check(bool passed, string label)
    {
        Console.WriteLine($"{label}: {(passed ? "PASS" : "FAIL")}");
        if (!passed)
        {
            throw new InvalidOperationException($"{label} failed.");
        }
    }
}

static int RunRsaKeyDemo(string repositoryRoot)
{
    using var original = RsaKeyManager.GenerateKeyPair();
    Console.WriteLine($"Key size: {original.KeySize} bits");
    string directory = Path.Combine(repositoryRoot, "keys", "rsa", $"run-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    string publicPath = Path.Combine(directory, "rsa-public.pem");
    string privatePath = Path.Combine(directory, "rsa-private.pem");

    RsaKeyManager.ExportPublicKey(original, publicPath);
    Console.WriteLine($"Public key exported: PASS — {publicPath}");
    RsaKeyManager.ExportPrivateKey(original, privatePath);
    Console.WriteLine($"Private key exported: PASS — {privatePath}");
    using var publicKey = RsaKeyManager.LoadPublicKey(publicPath);
    Console.WriteLine("Public key imported: PASS");
    using var privateKey = RsaKeyManager.LoadPrivateKey(privatePath);
    Console.WriteLine("Private key imported: PASS");

    RSAParameters originalParameters = PemAnalyzer.GetParameters(original);
    RSAParameters publicParameters = PemAnalyzer.GetParameters(publicKey);
    RSAParameters privatePublicParameters = PemAnalyzer.GetParameters(privateKey);
    bool publicMatch = originalParameters.Modulus.AsSpan().SequenceEqual(publicParameters.Modulus);
    bool privateMatch = originalParameters.Modulus.AsSpan().SequenceEqual(privatePublicParameters.Modulus);
    bool exponentsMatch = originalParameters.Exponent.AsSpan().SequenceEqual(publicParameters.Exponent)
        && originalParameters.Exponent.AsSpan().SequenceEqual(privatePublicParameters.Exponent);
    Console.WriteLine($"Public modulus match: {(publicMatch ? "PASS" : "FAIL")}");
    Console.WriteLine($"Private modulus match: {(privateMatch ? "PASS" : "FAIL")}");
    Console.WriteLine($"Public/private exponents match: {(exponentsMatch ? "PASS" : "FAIL")}");
    PemAnalyzer.PrintParameters(publicKey);
    PemAnalyzer.PrintParameters(privateKey, includePrivateParameters: true);

    Console.WriteLine("\nPEM failure handling checks");
    string missingPath = Path.Combine(directory, "does-not-exist.pem");
    ExpectRejected<FileNotFoundException>("Missing public PEM rejected", () =>
    {
        using var unexpected = RsaKeyManager.LoadPublicKey(missingPath);
    });
    ExpectRejected<FileNotFoundException>("Missing private PEM rejected", () =>
    {
        using var unexpected = RsaKeyManager.LoadPrivateKey(missingPath);
    });

    string invalidPath = Path.Combine(directory, "invalid.pem");
    File.WriteAllText(invalidPath, "This is not a PEM key.");
    ExpectRejected<FormatException>("Malformed public PEM rejected", () =>
    {
        using var unexpected = RsaKeyManager.LoadPublicKey(invalidPath);
    });
    ExpectRejected<FormatException>("Malformed private PEM rejected", () =>
    {
        using var unexpected = RsaKeyManager.LoadPrivateKey(invalidPath);
    });
    ExpectRejected<FormatException>("Private PEM passed to public loader rejected", () =>
    {
        using var unexpected = RsaKeyManager.LoadPublicKey(privatePath);
    });
    ExpectRejected<FormatException>("Public PEM passed to private loader rejected", () =>
    {
        using var unexpected = RsaKeyManager.LoadPrivateKey(publicPath);
    });
    ExpectRejected<ArgumentOutOfRangeException>("Unsupported key size rejected", () =>
    {
        using var unexpected = RsaKeyManager.GenerateKeyPair(1024);
    });

    bool passed = publicMatch && privateMatch && exponentsMatch;
    Console.WriteLine($"\nRSA key management result: {(passed ? "PASS" : "FAIL")}");
    return passed ? 0 : 1;
}

static int RunRsaCryptoDemo(string repositoryRoot)
{
    string runId = Guid.NewGuid().ToString("N");
    string keyDirectory = Path.Combine(repositoryRoot, "keys", "rsa", $"run-{runId}");
    string outputDirectory = Path.Combine(repositoryRoot, "results", "rsa", $"run-{runId}");
    Directory.CreateDirectory(keyDirectory);
    Directory.CreateDirectory(outputDirectory);
    string publicPath = Path.Combine(keyDirectory, "rsa-public.pem");
    string privatePath = Path.Combine(keyDirectory, "rsa-private.pem");
    using (var generated = RsaKeyManager.GenerateKeyPair())
    {
        RsaKeyManager.ExportPublicKey(generated, publicPath);
        RsaKeyManager.ExportPrivateKey(generated, privatePath);
    }

    using var publicKey = RsaKeyManager.LoadPublicKey(publicPath);
    using var privateKey = RsaKeyManager.LoadPrivateKey(privatePath);
    int maximumSize = RsaCryptoService.GetMaxPlaintextSize(publicKey);
    Console.WriteLine("Public/private PEM imported: PASS");
    Console.WriteLine($"Key size: {publicKey.KeySize} bits\nPadding: OAEP-SHA256");
    Console.WriteLine($"Max plaintext size: {maximumSize} bytes");

    // Fresh demonstration data only; do not use the AES key from another exercise.
    byte[] data = RandomNumberGenerator.GetBytes(32);
    Console.WriteLine($"Original data length: {data.Length} bytes");
    byte[] ciphertext = RsaCryptoService.Encrypt(data, publicKey);
    Console.WriteLine($"Encryption: PASS\nCiphertext length: {ciphertext.Length} bytes");
    string base64 = Convert.ToBase64String(ciphertext);
    Console.WriteLine($"Ciphertext Base64: {base64}");
    string ciphertextPath = Path.Combine(outputDirectory, "rsa-encrypted.txt");
    using (var file = new FileStream(ciphertextPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
    using (var writer = new StreamWriter(file, new UTF8Encoding(false)))
    {
        writer.Write(base64);
    }

    Console.WriteLine($"Base64 saved: PASS — {ciphertextPath}");
    byte[] loadedCiphertext = Convert.FromBase64String(File.ReadAllText(ciphertextPath));
    if (!ciphertext.AsSpan().SequenceEqual(loadedCiphertext))
    {
        throw new InvalidDataException("The saved Base64 ciphertext does not match the encrypted bytes.");
    }

    Console.WriteLine("Base64 loaded: PASS");
    byte[] decrypted = RsaCryptoService.Decrypt(loadedCiphertext, privateKey);
    Console.WriteLine("Decryption: PASS");
    if (!CryptographicOperations.FixedTimeEquals(data, decrypted))
    {
        throw new InvalidOperationException("Original vs decrypted: FAIL");
    }

    Console.WriteLine("Original vs decrypted: PASS");
    Console.WriteLine("\nPlaintext size checks");
    foreach (int size in new[] { 1, 32, 100, maximumSize })
    {
        byte[] sample = RandomNumberGenerator.GetBytes(size);
        byte[] encrypted = RsaCryptoService.Encrypt(sample, publicKey);
        byte[] recovered = RsaCryptoService.Decrypt(encrypted, privateKey);
        if (!CryptographicOperations.FixedTimeEquals(sample, recovered))
        {
            throw new InvalidOperationException($"{size}-byte round-trip: FAIL");
        }

        Console.WriteLine($"{size} byte{(size == maximumSize ? " (max allowed)" : "")}: PASS");
    }

    string text = "Hello RSA";
    byte[] textCiphertext = RsaCryptoService.Encrypt(Encoding.UTF8.GetBytes(text), publicKey);
    string recoveredText = Encoding.UTF8.GetString(RsaCryptoService.Decrypt(textCiphertext, privateKey));
    if (!string.Equals(text, recoveredText, StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Small UTF-8 text round-trip: FAIL");
    }

    Console.WriteLine("Hello RSA text round-trip: PASS");
    Console.WriteLine("\nRSA failure handling checks");
    using var wrongPrivateKey = RsaKeyManager.GenerateKeyPair();
    ExpectRejected<CryptographicException>("Wrong key rejected",
        () => RsaCryptoService.Decrypt(ciphertext, wrongPrivateKey));
    byte[] tamperedCiphertext = (byte[])ciphertext.Clone();
    tamperedCiphertext[tamperedCiphertext.Length / 2] ^= 1;
    ExpectRejected<CryptographicException>("Tampered ciphertext rejected",
        () => RsaCryptoService.Decrypt(tamperedCiphertext, privateKey));
    ExpectRejected<ArgumentException>("Oversized plaintext rejected",
        () => RsaCryptoService.Encrypt(RandomNumberGenerator.GetBytes(maximumSize + 1), publicKey));
    ExpectRejected<CryptographicException>("Public-only key cannot decrypt",
        () => RsaCryptoService.Decrypt(ciphertext, publicKey));
    ExpectRejected<ArgumentException>("Invalid ciphertext length rejected",
        () => RsaCryptoService.Decrypt(ciphertext[..^1], privateKey));
    Console.WriteLine("\nRSA encryption demo: PASS");
    return 0;
}

static void ExpectRejected<T>(string label, Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T ex)
    {
        Console.WriteLine($"{label}: PASS — {ex.Message}");
        return;
    }

    throw new InvalidOperationException($"{label}: expected input rejection.");
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
