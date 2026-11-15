using CafeDuaHau.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeDuaHau.Controllers;

[Authorize(Roles = "Customer")]
public class OrdersController(OrderRepository orders) : Controller
{
    public IActionResult Index() => View(orders.GetByCustomer(CustomerIdentity.GetId(User)));

    [HttpPost]
    public IActionResult Cancel(string id)
    {
        if (orders.Cancel(id, CustomerIdentity.GetId(User)))
            TempData["Success"] = "Đã hủy đơn hàng thành công.";
        else
            TempData["Error"] = "Không thể hủy đơn hàng này.";

        return RedirectToAction(nameof(Index));
    }
}
