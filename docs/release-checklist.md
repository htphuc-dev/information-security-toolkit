# Release checklist — Information Security Toolkit

Ngày rà soát: **23/10/2026 (UTC+07:00)**. Phiên bản dự kiến: **v1.0.0**. Các dấu kiểm phản ánh việc đã chạy/đối chiếu trong lần rà soát này; không thay thế kiểm tra demo thủ công.

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
- [ ] Screenshot complete — còn thiếu cả 6 ảnh liệt kê bên dưới.
- [x] No secret tracked — không thấy `.key`/PEM private hoặc private-key blob nhúng trong tracked files và các file đang chờ commit.
- [x] No large benchmark file tracked — không thấy tệp benchmark lớn trong tracked files/candidate files.
- [x] Git status reviewed — phân loại bên dưới; không stage hoặc commit.
- [ ] Ready for v1.0.0 — chờ screenshot, rà soát demo thủ công và quyết định cuối của người nộp.

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

Kiểm tra mở/đóng không chứng minh toàn bộ bố cục hoặc mọi tương tác người dùng. Cần thực hiện thủ công:

- [ ] Xem chữ tiếng Việt, font, căn lề và nội dung ở mức DPI của máy dùng để trình bày.
- [ ] AES: Sinh → Lưu → Nạp khóa; kiểm tra hộp chọn tệp.
- [ ] AES: văn bản Unicode round-trip; file round-trip; thử ghi đè Yes và hủy No trong phiên demo thực tế.
- [ ] RSA: thao tác public/private PEM và mã hóa/giải mã trong giao diện.
- [ ] SHA-256: nhập hai dữ liệu và kiểm tra kết quả.
- [ ] Chữ ký số: Ký số → Xác minh → Ký lại; sửa dữ liệu rồi xác minh.
- [ ] Benchmark UI: chỉ chạy Quick nếu cần; có thể dùng kết quả Console đã lưu cho phần file lớn.

Đợt sửa UX ghi đè trước đó đã có 24 kiểm tra backend/handler WinForms PASS. Kết quả đó là lịch sử riêng; checklist trên vẫn giữ yêu cầu diễn tập thủ công trước khi nộp.

## Automated tests giai đoạn 9

Thư mục `tests/information-security-toolkit.Tests` hiện không có. Solution chứa hai project ứng dụng và một solution folder `/tests/` rỗng. Không xóa hoặc tái tạo test project trong giai đoạn này.

TRX local còn ghi 50 passed, 0 failed từ giai đoạn 9; được giữ trong `TestResults/` và ignore. Không coi đó là kết quả chạy lại với source hiện tại. Cần khôi phục test source nếu muốn kiểm tra regression trước release.

## Screenshot còn thiếu

- [ ] `docs/screenshots/main-form.png`.
- [ ] `docs/screenshots/aes.png`.
- [ ] `docs/screenshots/rsa.png`.
- [ ] `docs/screenshots/sha256.png`.
- [ ] `docs/screenshots/signature.png`.
- [ ] `docs/screenshots/benchmark.png`.

[Hướng dẫn chụp](screenshots/README.md) có kịch bản từng ảnh. Không chụp khóa AES/private PEM, không dùng ảnh mô phỏng làm minh chứng.

## Kết quả được giữ lại

- [AES CSV](../results/benchmark/aes-benchmark.csv): 1/10/100/500 MB và 1 GB, đúng các RunId trong summary.
- [RSA CSV](../results/benchmark/rsa-benchmark.csv): 100/1.000/10.000 iterations, đúng timestamp trong summary.
- [SHA-256 CSV](../results/experiments/sha256-avalanche.csv).
- [RSA-PSS transcript](../results/experiments/signature-results.txt).
- [Experiment summary](../results/summary/experiment-summary.md).

CSV benchmark vẫn có lịch sử các lần đo cũ. Không dùng toàn bộ CSV làm một lần đo duy nhất; summary chỉ rõ đợt chính thức `20261023T153951Z`. Transcript local của đợt đó được giữ để đọc lại/chụp ảnh.

## Cleanup và secrets

Đã dọn khoảng **8,17 GB** artifact/dữ liệu sinh tự động: cache `.vs`, output build cũ, 5 nguồn benchmark, output mã hóa/giải mã cũ và các AES key/RSA PEM demo. Output và khóa mới từ smoke test cũng được dọn. Giữ nguyên source, 4 mẫu nhỏ trong `test-data/`, CSV, summary, transcript thực nghiệm và TRX lịch sử.

Release `bin/obj` được tạo lại bởi build và tiếp tục bị ignore. Các helper kiểm tra local nằm trong thư mục ignored, không phải project của solution. Không rewrite Git history, không chỉnh security policy hoặc registry.

## Git status và phạm vi commit

Nên đưa vào commit sau khi tự kiểm tra:

- `.gitignore`, `README.md`, `docs/release-checklist.md`, `docs/screenshots/README.md`.
- `results/README.md`, `results/summary/experiment-summary.md` và các CSV/transcript nhỏ được cho phép bởi `.gitignore`.
- Các thay đổi đã có từ bước UX trước trong `AesForm.cs`, `AesFileService.cs`.
- `information-security-toolkit.slnx` đang có thay đổi từ trước (solution folder tests rỗng); rà soát nếu muốn giữ thay đổi này.
- Screenshot PNG thật sau khi chụp.

Không đưa vào commit: `bin/`, `obj/`, `.vs/`, TestResults/coverage, khóa, PEM, dữ liệu benchmark lớn, output `.enc/.dec/.decrypted`, helper/log local và publish binaries trong `artifacts/`.

Giai đoạn 11 không sửa source crypto hoặc kiến trúc, không tự dùng git add/commit/push/tag/reset/rebase/clean/rm.

## Publish và release dự kiến

- [ ] Publish thử (tùy chọn, chưa thực hiện).
- [ ] Kiểm tra bản publish (nếu chọn publish).
- [ ] Người dùng chọn file commit và quyết định tạo tag/release.

```powershell
dotnet publish information-security-toolkit.WinForms/information-security-toolkit.WinForms.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish/win-x64
```

`artifacts/` được ignore. Ứng dụng hiện cần tìm `.slnx` trong thư mục cha qua `AppPaths`; giữ publish output bên trong cây repository. Chưa xác nhận khả năng chạy khi chỉ copy riêng publish folder ra ngoài repo. Không tạo MSI/MSIX/ClickOnce hoặc GitHub Release.

Gợi ý thông tin release:

- Version: `v1.0.0`.
- Title: `Information Security Toolkit v1.0.0`.
- Notes: AES-256 Key/Text/File và integrity; RSA PEM/OAEP; SHA-256 avalanche; RSA-PSS; WinForms tiếng Việt; benchmark và số liệu thực nghiệm.

## Nếu Windows chặn ứng dụng

Nếu gặp `0x800711C7` / `An Application Control policy has blocked this file`, dừng lần chạy đó, ghi chính xác tên/đường dẫn tệp bị chặn và xem **Event Viewer → Applications and Services Logs → Microsoft → Windows → CodeIntegrity → Operational**, Event ID 3077/3089. Không xử lý bằng đổi thuật toán, tắt security policy hay chạy script unblock.
