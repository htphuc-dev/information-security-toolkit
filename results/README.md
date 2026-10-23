# Results

Lưu kết quả benchmark, đối chiếu file và các output phát sinh tại đây.

Demo AES file tạo một thư mục `run-<id>/` mới mỗi lần chạy:

- `<tên-file>.enc`: file đã mã hóa từ `test-data/`.
- `<tên-file>.decrypted`: file đã giải mã, được so sánh từng byte với file nguồn.
- `checks/`: file phục vụ kiểm tra dữ liệu bị sửa và các tình huống lỗi.

Ví dụ: `test-data/sample.txt` tạo `run-<id>/sample.txt.enc` và `run-<id>/sample.txt.decrypted`. Kết quả cũ được giữ nguyên.

Benchmark lưu output tại `benchmark/run-<RunId>/` (`.enc`, `.dec`), append số liệu vào `benchmark/aes-benchmark.csv`. CSV ghi kích thước, thời gian, throughput, SHA-256 hai file và PASS/FAIL; cột `RunId` xác định thư mục output. Kết quả cũ được giữ nguyên, nên các lần Full tiếp theo cần thêm dung lượng đĩa.

Kết quả thực nghiệm giai đoạn 10 được tổng hợp tại [summary/experiment-summary.md](summary/experiment-summary.md). Hai CSV benchmark, `experiments/sha256-avalanche.csv`, `experiments/signature-results.txt` và summary được phép đưa vào Git; output nhị phân, transcript local và các file phát sinh khác vẫn bị ignore.

Đợt thực nghiệm chính thức đã dọn riêng các output lớn vừa tạo sau khi kiểm tra. Khi chuẩn bị release giai đoạn 11, các nguồn benchmark và output nhị phân cũ còn lại cũng đã được dọn; CSV, summary và transcript thực nghiệm được giữ nguyên. Benchmark chạy lại bằng CLI sẽ tạo lại nguồn cần thiết và giữ output cho đến khi người dùng dọn.
