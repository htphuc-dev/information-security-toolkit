using System.Security.Cryptography;
using System.Text;
using information_security_toolkit.RSA;
using Rsa = System.Security.Cryptography.RSA;

namespace information_security_toolkit.WinForms.Forms;

public sealed class RsaForm : ToolkitForm
{
    private Rsa? privateKey;
    private Rsa? publicKey;
    private readonly TextBox publicPem = Ui.TextBox("Khóa công khai PEM", readOnly: true);
    private readonly TextBox privatePem = Ui.TextBox("Khóa bí mật PEM", readOnly: true);
    private readonly TextBox parameters = Ui.TextBox("Tham số RSA", readOnly: true);
    private readonly TextBox input = Ui.TextBox("Văn bản gốc RSA");
    private readonly TextBox ciphertext = Ui.TextBox("Dữ liệu mã hóa RSA dạng Base64");
    private readonly TextBox decrypted = Ui.TextBox("Văn bản sau giải mã RSA", readOnly: true);
    private readonly Label keyInfo = Ui.Label("Chưa nạp khóa • Kiểu đệm: OAEP-SHA256");
    private readonly Label inputSize = Ui.Label("Dữ liệu đầu vào: 0 byte UTF-8");
    private static readonly Encoding TextEncoding = new UTF8Encoding(false, true);

    public RsaForm() : base("RSA — Mã hóa bất đối xứng", "Sinh hoặc nạp khóa PEM • OAEP-SHA256 • Dùng cho văn bản UTF-8 ngắn, không dùng để mã hóa tệp.")
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var pemColumns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        pemColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        pemColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        pemColumns.Controls.Add(Ui.Field("Khóa công khai (PEM)", publicPem), 0, 0);
        var privateField = Ui.Field("Khóa bí mật (PEM — cần giữ kín)", privatePem);
        privateField.Margin = new Padding(10, 0, 0, 0);
        pemColumns.Controls.Add(privateField, 1, 0);
        tabs.TabPages.Add(Ui.Tab("Quản lý khóa RSA", Ui.Column(
            (Ui.Buttons(ActionButton("Sinh cặp khóa RSA", GenerateAsync), ActionButton("Nạp khóa công khai", ImportPublic),
                ActionButton("Nạp khóa bí mật", ImportPrivate)), 0),
            (Ui.Buttons(ActionButton("Xuất khóa công khai", () => Export(false)),
                ActionButton("Xuất khóa bí mật", () => Export(true))), 0),
            (pemColumns, 50), (Ui.Field("Tham số (Base64)", parameters), 50))));
        tabs.TabPages.Add(Ui.Tab("Mã hóa / Giải mã", Ui.Column(
            (keyInfo, 0), (inputSize, 0), (Ui.Field("Văn bản gốc (UTF-8)", input), 33),
            (Ui.Buttons(ActionButton("Mã hóa bằng khóa công khai", Encrypt)), 0),
            (Ui.Field("Dữ liệu mã hóa (Base64; có thể dán dữ liệu cần giải mã vào đây)", ciphertext), 34),
            (Ui.Buttons(ActionButton("Giải mã bằng khóa bí mật", Decrypt)), 0),
            (Ui.Field("Văn bản sau giải mã", decrypted), 33))));
        Body.Controls.Add(tabs);
        input.TextChanged += (_, _) =>
        {
            inputSize.Text = $"Dữ liệu đầu vào: {Encoding.UTF8.GetByteCount(input.Text)} byte UTF-8";
            decrypted.Clear();
        };
        input.Text = "Hello RSA";
        ciphertext.TextChanged += (_, _) => decrypted.Clear();
    }

    private Rsa RequirePublic() => publicKey ?? throw new InvalidOperationException("Hãy sinh hoặc nạp khóa công khai trước.");
    private Rsa RequirePrivate() => privateKey ?? throw new InvalidOperationException("Hãy sinh hoặc nạp khóa bí mật trước.");

    private void UsePrivateKey(Rsa key)
    {
        Rsa derivedPublic = Rsa.Create();
        try { derivedPublic.ImportParameters(PemAnalyzer.GetParameters(key)); }
        catch { derivedPublic.Dispose(); key.Dispose(); throw; }
        privateKey?.Dispose();
        publicKey?.Dispose();
        privateKey = key;
        publicKey = derivedPublic;
        RefreshKeyDisplay();
    }

    private async Task GenerateAsync()
    {
        UsePrivateKey(await Task.Run(() => RsaKeyManager.GenerateKeyPair(2048)));
        Status("Đã sinh cặp khóa RSA-2048 thành công; khóa được giữ trong bộ nhớ.");
    }

    private void ImportPublic()
    {
        using var dialog = new OpenFileDialog { Title = "Nạp khóa công khai RSA", Filter = "Khóa công khai PEM|*.pem|Tất cả tệp|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK) { Status("Đã hủy nạp khóa."); return; }
        Rsa imported = RsaKeyManager.LoadPublicKey(dialog.FileName);
        privateKey?.Dispose();
        publicKey?.Dispose();
        privateKey = null; // Never keep an unrelated private key after a public-only import.
        publicKey = imported;
        RefreshKeyDisplay();
        Status("Đã nạp khóa công khai PEM. Hãy nạp khóa bí mật tương ứng để giải mã.");
    }

    private void ImportPrivate()
    {
        using var dialog = new OpenFileDialog { Title = "Nạp khóa bí mật RSA", Filter = "Khóa bí mật PEM|*.pem|Tất cả tệp|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK) { Status("Đã hủy nạp khóa."); return; }
        UsePrivateKey(RsaKeyManager.LoadPrivateKey(dialog.FileName));
        Status("Đã nạp khóa bí mật PEM cùng các tham số công khai tương ứng.");
    }

    private void Export(bool includePrivate)
    {
        Rsa key = includePrivate ? RequirePrivate() : RequirePublic();
        using var dialog = new SaveFileDialog
        {
            Title = includePrivate ? "Xuất khóa bí mật RSA" : "Xuất khóa công khai RSA",
            Filter = "Khóa PEM|*.pem|Tất cả tệp|*.*", OverwritePrompt = false,
            FileName = includePrivate ? "rsa-private.pem" : "rsa-public.pem"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) { Status("Đã hủy xuất khóa."); return; }
        if (includePrivate) RsaKeyManager.ExportPrivateKey(key, dialog.FileName);
        else RsaKeyManager.ExportPublicKey(key, dialog.FileName);
        Status($"Đã xuất khóa {(includePrivate ? "bí mật" : "công khai")} PEM thành công.");
    }

    private void RefreshKeyDisplay()
    {
        Rsa key = RequirePublic();
        // Serialization and Base64 formatting are presentation; cryptographic operations stay in the services.
        publicPem.Text = key.ExportSubjectPublicKeyInfoPem();
        privatePem.Text = privateKey?.ExportPkcs8PrivateKeyPem() ?? "Chưa nạp khóa bí mật.";
        RSAParameters values = PemAnalyzer.GetParameters(privateKey ?? key, privateKey is not null);
        try
        {
            var text = new StringBuilder();
            Add("n", values.Modulus); Add("e", values.Exponent); Add("d", values.D);
            Add("p", values.P); Add("q", values.Q); Add("dp", values.DP);
            Add("dq", values.DQ); Add("inverseQ", values.InverseQ);
            parameters.Text = text.ToString();
            void Add(string name, byte[]? value)
            {
                if (value is not null) text.AppendLine($"{name} ({value.Length} byte): {Convert.ToBase64String(value)}");
            }
        }
        finally
        {
            foreach (byte[]? value in new[] { values.D, values.P, values.Q, values.DP, values.DQ, values.InverseQ })
                if (value is not null) CryptographicOperations.ZeroMemory(value);
        }
        keyInfo.Text = $"Độ dài khóa: {key.KeySize} bit • OAEP-SHA256 • Văn bản gốc tối đa: {RsaCryptoService.GetMaxPlaintextSize(key)} byte";
        ciphertext.Clear();
        decrypted.Clear();
    }

    private void Encrypt()
    {
        byte[] encrypted = RsaCryptoService.Encrypt(TextEncoding.GetBytes(input.Text), RequirePublic());
        ciphertext.Text = Convert.ToBase64String(encrypted);
        Status("Mã hóa RSA thành công.");
    }

    private void Decrypt()
    {
        decrypted.Clear();
        byte[] recovered = RsaCryptoService.Decrypt(Convert.FromBase64String(ciphertext.Text), RequirePrivate());
        decrypted.Text = TextEncoding.GetString(recovered);
        Status("Giải mã RSA thành công.");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { privateKey?.Dispose(); publicKey?.Dispose(); }
        base.Dispose(disposing);
    }
}
