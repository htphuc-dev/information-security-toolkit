# Release checklist — Information Security Toolkit

Ngày rà soát: **23/10/2026 (UTC+07:00)**. Phiên bản dự kiến: **v1.0.0**. Các dấu kiểm phản ánh việc đã chạy/đối chiếu trong lần rà soát này; 
## Checklist chính

- [x] Release build PASS — 0 warning, 0 error.
- [x] Console smoke test PASS.
- [x] WinForms launch PASS — MainForm có cửa sổ, phản hồi bình thường và đóng đúng cách.
- [x] AES demo PASS — file text, Unicode, rỗng, nhị phân; so sánh nguồn/giải mã và kiểm tra đầu vào lỗi.
- [x] RSA demo PASS — RSA-2048/OAEP-SHA256, PEM/Base64 round-trip, giới hạn kích thước và các trường hợp từ chối.
- [x] SHA-256 demo PASS — deterministic, Unicode/rỗng, 4 cặp avalanche và cặp đối chứng.
- [x] RSA-PSS demo PASS — dữ liệu gốc, sai dữ liệu/khóa/chữ ký và hai chữ ký ngẫu nhiên đều được kiểm tra.
- [x] Benchmark results exist — 5 mức AES, 3 mức RSA; CSV khớp bản ghi thực nghiệm giai đoạn 10.
- [x] README complete — tính năng, kiến trúc, lệnh chạy, benchmark thật, screenshot, Git và publish.
- [x] No secret tracked — không thấy `.key`/PEM private hoặc private-key blob nhúng trong tracked files và các file đang chờ commit.
- [x] No large benchmark file tracked — không thấy tệp benchmark lớn trong tracked files/candidate files.
- [x] Git status reviewed — phân loại bên dưới; không stage hoặc commit.


## Build và smoke đã thực hiện

```powershell
dotnet clean information-security-toolkit.slnx -m:1
dotnet restore information-security-toolkit.slnx -m:1
dotnet build information-security-toolkit.slnx -c Release --no-restore -m:1 -p:UseSharedCompilation=false
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- rsa crypto
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- hash sha256
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- signature demo
dotnet run -c Release --no-build --project information-security-toolkit.WinForms/information-security-toolkit.WinForms.csproj
```

Restore đã chạy thành công với `-m:1`; lần thử restore mặc định trong môi trường kiểm tra trả exit 1 không có chẩn đoán. Build cuối 0 warning/0 error. Không chạy lại benchmark 1 GB và không đổi các CSV để cải thiện số đo.

Smoke WinForms đã mở cửa sổ thật qua các nút MainForm rồi đóng bình thường:

- [x] MainForm.
- [x] AES Form.
- [x] RSA Form.
- [x] SHA-256 Form.
- [x] Signature Form.
- [x] Benchmark Form — không bắt đầu benchmark khi mở.

