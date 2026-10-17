# Results

Lưu kết quả benchmark, đối chiếu file và các output phát sinh tại đây.

Demo AES file tạo một thư mục `run-<id>/` mới mỗi lần chạy:

- `<tên-file>.enc`: file đã mã hóa từ `test-data/`.
- `<tên-file>.decrypted`: file đã giải mã, được so sánh từng byte với file nguồn.
- `checks/`: file phục vụ kiểm tra dữ liệu bị sửa và các tình huống lỗi.

Ví dụ: `test-data/sample.txt` tạo `run-<id>/sample.txt.enc` và `run-<id>/sample.txt.decrypted`. Kết quả cũ được giữ nguyên.

Nội dung thư mục được Git bỏ qua, ngoại trừ README.md này. Kết quả cần đưa vào báo cáo có thể được tóm tắt trong docs.
