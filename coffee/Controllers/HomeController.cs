using Microsoft.AspNetCore.Mvc;

namespace CafeDuaHau.Controllers;

public class HomeController : Controller
{
    // Trang 404 (cấu hình trong Program.cs: UseStatusCodePagesWithReExecute)
    public IActionResult Error404() => View();

    // Trang lỗi chung khi môi trường không phải Development
    public IActionResult Error() => View();
}