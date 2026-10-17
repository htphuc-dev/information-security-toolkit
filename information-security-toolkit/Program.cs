using System.Security.Cryptography;
using System.Text;
using information_security_toolkit.AES;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("Information Security Toolkit");
Console.WriteLine("AES-256-GCM Text Encryption Demo");

try
{
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

    Console.Write("Enter text (press Enter to use sample): ");
    string? input = Console.ReadLine();
    string plaintext = string.IsNullOrEmpty(input)
        ? "Xin chào! Đây là bài tập AES-256-GCM."
        : input;
    Console.WriteLine($"Original plaintext: {plaintext}");

    string encryptedData = AesTextService.Encrypt(plaintext, key);
    string secondEncryptedData = AesTextService.Encrypt(plaintext, key);
    Console.WriteLine($"Encrypted data 1 (Base64): {encryptedData}");
    Console.WriteLine($"Encrypted data 2 (Base64): {secondEncryptedData}");

    string decryptedText = AesTextService.Decrypt(encryptedData, key);
    Console.WriteLine($"Decrypted plaintext: {decryptedText}");

    bool roundTripPassed = string.Equals(plaintext, decryptedText, StringComparison.Ordinal);
    bool nonceCheckPassed = !string.Equals(encryptedData, secondEncryptedData, StringComparison.Ordinal);
    Console.WriteLine($"Plaintext round-trip: {(roundTripPassed ? "PASS" : "FAIL")}");
    Console.WriteLine($"Two encryptions produce different output: {(nonceCheckPassed ? "PASS" : "FAIL")}");

    return roundTripPassed && nonceCheckPassed ? 0 : 1;
}
catch (Exception ex) when (ex is ArgumentException or FormatException or CryptographicException
    or IOException or UnauthorizedAccessException or NotSupportedException)
{
    Console.Error.WriteLine($"ERROR: {ex.Message}");
    return 1;
}
