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

Trạng thái hiện tại: **Milestone 4 — RSA Encryption/Decryption**. Đã hoàn thành các phần AES, quản lý khóa RSA/PEM và RSA encrypt/decrypt dữ liệu nhỏ. RSA benchmark, module SHA-256 riêng, chữ ký số và WinForms UI chưa triển khai.

Demo: load khóa 32 byte đã có hoặc sinh và lưu khóa nếu chưa có file. File nằm tại `keys/aes-demo.key` trong thư mục output của ứng dụng (mặc định: `information-security-toolkit/bin/Debug/net10.0/keys/aes-demo.key`) và đã được `.gitignore` bỏ qua. File khóa lỗi được báo ra Console, không tự ghi đè.

Demo đọc tất cả file trực tiếp trong `test-data/` ở root repository, bỏ qua `README.md` và `.gitkeep`. Các file mẫu `sample.txt`, `sample-unicode.txt`, `sample-empty.bin`, `sample-binary.bin` được tạo nếu chưa có; file đã có được giữ nguyên. Bạn có thể thêm file riêng vào `test-data/` để kiểm thử. Demo mã hóa/giải mã và so sánh từng byte bằng stream, đồng thời kiểm tra sai khóa, sửa dữ liệu, input thiếu, output không hợp lệ và format sai.

Mỗi lần chạy tạo `results/run-<id>/`, chứa `<tên-file>.enc` và `<tên-file>.decrypted`; thư mục con `checks/` chứa dữ liệu kiểm tra lỗi. Chương trình tự tìm root repository nên chạy được từ root hoặc thư mục project. Dữ liệu trong `test-data/` và `results/` đã được Git bỏ qua, ngoại trừ các README.

`AesTextService.Encrypt` chuyển văn bản sang UTF-8, dùng nonce ngẫu nhiên 12 byte và authentication tag 16 byte. Định dạng trả về là `Base64(nonce | tag | ciphertext)`: byte 0–11 là nonce, byte 12–27 là tag, các byte từ 28 trở đi là ciphertext. Khóa không nằm trong output. Nonce mới được sinh cho mỗi lần mã hóa nên cùng plaintext và khóa vẫn cho output khác nhau.

`AesTextService.Decrypt` xác thực tag rồi trả lại văn bản UTF-8; hỗ trợ plaintext rỗng, từ chối đầu vào lỗi, sai khóa và dữ liệu bị sửa. Module văn bản giữ nguyên AES-GCM.

`AesFileService` dùng **AES-256-CBC/PKCS7 + HMAC-SHA256 (Encrypt-then-MAC)**, với `FileStream`, `CryptoStream` và buffer 64 KB. Hai khóa riêng cho AES và HMAC được dẫn xuất bằng HKDF-SHA256 với hai nhãn khác nhau từ khóa 32 byte và salt ngẫu nhiên; IV 16 byte cũng được sinh mới cho mỗi file. CBC phù hợp với streaming trong API .NET mà không cần giữ toàn bộ file để gọi `AesGcm` một lần.

File nhị phân v1: `ISTF(4) | version=1(1) | salt(16) | IV(16) | HMAC(32) | ciphertext(N×16)`. HMAC bao phủ toàn bộ header (magic, version, salt, IV) và ciphertext, không bao gồm trường HMAC. Mã hóa ghi ciphertext rồi đọc lại theo chunk để tính HMAC; giải mã xác minh toàn bộ MAC bằng `FixedTimeEquals` trước khi decrypt trong lượt đọc thứ hai. Một handle input được giữ mở với `FileShare.Read` để ngăn ghi/xóa file trong lúc xác minh và giải mã trên Windows.

Output luôn là file mới: ghi vào file `.tmp` cùng thư mục, chỉ chuyển sang tên đích sau khi thành công; lỗi sẽ dọn file tạm và không ghi đè file hiện có. Bộ nhớ dùng cho buffer không tăng theo kích thước file; đánh đổi là hai lượt I/O. Lỗi được báo bằng exception chuẩn .NET và chương trình in `FAIL`, thoát với mã 1 khi không hoàn thành.

Benchmark chỉ chạy khi truyền `benchmark quick` (1/10 MB) hoặc `benchmark full` (100/500 MB, 1 GB). Quy ước: 1 MB = 1.000.000 byte, 1 GB = 1.000 MB. Không có tham số vẫn chạy demo file nhỏ. Dữ liệu được sinh theo chunk 1 MiB (lặp lại một block ngẫu nhiên), lưu trong `test-data/benchmark/`; file đúng kích thước được tái sử dụng, file sai kích thước được báo lỗi và giữ nguyên.

Sau warm-up 64 KiB, mỗi file được đo một lần bằng hai `Stopwatch` riêng cho encrypt/decrypt; không tính tạo file, SHA-256 hoặc ghi CSV vào thời gian đo. Throughput MB/s = kích thước nguồn MB / thời gian giây. Đo toàn bộ thao tác AES-CBC + HMAC và I/O; cache của hệ điều hành có thể ảnh hưởng số liệu. Kích thước và SHA-256 streaming của nguồn/giải mã phải trùng nhau; SHA-256 ở đây chỉ phục vụ xác minh benchmark, chưa phải module bài tập riêng.

Output nằm trong `results/benchmark/run-<RunId>/`; CSV `results/benchmark/aes-benchmark.csv` append thời gian UTC, tên/kích thước file, thời gian ms, throughput, hai SHA-256, integrity/PASS/FAIL, mode và RunId. CSV dùng dấu chấm thập phân để đọc nhất quán. Giữ nguyên kết quả cũ; benchmark kiểm tra dung lượng trống trước khi chạy. Full lần đầu có thể dùng hơn 4,8 GB cho nguồn + mã hóa + giải mã; chỉ chạy khi chọn rõ `full`. Dữ liệu/CSV benchmark đều được Git ignore. Full mode để chạy thủ công sau.

File khóa là văn bản Base64, **không được mã hóa**. Chỉ dùng khóa demo cho bài tập và giữ bí mật file khóa.

`RsaKeyManager` sinh cặp khóa RSA mặc định 2048 bit, hỗ trợ 3072/4096 bit. Public key export/import theo SPKI (`PUBLIC KEY`), private key theo PKCS#8 không mã hóa (`PRIVATE KEY`). Loader yêu cầu đúng loại, đúng một PEM block, không chấp nhận dữ liệu thừa hay nội dung ASN.1 không hợp lệ. Khóa import cũng phải có một trong các kích thước được hỗ trợ; export không ghi đè file đã có. Các đối tượng `RSA` trả về cần được giải phóng bằng `using`.

`PemAnalyzer` lấy `RSAParameters` và in Base64/số byte: public `(n, e)`, private `(n, e, d, p, q, dp, dq, inverseQ)`; exponent còn được hiển thị dạng số. Lệnh `rsa keys` sinh/export/import lại khóa, so sánh modulus/exponent và kiểm tra các đầu vào lỗi. PEM local nằm trong `keys/rsa/run-<id>/`, đã được Git ignore. Demo in cả tham số private ra Console và lưu private PEM không mã hóa; chỉ dùng khóa thử nghiệm và giữ bí mật các giá trị này.

`RsaCryptoService` mã hóa bằng thao tác public RSA và giải mã bằng private RSA với **OAEP-SHA256**. `GetMaxPlaintextSize` tính `k - 2×SHA256.HashSizeInBytes - 2`: RSA-2048 có `k = 256`, nhận tối đa **190 byte**, ciphertext **256 byte**. API hỗ trợ các khóa 2048/3072/4096 bit của `RsaKeyManager`; từ chối plaintext null/rỗng/vượt giới hạn và ciphertext sai độ dài. Sai khóa, public-only key khi decrypt hoặc ciphertext bị sửa đều báo lỗi rõ ràng.

Lệnh `rsa crypto` tạo cặp PEM thử nghiệm mới rồi import lại; mã hóa 32 byte ngẫu nhiên giả lập AES-256 key, lưu Base64 thuần trong `results/rsa/run-<id>/rsa-encrypted.txt`, đọc lại, giải mã và so sánh. PEM tương ứng ở `keys/rsa/run-<id>/`. Demo kiểm tra 1/32/100/max byte, text nhỏ, sai khóa, sửa ciphertext và vượt giới hạn; giữ nguyên các kết quả cũ. Không dùng khóa AES thật từ các demo trước.

OAEP thêm ngẫu nhiên và kiểm tra cấu trúc mã hóa khi giải mã. RSA có giới hạn đầu vào theo modulus/padding và phù hợp với dữ liệu nhỏ như khóa AES; file lớn sẽ cần mã hóa đối xứng. Ví dụ 32 byte ở đây chỉ minh họa bọc khóa, chưa triển khai hybrid encryption hoàn chỉnh.

Build và chạy từ thư mục root:

```powershell
dotnet build information-security-toolkit.slnx
dotnet run --project information-security-toolkit/information-security-toolkit.csproj

# RSA Key Management + PEM
dotnet run --project information-security-toolkit/information-security-toolkit.csproj -- rsa keys

# RSA Encrypt / Decrypt dữ liệu nhỏ
dotnet run --project information-security-toolkit/information-security-toolkit.csproj -- rsa crypto

# Quick benchmark (khuyên dùng Release khi thu thập số liệu)
dotnet run -c Release --project information-security-toolkit/information-security-toolkit.csproj -- benchmark quick

# Full benchmark: tự chạy khi cần, có thể dùng nhiều GB dung lượng
dotnet run -c Release --project information-security-toolkit/information-security-toolkit.csproj -- benchmark full
```
