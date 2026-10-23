namespace information_security_toolkit.WinForms.Forms;

public class ToolkitForm : Form
{
    protected Panel Body { get; } = new() { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 12) };
    protected string? OperationFilePath { get; set; }
    protected string? OperationStage { get; set; }
    private readonly Label status = new()
    {
        Text = "Sẵn sàng", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
        BackColor = Color.FromArgb(243, 244, 246), Padding = new Padding(10, 0, 10, 0), AutoEllipsis = true
    };
    private bool busy;

    protected ToolkitForm(string title, string description)
    {
        Text = title;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(980, 680);
        MinimumSize = new Size(840, 600);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        var heading = Ui.Label(title);
        heading.Font = new Font("Segoe UI", 17, FontStyle.Bold);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 3 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.Controls.Add(Ui.Column((heading, 0), (Ui.Label(description), 0)), 0, 0);
        root.Controls.Add(Body, 0, 1);
        root.Controls.Add(status, 0, 2);
        Controls.Add(root);
    }

    protected void Status(string message, bool passed = true)
    {
        status.Text = message;
        status.ForeColor = passed ? Color.FromArgb(25, 95, 57) : Color.FromArgb(170, 30, 30);
    }

    protected Button ActionButton(string text, Action action) => ActionButton(text, () =>
    {
        action();
        return Task.CompletedTask;
    });

    protected Button ActionButton(string text, Func<Task> action)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, MinimumSize = new Size(126, 36),
            Padding = new Padding(8, 3, 8, 3), Margin = new Padding(0, 0, 8, 8)
        };
        button.Click += async (_, _) =>
        {
            if (busy) return;
            OperationFilePath = null;
            OperationStage = null;
            busy = true;
            Body.Enabled = false;
            UseWaitCursor = true;
            Status("Đang xử lý…");
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                // UI boundary: show the failure instead of swallowing it or crashing an async event handler.
                string message = ErrorMessage(ex);
                string details = ErrorDetails(ex, text, OperationStage, OperationFilePath);
                System.Diagnostics.Debug.WriteLine(details);
                System.Diagnostics.Debug.WriteLine(ex);
                Status($"Không thành công — {message}", false);
                MessageBox.Show(this, $"{message}\n\nThông tin chẩn đoán:\n{details}",
                    "Không thể hoàn tất thao tác", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                busy = false;
                OperationFilePath = null;
                OperationStage = null;
                UseWaitCursor = false;
                Body.Enabled = true;
            }
        };
        return button;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (busy)
        {
            e.Cancel = true;
            Status("Một thao tác đang chạy. Vui lòng chờ hoàn tất trước khi đóng cửa sổ.", false);
        }
        base.OnFormClosing(e);
    }

    private static string ErrorDetails(Exception error, string operation, string? stage, string? filePath)
    {
        var details = new System.Text.StringBuilder();
        details.AppendLine($"Thao tác: {operation}");
        if (!string.IsNullOrEmpty(stage)) details.AppendLine($"Bước: {stage}");
        details.AppendLine($"Đường dẫn đang thao tác: {filePath ?? "(chưa chọn tệp)"}");
        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                string fullPath = Path.GetFullPath(filePath);
                string? parent = Path.GetDirectoryName(fullPath);
                details.AppendLine($"Đường dẫn đầy đủ: {fullPath}");
                details.AppendLine($"Tệp tồn tại: {File.Exists(fullPath)}; thư mục cha tồn tại: {Directory.Exists(parent)}");
                details.AppendLine($"Thư mục cha: {parent}");
            }
            catch (Exception pathError)
            {
                details.AppendLine($"Không kiểm tra được đường dẫn: {pathError.GetType().Name}: {pathError.Message}");
            }
        }
        for (Exception? current = error; current is not null; current = current.InnerException)
        {
            details.AppendLine($"Exception: {current.GetType().Name}");
            details.AppendLine($"Message: {current.Message}");
            details.AppendLine($"HResult: 0x{current.HResult:X8} ({current.HResult})");
            string? exceptionFile = current switch
            {
                FileNotFoundException missing => missing.FileName,
                FileLoadException load => load.FileName,
                BadImageFormatException image => image.FileName,
                _ => null
            };
            if (!string.IsNullOrEmpty(exceptionFile)) details.AppendLine($"Tệp trong exception: {exceptionFile}");
            if (current.InnerException is not null) details.AppendLine("Nguyên nhân bên trong:");
        }
        return details.ToString();
    }

    // Localize presentation only; the services and their exception contracts stay unchanged.
    private static string ErrorMessage(Exception error)
    {
        if (error.HResult == unchecked((int)0x800711C7))
            return "Windows App Control đã chặn việc nạp tệp hoặc thư viện. Đây là lỗi chính sách bảo mật Windows; xem đường dẫn tệp bị chặn trong chẩn đoán.";
        string message = error.Message;
        if (message.StartsWith("RSA-OAEP-SHA256 plaintext must contain", StringComparison.Ordinal))
        {
            var sizes = System.Text.RegularExpressions.Regex.Match(message,
                @"1 to (\d+) bytes for a (\d+)-bit key; received (\d+) bytes");
            return sizes.Success
                ? $"Với khóa RSA {sizes.Groups[2].Value} bit, văn bản gốc phải dài từ 1 đến {sizes.Groups[1].Value} byte UTF-8; dữ liệu hiện tại dài {sizes.Groups[3].Value} byte."
                : "Văn bản gốc nằm ngoài giới hạn kích thước của RSA-OAEP-SHA256.";
        }
        if (message.StartsWith("RSA ciphertext must contain", StringComparison.Ordinal))
            return "Dữ liệu mã hóa RSA có độ dài không hợp lệ. Hãy kiểm tra Base64 và kích thước khóa.";
        if (message.Contains("AES-256 key", StringComparison.Ordinal))
            return "Khóa AES-256 phải có đúng 32 byte. Hãy sinh hoặc nạp lại khóa hợp lệ.";
        if (message.Contains("incompatible header", StringComparison.Ordinal))
            return "Tệp CSV hiện có không đúng định dạng cột. Dữ liệu cũ được giữ nguyên; hãy đổi tên tệp trước khi chạy lại.";
        if (message.StartsWith("Not enough free disk space", StringComparison.Ordinal))
            return "Không đủ dung lượng trống để chạy đánh giá. Hãy giải phóng dung lượng rồi thử lại; kết quả cũ được giữ nguyên.";
        if (message.StartsWith("Existing benchmark file has the wrong size", StringComparison.Ordinal))
            return "Tệp thử nghiệm hiện có sai kích thước. Hãy đổi tên tệp đó trước khi chạy lại.";
        if (message.StartsWith("Input and output paths", StringComparison.Ordinal))
            return "Đường dẫn tệp đầu vào và tệp đầu ra phải khác nhau.";
        if (message.StartsWith("The output path already exists", StringComparison.Ordinal))
            return "Tệp đầu ra đã tồn tại. Hãy chọn tên tệp mới.";
        if (message.StartsWith("RSA encryption failed", StringComparison.Ordinal))
            return "Không thể mã hóa RSA. Cần khóa công khai hợp lệ hỗ trợ OAEP-SHA256.";
        if (message.StartsWith("RSA decryption failed", StringComparison.Ordinal))
            return "Không thể giải mã RSA. Hãy dùng khóa bí mật tương ứng và dữ liệu OAEP-SHA256 chưa bị sửa.";
        if (message.StartsWith("RSA-PSS signing failed", StringComparison.Ordinal))
            return "Không thể tạo chữ ký RSA-PSS. Cần khóa bí mật hỗ trợ SHA-256 và RSA-PSS.";
        if (message.StartsWith("Cannot import RSA", StringComparison.Ordinal))
            return "Không thể nạp khóa RSA từ PEM. Dữ liệu khóa không hợp lệ hoặc không được hỗ trợ.";

        return error switch
        {
            FileNotFoundException file => $"Không tìm thấy tệp. Hãy kiểm tra lại đường dẫn.\n{file.FileName}",
            DirectoryNotFoundException => "Không tìm thấy thư mục. Hãy chọn đường dẫn trong thư mục có sẵn của project.",
            UnauthorizedAccessException => "Không có quyền truy cập tệp hoặc thư mục. Hãy chọn vị trí có quyền đọc và ghi.",
            PathTooLongException => "Đường dẫn quá dài. Hãy chọn thư mục hoặc tên tệp ngắn hơn.",
            InvalidDataException => "Tệp không đúng định dạng, bị thiếu dữ liệu hoặc có phiên bản chưa được hỗ trợ.",
            FileLoadException => "Không thể nạp tệp hoặc thư viện ứng dụng. Xem tên tệp và lỗi gốc trong chẩn đoán.",
            IOException io when io.HResult == unchecked((int)0x80070020) => "Tệp đang được mở với chế độ chia sẻ không cho phép thao tác này. Hãy đóng tiến trình đang giữ tệp rồi thử lại.",
            IOException io when io.HResult == unchecked((int)0x80070021) => "Vùng dữ liệu của tệp đang bị khóa. Hãy đóng tiến trình đang giữ tệp rồi thử lại.",
            IOException => "Thao tác đọc/ghi tệp không thành công. Xem loại exception, thông báo gốc, HResult và đường dẫn bên dưới để xác định nguyên nhân.",
            System.Text.EncoderFallbackException or System.Text.DecoderFallbackException => "Văn bản chứa dữ liệu Unicode không hợp lệ.",
            FormatException => "Dữ liệu Base64 hoặc PEM không hợp lệ. Hãy kiểm tra nội dung và loại khóa cần nạp.",
            System.Security.Cryptography.CryptographicException => "Xác thực dữ liệu không thành công. Khóa có thể không đúng hoặc dữ liệu đã bị sửa.",
            ObjectDisposedException => "Chức năng đã được đóng. Hãy mở lại rồi thử lại thao tác.",
            InvalidOperationException => message,
            ArgumentException => "Dữ liệu đầu vào chưa hợp lệ. Hãy kiểm tra khóa, nội dung và đường dẫn tệp.",
            NotSupportedException => "Thao tác hoặc định dạng này chưa được hỗ trợ.",
            _ => $"Không thể hoàn tất thao tác ({error.GetType().Name}). Hãy kiểm tra dữ liệu và thử lại."
        };
    }
}
