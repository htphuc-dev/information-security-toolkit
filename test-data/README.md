# Test data

Lưu dữ liệu thử và các file lớn dùng cho benchmark tại đây.

Đặt file cần kiểm thử trực tiếp trong thư mục này, ví dụ `sample.txt`. Program đọc tất cả file ở cấp này, bỏ qua `README.md` và `.gitkeep`; chưa duyệt thư mục con. Các file mẫu text, Unicode, rỗng và nhị phân được tạo nếu chưa có, không ghi đè file sẵn có.

Chạy `dotnet run --project information-security-toolkit/information-security-toolkit.csproj` từ root repository. Kết quả được lưu trong `results/run-<id>/`.

Nội dung thư mục được Git bỏ qua, ngoại trừ README.md này. Tạo dữ liệu thử cục bộ; không dùng dữ liệu nhạy cảm hoặc khóa bí mật làm dữ liệu mẫu được commit.
