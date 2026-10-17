# Information Security Toolkit

Project `information-security-toolkit` phục vụ bài tập môn An toàn và bảo mật thông tin.

Mục tiêu: thực hành sinh và quản lý khóa, mã hóa/giải mã văn bản và file, băm dữ liệu, ký/xác minh chữ ký và đo hiệu năng.

Công nghệ: C#, .NET 10 LTS, Console App; phát triển trên Windows 11 với VS Code. Solution dùng định dạng `.slnx`, có thể mở bằng Visual Studio hỗ trợ .NET 10.

Các module dự kiến:

- **AES**: sinh, lưu/import khóa; mã hóa/giải mã văn bản và file.
- **RSA**: sinh cặp khóa, export/import PEM, đọc tham số và mã hóa/giải mã dữ liệu nhỏ.
- **SHA-256** (`Hashing`): băm dữ liệu và quan sát avalanche effect.
- **Digital Signature** (`Signature`): ký/xác minh RSA; bổ sung RSA-PSS ở giai đoạn sau.
- **Benchmark**: đo hiệu năng và so sánh file nguồn với file sau giải mã.

`Common` dành cho tiện ích dùng chung; `docs/screenshots` lưu ảnh minh họa; `test-data` lưu dữ liệu thử; `results` lưu kết quả.

Trạng thái hiện tại: **Initial setup**. Chưa triển khai logic mật mã hoặc WinForms UI.

Build và chạy từ thư mục root:

```powershell
dotnet build information-security-toolkit.slnx
dotnet run --project information-security-toolkit/information-security-toolkit.csproj
```
