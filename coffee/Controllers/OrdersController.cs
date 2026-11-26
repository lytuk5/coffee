using CafeDuaHau.Data;
using CafeDuaHau.Models;
using CafeDuaHau.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeDuaHau.Controllers;

[Authorize(Roles = "Customer")]
public class OrdersController(OrderRepository orders, CafeDbContext database, CartService cart) : Controller
{
    public IActionResult Index()
    {
        var customerId = CustomerIdentity.GetId(User);
        var history = orders.GetByCustomer(customerId);
        var deliveredIds = history.Where(order => order.Status == OrderStatus.Delivered)
            .SelectMany(order => order.Items).Select(item => item.ItemId).Distinct().ToList();
        ViewData["ReviewableItems"] = database.MenuItems.AsNoTracking()
            .Where(item => deliveredIds.Contains(item.Id)).Select(item => item.Id).ToHashSet();
        ViewData["OwnReviews"] = database.MenuReviews.AsNoTracking()
            .Where(review => review.CustomerAccountId == customerId && deliveredIds.Contains(review.MenuItemId))
            .ToDictionary(review => review.MenuItemId);
        return View(history);
    }

    [HttpPost]
    public IActionResult Reorder(string id)
    {
        var customerId = CustomerIdentity.GetId(User);
        var order = database.Orders.AsNoTracking()
            .SingleOrDefault(order => order.Id == id && order.CustomerAccountId == customerId);
        if (order == null)
            return NotFound();

        var ids = order.Items.Select(line => line.ItemId).Distinct().ToList();
        var menu = database.MenuItems.AsNoTracking().Where(item => ids.Contains(item.Id))
            .ToDictionary(item => item.Id);
        var cartItems = cart.GetItems();
        var added = 0;
        var unavailable = new HashSet<string>();
        foreach (var line in order.Items)
        {
            if (!menu.TryGetValue(line.ItemId, out var item) || item.IsSoldOut)
            {
                unavailable.Add(line.Name);
                continue;
            }

            int? price = item.HasSizes
                ? item.Sizes.Find(size => size.Label == line.Size)?.Price
                : line.Size == null ? item.Price : null;
            var newLine = new CartItem
            {
                ItemId = item.Id,
                Name = item.Name,
                Price = item.SalePrice(price.GetValueOrDefault()),
                Size = line.Size,
                Note = line.Note,
                ImageUrl = item.ImageUrl
            };
            var existing = cartItems.Find(entry => entry.Key == newLine.Key);
            var quantityInCart = cartItems.Where(entry => entry.ItemId == item.Id).Sum(entry => entry.Quantity);
            var stockRoom = item.StockQuantity.HasValue ? item.StockQuantity.Value - quantityInCart : line.Quantity;
            var quantity = Math.Min(line.Quantity, Math.Min(stockRoom, 99 - (existing?.Quantity ?? 0)));
            if (!price.HasValue || quantity < line.Quantity)
                unavailable.Add(line.Name);
            if (!price.HasValue || quantity <= 0)
                continue;

            if (existing == null)
            {
                newLine.Quantity = quantity;
                cartItems.Add(newLine);
            }
            else
            {
                existing.Quantity += quantity;
                existing.Name = newLine.Name;
                existing.Price = newLine.Price;
                existing.ImageUrl = newLine.ImageUrl;
            }
            added += quantity;
        }

        if (unavailable.Count > 0)
            TempData["Error"] = $"Không thể thêm đủ số lượng đã đặt cho: {string.Join(", ", unavailable)}.";
        if (added == 0)
        {
            TempData["Error"] ??= "Đơn này không có món khả dụng để đặt lại.";
            return RedirectToAction(nameof(Index));
        }

        cart.Save(cartItems);
        TempData["Success"] = $"Đã thêm {added} phần vào giỏ hàng theo giá hiện tại.";
        return RedirectToAction("Index", "Cart");
    }

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
