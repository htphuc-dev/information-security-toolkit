namespace information_security_toolkit.Common;

public static class FileComparison
{
    public static bool AreEqual(string firstPath, string secondPath)
    {
        const int BufferSize = 64 * 1024;
        using var first = new FileStream(firstPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var second = new FileStream(secondPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (first.Length != second.Length) return false;
        byte[] firstBuffer = new byte[BufferSize];
        byte[] secondBuffer = new byte[BufferSize];
        int count;
        while ((count = first.Read(firstBuffer)) != 0)
        {
            second.ReadExactly(secondBuffer.AsSpan(0, count));
            if (!firstBuffer.AsSpan(0, count).SequenceEqual(secondBuffer.AsSpan(0, count))) return false;
        }
        return true;
    }
}
