using System.Security.Cryptography;
using information_security_toolkit.AES;
using information_security_toolkit.Common;

namespace information_security_toolkit.WinForms.Forms;

public sealed class AesForm : ToolkitForm
{
    private byte[]? currentKey;
    private readonly TextBox keyText = Ui.TextBox("Khóa AES dạng Base64", readOnly: true);
    private readonly Label keyLength = Ui.Label("Độ dài khóa: chưa nạp khóa");
    private readonly TextBox keyFilePath = Ui.TextBox("Đường dẫn tệp khóa đang thao tác", readOnly: true, multiline: false);
    private readonly TextBox plaintext = Ui.TextBox("Văn bản gốc AES");
    private readonly TextBox ciphertext = Ui.TextBox("Dữ liệu mã hóa AES dạng Base64", readOnly: true);
    private readonly TextBox decryptedText = Ui.TextBox("Văn bản sau giải mã AES", readOnly: true);
    private readonly TextBox inputPath = Ui.TextBox("Tệp đầu vào", multiline: false);
    private readonly TextBox encryptedPath = Ui.TextBox("Tệp đã mã hóa", multiline: false);
    private readonly TextBox decryptedPath = Ui.TextBox("Tệp sau giải mã", multiline: false);
    private readonly Label fileResult = Ui.Label("So sánh tệp: chưa thực hiện");

    public AesForm() : base("AES — Mã hóa đối xứng", "AES-256 • Văn bản: GCM • Tệp: CBC + HMAC • Hãy sinh hoặc nạp khóa trước khi mã hóa.")
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(Ui.Tab("Quản lý khóa", Ui.Column(
            (Ui.Buttons(ActionButton("Sinh khóa", GenerateKey), ActionButton("Lưu khóa", SaveKey), ActionButton("Nạp khóa", LoadKey)), 0),
            (Ui.Field("Khóa (Base64)", keyText), 100), (keyLength, 0),
            (Ui.Field("Đường dẫn tệp khóa đang thao tác", keyFilePath), 0),
            (Ui.Label("Khóa Base64 là thông tin bí mật và chưa được mã hóa. Chỉ lưu khi cần dùng cho bài thực hành."), 0))));
        plaintext.Text = "Xin chào! Information Security Toolkit 🔐";
        tabs.TabPages.Add(Ui.Tab("Mã hóa văn bản", Ui.Column(
            (Ui.Field("Văn bản gốc", plaintext), 33),
            (Ui.Buttons(ActionButton("Mã hóa", EncryptText)), 0),
            (Ui.Field("Dữ liệu mã hóa (Base64)", ciphertext), 34),
            (Ui.Buttons(ActionButton("Giải mã", DecryptText)), 0),
            (Ui.Field("Văn bản sau giải mã", decryptedText), 33))));
        tabs.TabPages.Add(Ui.Tab("Mã hóa tệp", Ui.Column(
            (Ui.Field("Tệp gốc", Ui.PathRow(inputPath, ActionButton("Chọn tệp", BrowseInput))), 0),
            (Ui.Field("Tệp đã mã hóa (đầu ra khi mã hóa, đầu vào khi giải mã)", Ui.PathRow(encryptedPath,
                ActionButton("Chọn tệp mã hóa", () => ChooseOutput(encryptedPath, "Tệp đã mã hóa|*.enc|Tất cả tệp|*.*", "encrypted.enc")))), 0),
            (Ui.Field("Tệp đầu ra sau giải mã", Ui.PathRow(decryptedPath,
                ActionButton("Chọn tệp giải mã", () => ChooseOutput(decryptedPath, "Tất cả tệp|*.*", "decrypted.txt")))), 0),
            (Ui.Buttons(ActionButton("Mã hóa tệp", EncryptFileAsync), ActionButton("Giải mã tệp", DecryptFileAsync)), 0),
            (fileResult, 0),
            (Ui.Label("Hãy chọn tên tệp đầu ra mới; tệp đã có được giữ nguyên. Tệp được xử lý và so sánh theo luồng."), 100))));
        Body.Controls.Add(tabs);
        plaintext.TextChanged += (_, _) => { decryptedText.Clear(); Status("Văn bản gốc đã thay đổi — hãy mã hóa để cập nhật dữ liệu."); };
        foreach (TextBox path in new[] { inputPath, encryptedPath, decryptedPath })
            path.TextChanged += (_, _) => fileResult.Text = "So sánh tệp: chưa thực hiện với các đường dẫn này";
    }

    private byte[] RequireKey() => currentKey ?? throw new InvalidOperationException("Hãy sinh hoặc nạp khóa AES-256 trong tab Quản lý khóa trước.");

    private void ReplaceKey(byte[] key)
    {
        if (currentKey is not null) CryptographicOperations.ZeroMemory(currentKey);
        currentKey = key;
        keyText.Text = Convert.ToBase64String(key);
        keyLength.Text = $"Độ dài khóa: {key.Length} byte / {key.Length * 8} bit";
        keyFilePath.Clear();
        ciphertext.Clear();
        decryptedText.Clear();
        fileResult.Text = "So sánh tệp: chưa thực hiện với khóa này";
    }

    private void GenerateKey()
    {
        OperationStage = "Sinh khóa trong bộ nhớ (không đọc/ghi tệp)";
        ReplaceKey(AesKeyManager.GenerateKey());
        Status("Đã sinh khóa AES-256 thành công — 32 byte / 256 bit.");
    }

    private void SaveKey()
    {
        byte[] key = RequireKey();
        string directory = PrepareKeyDirectory();
        using var dialog = new SaveFileDialog
        {
            Title = "Lưu khóa AES", Filter = "Khóa AES|*.key|Tất cả tệp|*.*", FileName = "aes-demo.key",
            InitialDirectory = directory, DefaultExt = "key", AddExtension = true,
            OverwritePrompt = true, CheckPathExists = true, RestoreDirectory = true
        };
        OperationStage = "Mở hộp thoại Lưu khóa AES";
        DialogResult result = dialog.ShowDialog(this);
        System.Diagnostics.Debug.WriteLine($"[AES SaveFileDialog] Result={result}; FileName={dialog.FileName}");
        if (result != DialogResult.OK) { Status("Đã hủy lưu khóa; không ghi tệp."); return; }
        string path = SelectKeyFilePath("Ghi tệp khóa AES", dialog.FileName);
        // The dialog confirms replacement; SaveKey uses WriteAllText, which overwrites an approved existing file.
        AesKeyManager.SaveKey(key, path);
        Status($"Đã lưu khóa thành công: {path}");
    }

    private void LoadKey()
    {
        string directory = PrepareKeyDirectory();
        using var dialog = new OpenFileDialog
        {
            Title = "Nạp khóa AES", Filter = "Khóa AES|*.key|Tất cả tệp|*.*",
            InitialDirectory = directory, CheckFileExists = true, CheckPathExists = true, RestoreDirectory = true
        };
        OperationStage = "Mở hộp thoại Nạp khóa AES";
        DialogResult result = dialog.ShowDialog(this);
        System.Diagnostics.Debug.WriteLine($"[AES OpenFileDialog] Result={result}; FileName={dialog.FileName}");
        if (result != DialogResult.OK) { Status("Đã hủy nạp khóa."); return; }
        string path = SelectKeyFilePath("Đọc tệp khóa AES", dialog.FileName);
        ReplaceKey(AesKeyManager.LoadKey(path));
        keyFilePath.Text = path;
        Status($"Đã nạp khóa AES-256 hợp lệ: {path}");
    }

    private string PrepareKeyDirectory()
    {
        OperationStage = "Chuẩn bị thư mục khóa do ứng dụng quản lý";
        string directory = Path.Combine(AppPaths.RepositoryRoot, "keys", "aes");
        OperationFilePath = directory;
        Directory.CreateDirectory(directory);
        System.Diagnostics.Debug.WriteLine($"[AES key directory] {directory}; Exists={Directory.Exists(directory)}");
        OperationFilePath = null;
        return directory;
    }

    private string SelectKeyFilePath(string stage, string selectedPath)
    {
        OperationStage = stage;
        OperationFilePath = selectedPath;
        keyFilePath.Text = selectedPath;
        string path = Path.GetFullPath(selectedPath);
        OperationFilePath = path;
        keyFilePath.Text = path;
        System.Diagnostics.Debug.WriteLine($"[AES {stage}] Path={path}; FileExists={File.Exists(path)}; "
            + $"Parent={Path.GetDirectoryName(path)}; ParentExists={Directory.Exists(Path.GetDirectoryName(path))}");
        return path;
    }

    private void EncryptText()
    {
        ciphertext.Text = AesTextService.Encrypt(plaintext.Text, RequireKey());
        decryptedText.Clear();
        Status("Mã hóa văn bản AES thành công.");
    }

    private void DecryptText()
    {
        decryptedText.Clear();
        decryptedText.Text = AesTextService.Decrypt(ciphertext.Text, RequireKey());
        bool matches = string.Equals(plaintext.Text, decryptedText.Text, StringComparison.Ordinal);
        Status($"Giải mã văn bản AES thành công — so sánh với văn bản gốc: {(matches ? "KHỚP" : "KHÔNG KHỚP")}", matches);
    }

    private void BrowseInput()
    {
        using var dialog = new OpenFileDialog { Title = "Chọn tệp gốc", Filter = "Tất cả tệp|*.*", InitialDirectory = Path.Combine(AppPaths.RepositoryRoot, "test-data") };
        if (dialog.ShowDialog(this) != DialogResult.OK) { Status("Đã hủy chọn tệp."); return; }
        inputPath.Text = dialog.FileName;
        fileResult.Text = "So sánh tệp: chưa thực hiện";
        Status("Đã chọn tệp gốc. Hãy chọn đường dẫn tệp mã hóa và tệp sau giải mã.");
    }

    private void ChooseOutput(TextBox field, string filter, string filename)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Chọn đường dẫn tệp", Filter = filter, FileName = filename, OverwritePrompt = false,
            InitialDirectory = Path.Combine(AppPaths.RepositoryRoot, "results")
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) { Status("Đã hủy chọn đường dẫn."); return; }
        field.Text = dialog.FileName;
        fileResult.Text = "So sánh tệp: chưa thực hiện";
        Status("Đã chọn đường dẫn. Các tệp đầu ra đã tồn tại sẽ được giữ nguyên.");
    }

    private async Task EncryptFileAsync()
    {
        byte[] key = RequireKey();
        string input = inputPath.Text;
        string output = encryptedPath.Text;
        fileResult.Text = "So sánh tệp: chưa thực hiện";
        await Task.Run(() => AesFileService.EncryptFile(input, output, key));
        Status("Mã hóa tệp AES thành công.");
    }

    private async Task DecryptFileAsync()
    {
        byte[] key = RequireKey();
        string input = encryptedPath.Text;
        string output = decryptedPath.Text;
        string original = inputPath.Text;
        fileResult.Text = "So sánh tệp: chưa thực hiện";
        await Task.Run(() => AesFileService.DecryptFile(input, output, key));
        if (string.IsNullOrWhiteSpace(original))
        {
            fileResult.Text = "So sánh tệp: hãy chọn tệp gốc để đối chiếu.";
            Status("Giải mã tệp AES thành công — tính toàn vẹn của tệp đã được xác thực.");
            return;
        }
        bool matches = await Task.Run(() => FileComparison.AreEqual(original, output));
        fileResult.Text = $"So sánh tệp: {(matches ? "KHỚP" : "KHÔNG KHỚP")}";
        Status($"Giải mã tệp AES thành công — so sánh tệp: {(matches ? "KHỚP" : "KHÔNG KHỚP")}", matches);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && currentKey is not null) CryptographicOperations.ZeroMemory(currentKey);
        base.Dispose(disposing);
    }
}
