using information_security_toolkit.WinForms.Forms;

namespace information_security_toolkit.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
