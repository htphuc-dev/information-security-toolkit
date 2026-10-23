namespace information_security_toolkit.WinForms.Forms;

public sealed class MainForm : ToolkitForm
{
    public MainForm() : base("Bộ công cụ An toàn thông tin", "Chọn một chức năng để quản lý khóa, mã hóa dữ liệu, kiểm tra hàm băm hoặc xác minh chữ ký số.")
    {
        ClientSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(20) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int row = 0; row < 3; row++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333f));
        Add("AES", "Mã hóa đối xứng", () => new AesForm(), 0, 0);
        Add("RSA", "Mã hóa bất đối xứng", () => new RsaForm(), 1, 0);
        Add("SHA-256", "Hàm băm", () => new HashForm(), 0, 1);
        Add("Chữ ký số", "RSA-PSS", () => new SignatureForm(), 1, 1);
        Add("Đánh giá hiệu năng", "AES / RSA", () => new BenchmarkForm(), 0, 2);
        var exit = new Button { Text = "Thoát", Dock = DockStyle.Fill, Margin = new Padding(10), Font = new Font(Font, FontStyle.Bold) };
        exit.Click += (_, _) => Close();
        grid.Controls.Add(exit, 1, 2);
        Body.Controls.Add(grid);
        Status("Sẵn sàng — hãy chọn một chức năng.");

        void Add(string caption, string subtitle, Func<Form> createForm, int column, int row)
        {
            var button = ActionButton($"{caption}\n{subtitle}", () =>
            {
                using Form form = createForm();
                form.ShowDialog(this);
                Status("Sẵn sàng — hãy chọn một chức năng.");
            });
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(10);
            button.Font = new Font(Font, FontStyle.Bold);
            grid.Controls.Add(button, column, row);
        }
    }
}
