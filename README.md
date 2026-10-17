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

Trạng thái hiện tại: **Milestone 2.2 — AES Text Encryption**. Đã có quản lý khóa AES-256 và mã hóa/giải mã văn bản bằng AES-GCM. Chưa triển khai mã hóa file, các module mật mã khác hoặc WinForms UI.

Demo: load khóa 32 byte đã có hoặc sinh và lưu khóa nếu chưa có file. File nằm tại `keys/aes-demo.key` trong thư mục output của ứng dụng (mặc định: `information-security-toolkit/bin/Debug/net10.0/keys/aes-demo.key`) và đã được `.gitignore` bỏ qua. File khóa lỗi được báo ra Console, không tự ghi đè.

Nhập văn bản vào Console, hoặc nhấn Enter để dùng mẫu tiếng Việt. Demo mã hóa hai lần bằng cùng khóa, in hai chuỗi Base64, giải mã và in `PASS/FAIL` cho phép so sánh plaintext và kiểm tra hai output khác nhau.

`AesTextService.Encrypt` chuyển văn bản sang UTF-8, dùng nonce ngẫu nhiên 12 byte và authentication tag 16 byte. Định dạng trả về là `Base64(nonce | tag | ciphertext)`: byte 0–11 là nonce, byte 12–27 là tag, các byte từ 28 trở đi là ciphertext. Khóa không nằm trong output. Nonce mới được sinh cho mỗi lần mã hóa nên cùng plaintext và khóa vẫn cho output khác nhau.

`Decrypt` decode Base64, tách ba phần, xác thực tag rồi trả lại văn bản UTF-8. Sai khóa hoặc dữ liệu bị sửa sẽ bị từ chối. API hỗ trợ chuỗi plaintext rỗng; từ chối `null`, khóa khác 32 byte, Base64 lỗi, payload thiếu nonce/tag và văn bản Unicode không hợp lệ. Các lỗi được báo bằng exception chuẩn .NET và demo bắt lỗi để in thông báo, thoát với mã 1 thay vì stack trace không kiểm soát.

File khóa là văn bản Base64, **không được mã hóa**. Chỉ dùng khóa demo cho bài tập và giữ bí mật file khóa.

Build và chạy từ thư mục root:

```powershell
dotnet build information-security-toolkit.slnx
dotnet run --project information-security-toolkit/information-security-toolkit.csproj
```
