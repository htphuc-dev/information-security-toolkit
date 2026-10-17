using System.Security.Cryptography;
using information_security_toolkit.AES;

Console.WriteLine("Information Security Toolkit");
Console.WriteLine("AES-256 Key Management Demo");

byte[] originalKey = AesKeyManager.GenerateKey();
Console.WriteLine($"Generated key length: {originalKey.Length} bytes ({originalKey.Length * 8} bits)");
Console.WriteLine($"Generated key (Base64): {Convert.ToBase64String(originalKey)}");

string keyDirectory = Path.Combine(AppContext.BaseDirectory, "keys");
Directory.CreateDirectory(keyDirectory);
string keyFilePath = Path.Combine(keyDirectory, "aes-demo.key");

AesKeyManager.SaveKey(originalKey, keyFilePath);
Console.WriteLine($"Key saved to: {keyFilePath}");

byte[] loadedKey = AesKeyManager.LoadKey(keyFilePath);
bool keysMatch = AesKeyManager.IsValidKey(originalKey)
    && AesKeyManager.IsValidKey(loadedKey)
    && CryptographicOperations.FixedTimeEquals(originalKey, loadedKey);

Console.WriteLine($"Generate -> Save -> Load -> Compare: {(keysMatch ? "PASS" : "FAIL")}");
return keysMatch ? 0 : 1;
