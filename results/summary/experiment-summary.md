# Experimental Results

Đợt thực nghiệm: `20261023T153951Z` (23/10/2026, UTC; giờ Việt Nam UTC+07:00).
Chỉ các phép đo mới của đợt này được tổng hợp dưới đây. CSV benchmark giữ nguyên lịch sử cũ và append số liệu mới.

## Điều kiện thực nghiệm

- Release build, .NET SDK 10.0.402, target `net10.0`, chạy Console không qua debugger.
- Hệ điều hành: Microsoft Windows NT 10.0.26300.0; CPU identifier: AMD64 Family 23 Model 104 Stepping 1, AuthenticAMD; 12 logical processors.
- Git HEAD: `da7f6550d50aeb532d6ce450fde79e9d8db9ea79`; workspace có thay đổi chưa commit.
- Dung lượng trống trước thực nghiệm: 100125261824 byte (~100.13 GB).
- Đơn vị thập phân: 1 MB = 1.000.000 byte; 1 GB = 1.000 MB = 1.000.000.000 byte.
- Tái sử dụng đủ 5 tệp nguồn có sẵn, đã được module kiểm tra kích thước. Dữ liệu benchmark được tạo bằng một block ngẫu nhiên 1 MiB lặp lại; không đại diện mọi loại dữ liệu thực tế.
- Mỗi kích thước/mức chạy được đo một lần. Cache I/O, JIT, GC và tải nền có thể ảnh hưởng số liệu; không kiểm soát hoặc đóng ứng dụng khác của người dùng.
- AES warm-up 64 KiB, RSA warm-up 10 cặp encrypt/decrypt. Tạo dữ liệu/khóa, warm-up, SHA-256 đối chiếu, Console và ghi CSV nằm ngoài thời gian đo.
- Release solution build: PASS, 0 warning, 0 error. Không thay đổi thuật toán, padding, format hoặc service.

## AES

**AES-256-CBC/PKCS7 + HMAC-SHA256 (Encrypt-then-MAC)**, xử lý streaming; HKDF-SHA256 tách khóa mã hóa và khóa MAC. Giữ nguyên service hiện có. Thời gian đo gồm thao tác mã hóa/xác thực và I/O của service, bao gồm các lượt đọc cần thiết cho HMAC; không chỉ đo riêng primitive AES.

| Size | Encrypt Time (ms) | Decrypt Time (ms) | Encrypt MB/s | Decrypt MB/s | Integrity |
|---|---:|---:|---:|---:|---|
| 1 MB | 7.869 | 3.935 | 127.08 | 254.11 | PASS |
| 10 MB | 53.117 | 22.162 | 188.26 | 451.22 | PASS |
| 100 MB | 241.614 | 164.682 | 413.88 | 607.23 | PASS |
| 500 MB | 1406.188 | 828.297 | 355.57 | 603.65 | PASS |
| 1 GB | 3239.574 | 1685.956 | 308.68 | 593.14 | PASS |

Trong lần chạy này, thời gian mã hóa và giải mã tăng theo kích thước tệp. Với nhóm 100 MB–1 GB, throughput mã hóa nằm trong khoảng 308,68–413,88 MB/s, giải mã 593,14–607,23 MB/s. Throughput thay đổi giữa các mức; chưa có đủ lần lặp để kết luận độ ổn định. Cả 5 tệp sau giải mã giữ nguyên kích thước và SHA-256 nguồn.

| Size | File size equal | Source SHA-256 | Decrypted SHA-256 |
|---|---|---|---|
| 1 MB | true | `D0DCA620C9442E478589811966E2AEDB65B88C8D6EF5103B4D02C2808E636BDA` | `D0DCA620C9442E478589811966E2AEDB65B88C8D6EF5103B4D02C2808E636BDA` |
| 10 MB | true | `17BF3ED6B71CEE8838CFECBEA9A93F09DFFD97AA583EDD2C9C610A79BA64A2C7` | `17BF3ED6B71CEE8838CFECBEA9A93F09DFFD97AA583EDD2C9C610A79BA64A2C7` |
| 100 MB | true | `B31A2FD7E318E5FA4142287269C481C924C1265DFECC10FA05B5E42BF6ED2653` | `B31A2FD7E318E5FA4142287269C481C924C1265DFECC10FA05B5E42BF6ED2653` |
| 500 MB | true | `4ACB475985677F76B5FFD52DD93896CCE955A7BF994AAFEBB5AD8A6111B6384A` | `4ACB475985677F76B5FFD52DD93896CCE955A7BF994AAFEBB5AD8A6111B6384A` |
| 1 GB | true | `A1AD351654308B12AF730A39070ABB6EB08A28AACB3E33134F0FB7DD6AD40095` | `A1AD351654308B12AF730A39070ABB6EB08A28AACB3E33134F0FB7DD6AD40095` |

Số đo đầy đủ: [aes-benchmark.csv](../benchmark/aes-benchmark.csv). Các dòng chính thức có RunId:

- `16f21c26282141cd85d23c4b1729ec1f`
- `99f6595b0e6c41aca673de6760af6016`

## RSA

RSA-2048, OAEP-SHA256, đầu vào 32 byte; encrypt dùng public-only key, decrypt dùng private key. Mỗi mức sinh cặp khóa và dữ liệu thử nghiệm riêng. Vòng decrypt dùng lại ciphertext cuối của vòng encrypt theo module hiện có.

| Iterations | Encrypt Avg (ms/op) | Decrypt Avg (ms/op) | Encrypt Ops/s | Decrypt Ops/s | Ratio | Round-trip |
|---|---:|---:|---:|---:|---:|---|
| 100 | 0.018553 | 0.571488 | 53899.64 | 1749.82 | 30.80x | PASS |
| 1000 | 0.024431 | 0.621827 | 40931.10 | 1608.17 | 25.45x | PASS |
| 10000 | 0.018039 | 0.532804 | 55435.29 | 1876.86 | 29.54x | PASS |

| Iterations | Encrypt Total (ms) | Decrypt Total (ms) | Timestamp UTC |
|---|---:|---:|---|
| 100 | 1.8553 | 57.1488 | 2026-10-23T15:40:51.6256330+00:00 |
| 1000 | 24.4313 | 621.8266 | 2026-10-23T15:40:52.9635714+00:00 |
| 10000 | 180.3905 | 5328.0377 | 2026-10-23T15:40:55.1279960+00:00 |

Ở cả 3 mức đo, thao tác public-key encryption nhanh hơn private-key decryption. Tỷ lệ decrypt average / encrypt average lần lượt là 30,80x; 25,45x; 29,54x. Đây là tỷ lệ thực đo của từng mức, không phải hằng số. Round-trip đều PASS. RSA-2048/OAEP-SHA256 nhận tối đa 190 byte; bài đo này chỉ dùng 32 byte, phù hợp minh họa bọc khóa AES thay vì mã hóa trực tiếp tệp lớn.

Số đo đầy đủ: [rsa-benchmark.csv](../benchmark/rsa-benchmark.csv); chọn 3 timestamp trên để phân biệt lịch sử cũ.

## SHA-256

| Input A | Input B | Different Bits | Difference |
|---|---|---:|---:|
| hello | Hello | 125 / 256 | 48.83% |
| Hello world | Hello worle | 129 / 256 | 50.39% |
| abc | abd | 122 / 256 | 47.66% |
| An toàn và bảo mật thông tin | An toàn và bảo mật thông tio | 136 / 256 | 53.12% |

Chỉ thay một ký tự ở mỗi cặp nhưng 122–136 trong 256 bit của hash đã thay đổi, minh họa avalanche effect. Không yêu cầu một cặp riêng lẻ phải đạt đúng 50%. Cặp đối chứng `hello` / `hello` cho 0 bit khác nhau (0,00%).

Hash A, Hash B và số bit của cả 4 cặp cùng cặp đối chứng được ghi trong [sha256-avalanche.csv](../experiments/sha256-avalanche.csv). Tỷ lệ trong CSV giữ cách hiển thị 2 chữ số thập phân của Console.

## RSA-PSS Digital Signature

RSA-2048; hash SHA-256; padding RSA-PSS. Dữ liệu gốc: `Information Security Toolkit`. RSA private keys chỉ tồn tại trong bộ nhớ, không export.

| Check | Result |
|---|---|
| Verify original | PASS |
| Modified data rejected | PASS |
| Wrong public key rejected | PASS |
| Tampered signature rejected | PASS |
| Signatures different | PASS |
| Signature #1 verify | PASS |
| Signature #2 verify | PASS |
| RSA-PSS randomized signature test | PASS |
| Digital signature demo | PASS |

Demo xác nhận **Same data: YES**, **Same private key: YES**. RSA-PSS dùng random salt nên hai chữ ký khác nhau; cả chữ ký #1 và #2 vẫn xác minh hợp lệ bằng public key tương ứng. Các kiểm tra dữ liệu rỗng, Unicode, public-only key không thể ký và đầu vào null cũng PASS trong transcript.

Kết quả và hai chữ ký Base64 thực tế: [signature-results.txt](../experiments/signature-results.txt). Không benchmark tốc độ ký.

## Tệp tạm và khả năng kiểm tra lại

- Không tạo mới tệp nguồn benchmark lớn: tái sử dụng 5 tệp có sẵn trong `test-data/benchmark/`, tổng 1.611.000.000 byte; tất cả tiếp tục bị Git ignore.
- Đã xóa 10 output `.enc`/`.dec` của riêng đợt này (3.222.000.425 byte, khoảng 3,22 GB), trong 2 thư mục RunId nêu trên. Warm-up được module tự xóa.
- Đã xóa khóa demo AES vừa được sinh trong `information-security-toolkit/bin/Release/net10.0/keys/aes-demo.key`; không xóa khóa hoặc output cũ.
- Giữ nguyên 9 thư mục benchmark cũ và toàn bộ CSV lịch sử. Các đường dẫn RunId của lần chạy mới trong CSV xác định lần đo, output nhị phân tương ứng đã được dọn.
- Transcript Console, manifest và danh sách cleanup được giữ local tại `results/experiments/run-20261023T153951Z/`, vẫn bị Git ignore. Chỉ cho phép Git nhận đúng 2 CSV benchmark, CSV avalanche, kết quả chữ ký và summary.
- Không chụp screenshot tự động. Có thể mở terminal và dùng `Get-Content` với các transcript local dưới đây để chụp đúng số đo đã ghi:

```powershell
Get-Content results/experiments/run-20261023T153951Z/aes-full.console.txt
Get-Content results/experiments/run-20261023T153951Z/rsa-extended.console.txt
Get-Content results/experiments/run-20261023T153951Z/sha256.console.txt
Get-Content results/experiments/signature-results.txt -Encoding UTF8
```

## Chạy lại

```powershell
dotnet build information-security-toolkit.slnx -c Release
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- benchmark quick
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- benchmark full
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- rsa benchmark quick
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- rsa benchmark normal
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- rsa benchmark extended
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- hash sha256
dotnet run -c Release --no-build --project information-security-toolkit/information-security-toolkit.csproj -- signature demo
```

Các lệnh trên tạo lần đo mới và append CSV benchmark; số liệu có thể thay đổi. Kiểm tra dung lượng trước `full`; nếu tái sử dụng nguồn thì cần thêm khoảng 3,2 GB cho output, nếu tạo cả nguồn thì trên 4,8 GB. Sau một lần chạy thủ công, output được module giữ lại cho đến khi người dùng dọn.

## Giới hạn và vấn đề được phát hiện

- Không có mức benchmark bị bỏ qua: AES đủ 1/10/100/500 MB và 1 GB; RSA đủ 100/1.000/10.000 iterations. Không có kết quả FAIL trong đợt này.
- Thư mục `tests/information-security-toolkit.Tests` không có trong workspace tại thời điểm thực nghiệm. Solution hiện chỉ có Console, WinForms và solution folder `/tests/` rỗng. Vì vậy đợt này không chạy lại hoặc xác nhận lại kết quả 50 test của giai đoạn 9; cần khôi phục project test nếu muốn kiểm tra regression.
- Không sửa source crypto, WinForms hoặc solution trong giai đoạn 10. Không git add, commit hoặc push.

## Ghi chú khi chuẩn bị release — Giai đoạn 11

Ngày 23/10/2026, đã dọn các nguồn benchmark và output nhị phân cũ còn lại, cùng khóa demo và artifact build tạm; tổng cleanup khoảng 8,17 GB. Việc giữ nguồn và 9 thư mục output cũ được mô tả ở trên là trạng thái tại cuối giai đoạn 10, trước lần cleanup này. CSV, hash, thời gian, PASS/FAIL và các bảng số liệu được giữ nguyên; transcript local của đợt `20261023T153951Z` vẫn còn để đối chiếu/chụp ảnh. Các tệp benchmark nguồn sẽ được module tạo lại khi chạy lần sau.

Release solution build và các demo Console ngắn PASS; MainForm và 5 form con đã mở/đóng thành công. Không benchmark 1 GB lại để chuẩn bị release. Xem [release checklist](../../docs/release-checklist.md) cho screenshot còn thiếu, phạm vi smoke và giới hạn publish hiện tại.
