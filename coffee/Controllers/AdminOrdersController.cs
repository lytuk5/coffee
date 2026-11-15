using CafeDuaHau.Models;
using CafeDuaHau.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeDuaHau.Controllers;

[Authorize(Roles = "Admin")]
public class AdminOrdersController(OrderRepository orders) : Controller
{
    public IActionResult Index(string? q)
    {
        ViewData["Query"] = q?.Trim();
        return View(orders.GetAll(q));
    }

    [HttpPost]
    public IActionResult SetStatus(string id, OrderStatus? status, string? q)
    {
        if (!ModelState.IsValid || !status.HasValue || !orders.SetStatus(id, status.Value))
            TempData["Error"] = "Đơn hàng này không thể đổi trạng thái.";
        else
            TempData["Success"] = status.Value.Label();

        return RedirectToAction(nameof(Index), new { q });
    }
}
