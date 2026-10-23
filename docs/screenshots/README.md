# Screenshot minh chứng

Chưa có screenshot thực tế. Chụp thủ công và lưu PNG với tên bên dưới; không tạo ảnh mô phỏng hoặc thay số liệu để làm minh chứng.

| Tên tệp | Thao tác và nội dung cần thấy |
|---|---|
| `main-form.png` | MainForm với các nút AES, RSA, SHA-256, Chữ ký số và Đánh giá hiệu năng. |
| `aes.png` | Sinh khóa, vào tab Mã hóa tệp; mã hóa/giải mã mẫu Unicode, thấy So sánh tệp: KHỚP. Để khóa ở tab khác. |
| `rsa.png` | Sinh cặp khóa RSA, mã hóa văn bản ngắn rồi giải mã. Chụp văn bản kết quả, không chụp private parameters. |
| `sha256.png` | Nhập `abc` và `abd`, chọn Tính SHA-256; thấy hai hash, 122 bit khác nhau và 47,66% (dấu thập phân tùy môi trường). |
| `signature.png` | Sinh cặp khóa RSA-2048, Ký số rồi Ký lại với dữ liệu giữ nguyên; hai chữ ký khác nhau và đều xác minh hợp lệ. |
| `benchmark.png` | Số liệu AES 1 GB giai đoạn 10 từ transcript local; thấy rõ đơn vị và Integrity: PASS. |

Có thể thêm `benchmark-rsa.png` cho RSA 10.000 iterations. Chỉ dùng dữ liệu demo; tránh đưa khóa Base64 hoặc private PEM vào ảnh được commit.

## Chạy giao diện

Từ root repository:

```powershell
dotnet run -c Release --project information-security-toolkit.WinForms/information-security-toolkit.WinForms.csproj
```

## Hiển thị lại kết quả đã đo

Transcript local được giữ lại và Git ignore. Các lệnh này chỉ đọc số liệu, không tạo lại tệp 1 GB:

```powershell
Get-Content results/experiments/run-20261023T153951Z/aes-full.console.txt
Get-Content results/experiments/run-20261023T153951Z/rsa-extended.console.txt
Get-Content results/experiments/run-20261023T153951Z/sha256.console.txt
Get-Content results/experiments/signature-results.txt -Encoding UTF8
```

Nếu clone repository trên máy khác, transcript local có thể không có. Dùng CSV/summary đã commit để đối chiếu hoặc chủ động chạy benchmark mới; không trình bày ảnh của lần chạy mới là số đo cũ.

Sau khi ảnh tồn tại, nhúng vào README root bằng relative path, ví dụ `![MainForm](docs/screenshots/main-form.png)`. Hiện README chỉ liệt kê tên cần chụp để tránh link ảnh chưa tồn tại.
