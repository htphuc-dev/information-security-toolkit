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

Trạng thái hiện tại: **Milestone 2.3 — AES File Encryption**. Đã có quản lý khóa AES-256, mã hóa/giải mã văn bản và file. Chưa benchmark file lớn, triển khai các module mật mã khác hoặc WinForms UI.

Demo: load khóa 32 byte đã có hoặc sinh và lưu khóa nếu chưa có file. File nằm tại `keys/aes-demo.key` trong thư mục output của ứng dụng (mặc định: `information-security-toolkit/bin/Debug/net10.0/keys/aes-demo.key`) và đã được `.gitignore` bỏ qua. File khóa lỗi được báo ra Console, không tự ghi đè.

Demo đọc tất cả file trực tiếp trong `test-data/` ở root repository, bỏ qua `README.md` và `.gitkeep`. Các file mẫu `sample.txt`, `sample-unicode.txt`, `sample-empty.bin`, `sample-binary.bin` được tạo nếu chưa có; file đã có được giữ nguyên. Bạn có thể thêm file riêng vào `test-data/` để kiểm thử. Demo mã hóa/giải mã và so sánh từng byte bằng stream, đồng thời kiểm tra sai khóa, sửa dữ liệu, input thiếu, output không hợp lệ và format sai.

Mỗi lần chạy tạo `results/run-<id>/`, chứa `<tên-file>.enc` và `<tên-file>.decrypted`; thư mục con `checks/` chứa dữ liệu kiểm tra lỗi. Chương trình tự tìm root repository nên chạy được từ root hoặc thư mục project. Dữ liệu trong `test-data/` và `results/` đã được Git bỏ qua, ngoại trừ các README.

`AesTextService.Encrypt` chuyển văn bản sang UTF-8, dùng nonce ngẫu nhiên 12 byte và authentication tag 16 byte. Định dạng trả về là `Base64(nonce | tag | ciphertext)`: byte 0–11 là nonce, byte 12–27 là tag, các byte từ 28 trở đi là ciphertext. Khóa không nằm trong output. Nonce mới được sinh cho mỗi lần mã hóa nên cùng plaintext và khóa vẫn cho output khác nhau.

`AesTextService.Decrypt` xác thực tag rồi trả lại văn bản UTF-8; hỗ trợ plaintext rỗng, từ chối đầu vào lỗi, sai khóa và dữ liệu bị sửa. Module văn bản giữ nguyên AES-GCM.

`AesFileService` dùng **AES-256-CBC/PKCS7 + HMAC-SHA256 (Encrypt-then-MAC)**, với `FileStream`, `CryptoStream` và buffer 64 KB. Hai khóa riêng cho AES và HMAC được dẫn xuất bằng HKDF-SHA256 với hai nhãn khác nhau từ khóa 32 byte và salt ngẫu nhiên; IV 16 byte cũng được sinh mới cho mỗi file. CBC phù hợp với streaming trong API .NET mà không cần giữ toàn bộ file để gọi `AesGcm` một lần.

File nhị phân v1: `ISTF(4) | version=1(1) | salt(16) | IV(16) | HMAC(32) | ciphertext(N×16)`. HMAC bao phủ toàn bộ header (magic, version, salt, IV) và ciphertext, không bao gồm trường HMAC. Mã hóa ghi ciphertext rồi đọc lại theo chunk để tính HMAC; giải mã xác minh toàn bộ MAC bằng `FixedTimeEquals` trước khi decrypt trong lượt đọc thứ hai. Một handle input được giữ mở với `FileShare.Read` để ngăn ghi/xóa file trong lúc xác minh và giải mã trên Windows.

Output luôn là file mới: ghi vào file `.tmp` cùng thư mục, chỉ chuyển sang tên đích sau khi thành công; lỗi sẽ dọn file tạm và không ghi đè file hiện có. Bộ nhớ dùng cho buffer không tăng theo kích thước file; đánh đổi là hai lượt I/O. Chưa thực hiện benchmark hoặc thử file 1 GB ở giai đoạn này. Lỗi được báo bằng exception chuẩn .NET và demo in `FAIL`, thoát với mã 1 khi kiểm tra không đạt.

File khóa là văn bản Base64, **không được mã hóa**. Chỉ dùng khóa demo cho bài tập và giữ bí mật file khóa.

Build và chạy từ thư mục root:

```powershell
dotnet build information-security-toolkit.slnx
dotnet run --project information-security-toolkit/information-security-toolkit.csproj
```
