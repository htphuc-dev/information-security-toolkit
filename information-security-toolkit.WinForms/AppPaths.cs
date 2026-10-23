namespace information_security_toolkit.WinForms;

internal static class AppPaths
{
    public static string RepositoryRoot
    {
        get
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "information-security-toolkit.slnx")))
                    return directory.FullName;
            }
            throw new DirectoryNotFoundException("Không tìm thấy information-security-toolkit.slnx. Hãy chạy ứng dụng từ repository của project.");
        }
    }
}
