namespace information_security_toolkit.Benchmark;

public sealed record AesBenchmarkResult(
    DateTimeOffset Timestamp,
    string FileName,
    long FileSizeBytes,
    TimeSpan EncryptionTime,
    TimeSpan DecryptionTime,
    string SourceSHA256,
    string DecryptedSHA256,
    bool FileSizesEqual)
{
    public const long BytesPerMegabyte = 1_000_000;

    public double FileSizeMB => FileSizeBytes / (double)BytesPerMegabyte;
    public double EncryptionMBps => EncryptionTime.TotalSeconds > 0
        ? FileSizeMB / EncryptionTime.TotalSeconds : 0;
    public double DecryptionMBps => DecryptionTime.TotalSeconds > 0
        ? FileSizeMB / DecryptionTime.TotalSeconds : 0;
    public bool IntegrityPassed => FileSizesEqual
        && !string.IsNullOrEmpty(SourceSHA256)
        && string.Equals(SourceSHA256, DecryptedSHA256, StringComparison.Ordinal);
    public bool Passed => IntegrityPassed && EncryptionMBps > 0 && DecryptionMBps > 0;
}
