using System.Security.Cryptography;
using System.Text;
using information_security_toolkit.RSA;
using information_security_toolkit.Signature;
using Rsa = System.Security.Cryptography.RSA;

namespace information_security_toolkit.WinForms.Forms;

public sealed class SignatureForm : ToolkitForm
{
    private Rsa? privateKey;
    private Rsa? publicKey;
    private byte[]? signedData;
    private readonly TextBox data = Ui.TextBox("Dữ liệu ký số");
    private readonly TextBox firstSignature = Ui.TextBox("Chữ ký 1 dạng Base64", readOnly: true);
    private readonly TextBox secondSignature = Ui.TextBox("Chữ ký 2 dạng Base64", readOnly: true);
    private readonly Label verification = Ui.Label("Kết quả xác minh: chưa thực hiện");
    private readonly Label comparison = Ui.Label("Hai chữ ký khác nhau: hãy ký cùng dữ liệu hai lần để so sánh");
    private static readonly Encoding TextEncoding = new UTF8Encoding(false, true);

    public SignatureForm() : base("Chữ ký số — RSA-PSS", "RSA-2048 • SHA-256 • RSA-PSS • Bộ khóa riêng được giữ trong bộ nhớ đến khi đóng cửa sổ này.")
    {
        data.Text = "Information Security Toolkit";
        Body.Controls.Add(Ui.Column(
            (Ui.Buttons(ActionButton("Sinh cặp khóa RSA-2048", GenerateAsync)), 0),
            (Ui.Field("Dữ liệu (UTF-8)", data), 30),
            (Ui.Buttons(ActionButton("Ký số", Sign), ActionButton("Ký lại", SignAgain), ActionButton("Xác minh", () => Verify())), 0),
            (Ui.Field("Chữ ký #1 (Base64)", firstSignature), 35),
            (Ui.Field("Chữ ký #2 (Base64)", secondSignature), 35),
            (comparison, 0), (verification, 0)));
        data.TextChanged += (_, _) =>
        {
            verification.Text = "Kết quả xác minh: dữ liệu đã thay đổi — hãy xác minh lại chữ ký hiện có.";
            comparison.Text = "Hai chữ ký khác nhau: chỉ so sánh khi dữ liệu không đổi";
            Status("Dữ liệu đã thay đổi. Hãy xác minh chữ ký cũ hoặc chọn Ký số để tạo chữ ký mới.");
        };
    }

    private async Task GenerateAsync()
    {
        Rsa generated = await Task.Run(() => RsaKeyManager.GenerateKeyPair(2048));
        Rsa derivedPublic = Rsa.Create();
        try { derivedPublic.ImportParameters(PemAnalyzer.GetParameters(generated)); }
        catch { derivedPublic.Dispose(); generated.Dispose(); throw; }
        privateKey?.Dispose(); publicKey?.Dispose();
        privateKey = generated;
        publicKey = derivedPublic;
        ClearSignedData();
        firstSignature.Clear(); secondSignature.Clear();
        verification.Text = "Kết quả xác minh: chưa thực hiện với khóa này";
        comparison.Text = "Hai chữ ký khác nhau: hãy ký cùng dữ liệu hai lần để so sánh";
        Status("Đã sinh cặp khóa RSA-2048 thành công; khóa chỉ được giữ trong bộ nhớ.");
    }

    private void Sign()
    {
        Rsa key = privateKey ?? throw new InvalidOperationException("Hãy sinh cặp khóa RSA-2048 trước khi ký số.");
        byte[] input = TextEncoding.GetBytes(data.Text);
        byte[] signature = DigitalSignatureService.SignData(input, key);
        ClearSignedData();
        signedData = input;
        firstSignature.Text = Convert.ToBase64String(signature);
        secondSignature.Clear();
        verification.Text = "Kết quả xác minh: chưa thực hiện";
        comparison.Text = "Hai chữ ký khác nhau: giữ nguyên dữ liệu rồi chọn Ký lại";
        Status("Đã tạo chữ ký thành công bằng khóa bí mật — SHA-256 / RSA-PSS.");
    }

    private void SignAgain()
    {
        Rsa key = privateKey ?? throw new InvalidOperationException("Hãy sinh khóa và chọn Ký số trước.");
        byte[] input = TextEncoding.GetBytes(data.Text);
        if (signedData is null || !input.AsSpan().SequenceEqual(signedData))
            throw new InvalidOperationException("Hãy chọn Ký số trước, sau đó giữ nguyên dữ liệu và khóa để Ký lại.");
        byte[] signature = DigitalSignatureService.SignData(input, key);
        secondSignature.Text = Convert.ToBase64String(signature);
        bool different = !string.Equals(firstSignature.Text, secondSignature.Text, StringComparison.Ordinal);
        comparison.Text = $"Hai chữ ký khác nhau: {(different ? "CÓ" : "KHÔNG")} — cùng dữ liệu và khóa bí mật; RSA-PSS dùng salt ngẫu nhiên.";
        bool verified = Verify();
        Status($"Kiểm tra tính ngẫu nhiên của chữ ký RSA-PSS: {(different && verified ? "ĐẠT" : "KHÔNG ĐẠT")} — xem kết quả xác minh bên dưới.", different && verified);
    }

    private bool Verify()
    {
        Rsa key = publicKey ?? throw new InvalidOperationException("Hãy sinh khóa trước khi xác minh.");
        if (string.IsNullOrWhiteSpace(firstSignature.Text)) throw new InvalidOperationException("Hãy chọn Ký số trước khi xác minh.");
        byte[] input = TextEncoding.GetBytes(data.Text);
        bool firstValid = DigitalSignatureService.VerifyData(input, Convert.FromBase64String(firstSignature.Text), key);
        bool secondValid = string.IsNullOrEmpty(secondSignature.Text)
            || DigitalSignatureService.VerifyData(input, Convert.FromBase64String(secondSignature.Text), key);
        verification.Text = $"Kết quả xác minh chữ ký #1: {(firstValid ? "HỢP LỆ" : "KHÔNG HỢP LỆ")}";
        if (!string.IsNullOrEmpty(secondSignature.Text))
            verification.Text += $" • Chữ ký #2: {(secondValid ? "HỢP LỆ" : "KHÔNG HỢP LỆ")}";
        Status(firstValid && secondValid ? "Xác minh thành công — chữ ký hợp lệ với khóa công khai tương ứng."
            : "Xác minh không thành công — dữ liệu hoặc chữ ký không khớp.", firstValid && secondValid);
        return firstValid && secondValid;
    }

    private void ClearSignedData()
    {
        if (signedData is not null) CryptographicOperations.ZeroMemory(signedData);
        signedData = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { privateKey?.Dispose(); publicKey?.Dispose(); ClearSignedData(); }
        base.Dispose(disposing);
    }
}
