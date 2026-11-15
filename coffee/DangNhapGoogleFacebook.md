# Bật đăng nhập Google và Facebook

Chọn profile **Dua Hau Coffee (HTTPS)** trong Visual Studio. Website chạy ở `https://localhost:5501` với profile này. Nếu trình duyệt chưa tin cậy chứng chỉ phát triển, chạy `dotnet dev-certs https --trust`.

## Google

Tạo OAuth client loại **Web application** trong Google Cloud và thêm URL chuyển hướng `https://localhost:5501/signin-google`. Nếu ứng dụng Google ở chế độ thử nghiệm, thêm tài khoản thử nghiệm trong cấu hình Google.

Trong terminal của thư mục dự án (thư mục chứa `do_an_1.csproj`), chạy:

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "<Client ID của bạn>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<Client secret của bạn>"
```

[Tài liệu cấu hình Google cho ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authentication/social/google-logins)

## Facebook

Tạo app với Facebook Login, sau đó thêm `https://localhost:5501/signin-facebook` trong **Valid OAuth Redirect URIs**. Khi app ở chế độ phát triển, dùng tài khoản có quyền thử nghiệm trên app.

```powershell
dotnet user-secrets set "Authentication:Facebook:AppId" "<App ID của bạn>"
dotnet user-secrets set "Authentication:Facebook:AppSecret" "<App secret của bạn>"
```

[Tài liệu cấu hình Facebook cho ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authentication/social/facebook-logins)

Chạy lại ứng dụng bằng profile HTTPS sau khi lưu cấu hình. Lần đầu đăng nhập bằng Google hoặc Facebook, khách nhập số điện thoại trước khi website tạo tài khoản.
