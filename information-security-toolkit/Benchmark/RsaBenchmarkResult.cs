namespace information_security_toolkit.Benchmark;

public sealed record RsaBenchmarkResult(
    DateTimeOffset Timestamp,
    int KeySizeBits,
    string Padding,
    int InputSizeBytes,
    int Iterations,
    TimeSpan EncryptionTime,
    TimeSpan DecryptionTime,
    bool RoundTripPassed)
{
    public double EncryptionAverageMs => Iterations > 0
        ? EncryptionTime.TotalMilliseconds / Iterations : 0;
    public double DecryptionAverageMs => Iterations > 0
        ? DecryptionTime.TotalMilliseconds / Iterations : 0;
    public double EncryptionOpsPerSecond => Iterations > 0 && EncryptionTime > TimeSpan.Zero
        ? Iterations / EncryptionTime.TotalSeconds : 0;
    public double DecryptionOpsPerSecond => Iterations > 0 && DecryptionTime > TimeSpan.Zero
        ? Iterations / DecryptionTime.TotalSeconds : 0;
    public double? PrivateVsPublicRatio => Iterations > 0
        && EncryptionTime > TimeSpan.Zero && DecryptionTime > TimeSpan.Zero
        ? DecryptionAverageMs / EncryptionAverageMs : null;
    public bool Passed => RoundTripPassed && PrivateVsPublicRatio.HasValue;
}
