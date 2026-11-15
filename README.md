# Dưa Hấu Coffee

Website đặt món và quản lý quán cà phê. Khách có thể xem thực đơn, đặt món và theo dõi đơn hàng. Chủ quán có trang quản lý riêng để cập nhật món, xử lý đơn và trả lời tin nhắn của khách.

## Tính năng

| Khách hàng | Chủ quán |
| --- | --- |
| Tìm món theo tên, lọc theo danh mục và xem ảnh, mô tả, giá theo kích cỡ. | Thêm, sửa, xóa món, tải nhiều ảnh, đổi giá và gạt công tắc còn hàng hoặc hết hàng. |
| Đăng ký bằng số điện thoại, đăng nhập bằng mật khẩu hoặc qua Google và Facebook khi đã cấu hình OAuth. | Tra cứu đơn theo mã đơn, tài khoản khách, tên người nhận hoặc số điện thoại. |
| Thêm món vào giỏ, nhập thông tin giao hàng trong hộp thoại và theo dõi đơn của mình. | Xem món, số lượng, giá tiền và thông tin giao hàng. Hủy đơn đang chờ hoặc xác nhận đã giao. |
| Đánh giá công khai từ 1 đến 5 sao và viết nhận xét cho món đã nhận. | Xem đánh giá của khách và có thể xóa tin nhắn do khách gửi. |

Chat giữa khách và chủ quán dùng SignalR. Bong bóng chat ở góc dưới bên phải, khung chat có thể thu nhỏ, và lịch sử tin nhắn được lưu trong cơ sở dữ liệu.

## Công nghệ

- ASP.NET Core MVC trên .NET 10, Razor, HTML, CSS và JavaScript.
- Entity Framework Core và SQL Server Express LocalDB.
- SignalR cho trò chuyện thời gian thực. Xác thực bằng cookie, phân quyền khách hàng và chủ quán.

## Chạy trên Windows

1. Cài .NET 10 SDK, Visual Studio hỗ trợ .NET 10 với workload **ASP.NET and web development**, và SQL Server Express LocalDB.
2. Mở [Coffe.slnx](Coffe.slnx) trong Visual Studio.
3. Chọn profile **Dua Hau Coffee** và nhấn **F5**. Website mở ở `http://localhost:5500`. Ứng dụng tự áp dụng migration để tạo cơ sở dữ liệu `CafeDuaHau` và nạp thực đơn mẫu.

Bạn cũng có thể chạy từ thư mục chứa solution.

```powershell
dotnet run --project .\coffee\do_an_1.csproj --launch-profile "Dua Hau Coffee"
```

### Tạo tài khoản chủ quán lần đầu

Chạy lệnh sau từ thư mục chứa solution và nhập mật khẩu bạn chọn khi được nhắc.

```powershell
dotnet run --project .\coffee\do_an_1.csproj --no-launch-profile -- --setup-admin
```

Sau đó mở website, chọn **Đăng nhập** và dùng tên `admin`. Khách tự tạo tài khoản bằng nút **Đăng ký**.

### Bật Google và Facebook

Hai nhà cung cấp cần thông tin OAuth riêng. Lưu thông tin vào **Manage User Secrets** trong Visual Studio rồi chạy profile **Dua Hau Coffee (HTTPS)** ở `https://localhost:5501`. Xem [hướng dẫn cấu hình Google và Facebook](coffee/DangNhapGoogleFacebook.md) để biết URL chuyển hướng và tên các khóa cấu hình.

## Cơ sở dữ liệu

LocalDB mặc định dùng instance `(localdb)\MSSQLLocalDB`. Cơ sở dữ liệu lưu món, giá theo kích cỡ, ảnh tải lên, tài khoản, đơn hàng, đánh giá và tin nhắn.

- [CafeDuaHau.sql](coffee/Database/CafeDuaHau.sql) tạo hoặc cập nhật cấu trúc bảng và nạp dữ liệu mẫu bằng SQL Server.
- [Sơ đồ quan hệ](coffee/Database/SoDoDatabase.md) mô tả các bảng và khóa ngoại.

Khi đưa dự án lên GitHub, giữ tệp SQL và migrations trong mã nguồn. Bỏ qua các thư mục `.vs/`, `bin/`, `obj/`, các tệp `App_Data/*.mdf`, `App_Data/*.ldf` và mọi thông tin đăng nhập OAuth.
