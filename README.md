# Information Security Toolkit

## 1. Giới thiệu

Project môn **An toàn và bảo mật thông tin**, thực hành AES, RSA, SHA-256, chữ ký số RSA-PSS và đo hiệu năng. Console phục vụ demo/thực nghiệm; WinForms cung cấp giao diện tiếng Việt dùng lại các service hiện có.

---
<img width="722" height="512" alt="Screenshot 2026-10-23 235836" src="https://github.com/user-attachments/assets/5a926925-69dd-468b-ac63-ce397c00e3af" />

---
## 2. Công nghệ sử dụng

- C#, .NET 10; Console target `net10.0`, WinForms target `net10.0-windows`.
- Windows Forms, font Segoe UI; `System.Security.Cryptography`.
- Git/GitHub; solution `.slnx`; phát triển trên Windows 11.

Project test xUnit giai đoạn 9 hiện không có trong workspace. Kết quả 50/50 PASS là báo cáo lịch sử, chưa được chạy lại trong đợt chuẩn bị release này.

## 3. Chức năng chính

### AES

- Sinh khóa AES-256 ngẫu nhiên 32 byte; lưu/nạp Base64 và kiểm tra độ dài.
- Văn bản UTF-8/Unicode: AES-GCM, nonce ngẫu nhiên 12 byte, tag 16 byte; output `Base64(nonce | tag | ciphertext)`.
- Tệp: AES-256-CBC/PKCS7 + HMAC-SHA256, Encrypt-then-MAC. HKDF-SHA256 tách khóa AES/HMAC; salt và IV ngẫu nhiên.
- Xử lý streaming, buffer 64 KiB; xác minh HMAC trước khi giải mã. File v1: `ISTF | version | salt | IV | HMAC | ciphertext`.
- Backend mặc định `overwrite=false`. WinForms hỏi Yes/No khi tệp đích tồn tại; chỉ thay tệp cũ sau khi xử lý thành công. No hủy bình thường.
- Benchmark tệp tới 1 GB; so sánh kích thước và SHA-256 nguồn/giải mã.

### RSA

- Sinh cặp khóa mặc định 2048 bit, hỗ trợ 3072/4096 bit.
- Export/import public PEM dạng SPKI, private PEM dạng PKCS#8; xem `n, e, d, p, q, dp, dq, inverseQ`.
- Public-key encrypt/private-key decrypt với OAEP-SHA256. RSA-2048 nhận tối đa **190 byte**, tạo ciphertext 256 byte.
- Demo dữ liệu nhỏ, lưu/nạp ciphertext Base64; benchmark public/private operations với đầu vào 32 byte.

### SHA-256

- Hash chuỗi UTF-8 hoặc `byte[]`, hỗ trợ rỗng và Unicode; trả Hex uppercase 64 ký tự = 32 byte = 256 bit.
- Hamming distance và tỷ lệ bit khác biệt minh họa avalanche effect.

### Digital Signature

- SHA-256 + RSA-PSS; ký bằng private key, xác minh bằng public key.
- Phát hiện dữ liệu/chữ ký bị sửa hoặc sai public key.
- Cùng dữ liệu và private key có thể tạo chữ ký khác nhau; cả hai vẫn xác minh hợp lệ.

### WinForms

MainForm mở AES, RSA, SHA-256, Chữ ký số và Đánh giá hiệu năng. Giao diện tiếng Việt dùng control chuẩn; I/O và benchmark chạy nền. Benchmark chỉ bắt đầu khi người dùng chọn chạy.

## 4. Kiến trúc project

```text
information-security-toolkit/
├── information-security-toolkit/         # Logic và Console
│   ├── AES/
│   ├── RSA/
│   ├── Hashing/
│   ├── Signature/
│   ├── Benchmark/
│   ├── Common/
│   └── Program.cs
├── information-security-toolkit.WinForms/
│   ├── Forms/
│   ├── AppPaths.cs
│   ├── Ui.cs
│   └── Program.cs
├── docs/
│   ├── screenshots/
│   └── release-checklist.md
├── test-data/                            # Dữ liệu local, được ignore
├── results/                              # CSV, summary và output local
├── information-security-toolkit.slnx
├── information-security-toolkit.slnLaunch
├── README.md
└── .gitignore
```

WinForms dùng `ProjectReference` tới project Console/logic; Console chạy độc lập. Không có project Core riêng hoặc DLL reference thủ công vào thư mục build.

## 5. Hướng dẫn chạy

Yêu cầu: Windows và **.NET 10 SDK**. Visual Studio cần hỗ trợ .NET 10 và workload **.NET desktop development**. Chạy từ root repository:

```powershell
dotnet clean information-security-toolkit.slnx -m:1
dotnet restore information-security-toolkit.slnx -m:1
dotnet build information-security-toolkit.slnx -c Release -m:1

# Console: demo AES file nhỏ
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj

# WinForms
dotnet run -c Release --no-build --project information-security-toolkit.WinForms/information-security-toolkit.WinForms.csproj
```

Trong Visual Studio, mở `information-security-toolkit.slnx`, chọn launch profile **WinForms** hoặc đặt WinForms làm Startup Project. Profile dùng đường dẫn tương đối; Visual Studio ghi nhớ lựa chọn theo máy.

Các demo Console khác:

```powershell
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- rsa keys
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- rsa crypto
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- hash sha256
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- signature demo
```

Demo AES đọc các tệp trực tiếp trong `test-data/`, bỏ qua README; tự tạo mẫu text, Unicode, rỗng, nhị phân nếu thiếu. Output nằm trong `results/run-<id>/`. Các thư mục dữ liệu được tạo khi cần.

Khóa AES Base64 và private PEM **chưa được mã hóa**; chỉ dùng khóa thử nghiệm. `rsa keys` in cả private parameters ra Console. Không đưa khóa hoặc ảnh chứa khóa bí mật vào Git.

## 6. Chạy benchmark

```powershell
# AES: 1 MB, 10 MB
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- benchmark quick

# AES: 100 MB, 500 MB, 1 GB
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- benchmark full

# RSA: quick=100, normal=1000, extended=10000 iterations
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- rsa benchmark quick
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- rsa benchmark normal
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- rsa benchmark extended
```

Đơn vị: **1 MB = 1.000.000 byte**, 1 GB = 1.000 MB. AES Full lần đầu cần trên 4,8 GB cho nguồn và output; kiểm tra dung lượng trước khi chạy. Source/encrypted/decrypted được giữ local và ignore; CSV append, không thay kết quả cũ.

Thời gian đo loại trừ tạo dữ liệu/khóa, warm-up, SHA-256 đối chiếu và ghi CSV. AES đo cả CBC, HMAC và I/O; RSA đo các vòng service OAEP. Cache, JIT, GC và tải máy có thể ảnh hưởng số liệu.

## 7. Kết quả thực nghiệm

Số liệu Release ngày **23/10/2026**, đợt `20261023T153951Z`; mỗi mức một lần đo. Đây là số liệu đã lưu, không phải ngưỡng tốc độ cam kết.

| AES | Encrypt ms | Decrypt ms | Encrypt MB/s | Decrypt MB/s | Integrity |
|---|---:|---:|---:|---:|---|
| 1 MB | 7.869 | 3.935 | 127.08 | 254.11 | PASS |
| 10 MB | 53.117 | 22.162 | 188.26 | 451.22 | PASS |
| 100 MB | 241.614 | 164.682 | 413.88 | 607.23 | PASS |
| 500 MB | 1406.188 | 828.297 | 355.57 | 603.65 | PASS |
| 1 GB | 3239.574 | 1685.956 | 308.68 | 593.14 | PASS |

Cả 5 tệp có kích thước và SHA-256 sau giải mã bằng nguồn.

| RSA iterations | Public encrypt avg ms | Private decrypt avg ms | Private/Public ratio |
|---|---:|---:|---:|
| 100 | 0.018553 | 0.571488 | 30.80x |
| 1.000 | 0.024431 | 0.621827 | 25.45x |
| 10.000 | 0.018039 | 0.532804 | 29.54x |

RSA-2048/OAEP-SHA256, dữ liệu 32 byte; round-trip đều PASS. Trong ba lần đo, private decryption chậm hơn public encryption. AES đã xử lý tệp 1 GB theo stream; RSA được dùng cho dữ liệu nhỏ như khóa AES, không mã hóa trực tiếp tệp lớn trong project này.

SHA-256 có 4 cặp thay một ký tự, khác **122–136/256 bit**; cặp giống nhau khác 0 bit. RSA-PSS tạo hai chữ ký khác nhau do random salt và xác minh cả hai thành công.

- [AES benchmark CSV](results/benchmark/aes-benchmark.csv).
- [RSA benchmark CSV](results/benchmark/rsa-benchmark.csv).
- [SHA-256 avalanche CSV](results/experiments/sha256-avalanche.csv).
- [RSA-PSS transcript](results/experiments/signature-results.txt).
- [Báo cáo thực nghiệm và điều kiện đo](results/summary/experiment-summary.md).

## 8. Lỗi Windows App Control

Nếu gặp `An Application Control policy has blocked this file` / `0x800711C7`, ghi đúng tệp và đường dẫn bị chặn, dừng lần chạy đó và kiểm tra **Event Viewer → Microsoft → Windows → CodeIntegrity → Operational**, Event ID 3077/3089. Đây là lỗi chính sách Windows, khác lỗi compile/project; không đổi thuật toán hoặc thêm script bypass/unblock.
