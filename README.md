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

Trạng thái hiện tại: **Milestone 2 — AES Key Management**. Đã có sinh, lưu/import và kiểm tra khóa AES-256. Chưa triển khai mã hóa/giải mã, các module mật mã khác hoặc WinForms UI.

Demo: sinh khóa ngẫu nhiên 32 byte, in Base64, lưu file, đọc lại và so sánh; in `PASS` khi khóa trùng khớp. File nằm tại `keys/aes-demo.key` trong thư mục output của ứng dụng (mặc định: `information-security-toolkit/bin/Debug/net10.0/keys/aes-demo.key`) và đã được `.gitignore` bỏ qua. Mỗi lần chạy demo sẽ sinh khóa mới và ghi đè file demo này.

File khóa là văn bản Base64, **không được mã hóa**. Chỉ dùng khóa demo cho bài tập; giữ bí mật file khóa và nội dung khóa in ra Console.

Build và chạy từ thư mục root:

```powershell
dotnet build information-security-toolkit.slnx
dotnet run --project information-security-toolkit/information-security-toolkit.csproj
```
