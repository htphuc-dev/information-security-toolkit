using information_security_toolkit.Hashing;

namespace information_security_toolkit.WinForms.Forms;

public sealed class HashForm : ToolkitForm
{
    private readonly TextBox inputA = Ui.TextBox("Dữ liệu A");
    private readonly TextBox inputB = Ui.TextBox("Dữ liệu B");
    private readonly TextBox hashA = Ui.TextBox("Giá trị băm A", readOnly: true);
    private readonly TextBox hashB = Ui.TextBox("Giá trị băm B", readOnly: true);
    private readonly Label differentBits = Ui.Label("Số bit khác nhau: chưa tính");
    private readonly Label difference = Ui.Label("Tỷ lệ khác biệt: chưa tính");

    public HashForm() : base("SHA-256 — Hàm băm", "SHA-256 • 256 bit / 32 byte / 64 ký tự Hex • Quan sát tác động của thay đổi nhỏ trong dữ liệu.")
    {
        inputA.Text = "hello";
        inputB.Text = "Hello";
        Body.Controls.Add(Ui.Column(
            (Ui.Field("Dữ liệu A", inputA), 30), (Ui.Field("Dữ liệu B", inputB), 30),
            (Ui.Buttons(ActionButton("Tính SHA-256", Calculate)), 0),
            (Ui.Field("Giá trị băm A (Hex)", hashA), 20), (Ui.Field("Giá trị băm B (Hex)", hashB), 20),
            (differentBits, 0), (difference, 0)));
        inputA.TextChanged += (_, _) => InvalidateResults();
        inputB.TextChanged += (_, _) => InvalidateResults();
    }

    private void Calculate()
    {
        hashA.Text = Sha256Service.ComputeHash(inputA.Text);
        hashB.Text = Sha256Service.ComputeHash(inputB.Text);
        int bits = Sha256Service.CountDifferentBits(Convert.FromHexString(hashA.Text), Convert.FromHexString(hashB.Text));
        differentBits.Text = $"Số bit khác nhau: {bits} / {Sha256Service.HashSizeInBits}";
        difference.Text = $"Tỷ lệ khác biệt: {bits / (double)Sha256Service.HashSizeInBits * 100:F2}%";
        Status(inputA.Text == inputB.Text ? "Đã tính SHA-256 — dữ liệu giống nhau cho cùng giá trị băm và 0 bit khác nhau."
            : "Đã tính SHA-256 — số bit khác nhau được đo thực tế; tỷ lệ không nhất thiết bằng 50%.");
    }

    private void InvalidateResults()
    {
        hashA.Clear(); hashB.Clear();
        differentBits.Text = "Số bit khác nhau: chưa tính";
        difference.Text = "Tỷ lệ khác biệt: chưa tính";
        Status("Dữ liệu đã thay đổi — hãy tính lại để cập nhật giá trị băm.");
    }
}
