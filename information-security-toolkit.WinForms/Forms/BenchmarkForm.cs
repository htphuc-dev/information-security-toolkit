using System.Security.Cryptography;
using information_security_toolkit.AES;
using information_security_toolkit.Benchmark;

namespace information_security_toolkit.WinForms.Forms;

public sealed class BenchmarkForm : ToolkitForm
{
    private sealed record BenchmarkMode(string Value, string Caption);

    private readonly ComboBox algorithm = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, AccessibleName = "Thuật toán" };
    private readonly ComboBox mode = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, AccessibleName = "Mức đánh giá",
        DisplayMember = nameof(BenchmarkMode.Caption)
    };
    private readonly Label description = Ui.Label("");
    private readonly TextBox details = Ui.TextBox("Chi tiết đánh giá hiệu năng", readOnly: true);
    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = Color.White, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AccessibleName = "Kết quả đánh giá hiệu năng", Margin = new Padding(0, 8, 0, 10)
    };

    public BenchmarkForm() : base("Đánh giá hiệu năng", "Chọn thuật toán và mức đánh giá, sau đó nhấn Chạy đánh giá • Chương trình không tự động chạy.")
    {
        foreach (string caption in new[] { "Thuật toán", "Kích thước dữ liệu", "Số lần lặp", "Thời gian mã hóa (ms)", "Thời gian giải mã (ms)", "Tốc độ mã hóa", "Tốc độ giải mã", "Kết quả" })
        {
            int index = grid.Columns.Add(caption, caption);
            grid.Columns[index].MinimumWidth = 70;
            grid.Columns[index].SortMode = DataGridViewColumnSortMode.NotSortable;
        }
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        Body.Controls.Add(Ui.Column(
            (Ui.Buttons(Ui.Label("Thuật toán"), algorithm, Ui.Label("Mức đánh giá"), mode, ActionButton("Chạy đánh giá", RunAsync)), 0),
            (description, 0), (grid, 70), (Ui.Field("Tóm tắt / Kết quả CSV", details), 30)));
        algorithm.Items.AddRange(["AES", "RSA"]);
        algorithm.SelectedIndexChanged += (_, _) =>
        {
            if (algorithm.SelectedIndex < 0) return;
            mode.Items.Clear();
            mode.Items.AddRange(algorithm.SelectedIndex == 0
                ? [new BenchmarkMode("quick", "Nhanh"), new BenchmarkMode("full", "Đầy đủ")]
                : [new BenchmarkMode("quick", "Nhanh"), new BenchmarkMode("normal", "Tiêu chuẩn"), new BenchmarkMode("extended", "Mở rộng")]);
            mode.SelectedIndex = 0;
        };
        mode.SelectedIndexChanged += (_, _) =>
        {
            if (mode.SelectedIndex < 0) return;
            grid.Rows.Clear(); details.Clear();
            description.Text = algorithm.SelectedIndex == 0
                ? mode.SelectedIndex == 0 ? "AES — Nhanh: tệp 1 MB + 10 MB • AES-256-CBC + HMAC-SHA256"
                    : "AES — Đầy đủ: 100 MB + 500 MB + 1 GB • Lần chạy đầu có thể giữ lại hơn 4,8 GB dữ liệu."
                : $"RSA: 2048 bit • OAEP-SHA256 • Dữ liệu 32 byte • {new[] { 100, 1000, 10000 }[mode.SelectedIndex]} lần lặp";
            Status("Sẵn sàng — nhấn Chạy đánh giá để bắt đầu với mức đã chọn.");
        };
        algorithm.SelectedIndex = 0;
    }

    private async Task RunAsync()
    {
        string root = AppPaths.RepositoryRoot;
        string selectedMode = ((BenchmarkMode)mode.SelectedItem!).Value;
        grid.Rows.Clear(); details.Clear();
        if (algorithm.SelectedIndex == 0)
        {
            byte[] key = AesKeyManager.GenerateKey();
            IReadOnlyList<AesBenchmarkResult> results;
            try { results = await Task.Run(() => AesFileBenchmark.Run(root, key, selectedMode)); }
            finally { CryptographicOperations.ZeroMemory(key); }
            foreach (AesBenchmarkResult result in results)
            {
                grid.Rows.Add("AES", $"{result.FileSizeMB:F0} MB", "1 / tệp", $"{result.EncryptionTime.TotalMilliseconds:F3}",
                    $"{result.DecryptionTime.TotalMilliseconds:F3}", $"{result.EncryptionMBps:F2} MB/s",
                    $"{result.DecryptionMBps:F2} MB/s", result.Passed ? "ĐẠT" : "KHÔNG ĐẠT");
            }
            details.Text = $"CSV: {Path.Combine(root, "results", "benchmark", "aes-benchmark.csv")}\r\n"
                + string.Join("\r\n", results.Select(result => $"{result.FileName}: kiểm tra kích thước và tính toàn vẹn SHA-256 — {(result.IntegrityPassed ? "ĐẠT" : "KHÔNG ĐẠT")}"));
            bool passed = results.All(result => result.Passed);
            Status($"Đánh giá hiệu năng AES — {mode.Text}: {(passed ? "ĐẠT" : "KHÔNG ĐẠT")}", passed);
        }
        else
        {
            RsaBenchmarkResult result = await Task.Run(() => RsaBenchmark.Run(root, selectedMode));
            grid.Rows.Add("RSA", $"{result.InputSizeBytes} byte", result.Iterations, $"{result.EncryptionTime.TotalMilliseconds:F3}",
                $"{result.DecryptionTime.TotalMilliseconds:F3}", $"{result.EncryptionOpsPerSecond:F2} lần/giây",
                $"{result.DecryptionOpsPerSecond:F2} lần/giây", result.Passed ? "ĐẠT" : "KHÔNG ĐẠT");
            string ratio = result.PrivateVsPublicRatio is double value ? $"{value:F2} lần" : "Chưa xác định";
            details.Text = $"Mã hóa trung bình: {result.EncryptionAverageMs:F6} ms • Giải mã trung bình: {result.DecryptionAverageMs:F6} ms\r\n"
                + $"Tỷ lệ thời gian giải mã / mã hóa: {ratio} • Dữ liệu sau giải mã khớp dữ liệu gốc: {(result.RoundTripPassed ? "ĐẠT" : "KHÔNG ĐẠT")}\r\n"
                + $"CSV: {Path.Combine(root, "results", "benchmark", "rsa-benchmark.csv")}";
            Status($"Đánh giá hiệu năng RSA — {mode.Text}: {(result.Passed ? "ĐẠT" : "KHÔNG ĐẠT")}", result.Passed);
        }
    }
}
