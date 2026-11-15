using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CafeDuaHau.Data;
using CafeDuaHau.Models;
using CafeDuaHau.Services;
using Microsoft.AspNetCore.Mvc;

namespace CafeDuaHau.Controllers;

public class CartController : Controller
{
    private readonly CartService _cart;
    private readonly OrderRepository _orders;
    private readonly CafeDbContext _database;

    public CartController(CartService cart, OrderRepository orders, CafeDbContext database)
    {
        _cart = cart;
        _orders = orders;
        _database = database;
    }

    // GET /Cart
    public IActionResult Index() => View(_cart.GetItems());

    // POST /Cart/Add
    [HttpPost]
    public IActionResult Add(string itemId, string? size, int quantity, string? note, bool buyNow, string? returnUrl)
    {
        var item = _database.MenuItems.AsNoTracking().SingleOrDefault(item => item.Id == itemId);
        if (item == null || item.IsSoldOut)
        {
            TempData["Error"] = "Món này hiện không thể đặt.";
            return RedirectBack(returnUrl);
        }

        // Xác định giá theo size (nếu món có size)
        var price = item.Price;
        string? sizeLabel = null;
        if (item.HasSizes)
        {
            var chosen = size == null ? item.Sizes[0] : item.Sizes.FirstOrDefault(s => s.Label == size);
            if (chosen == null)
            {
                TempData["Error"] = "Size đã chọn hiện không còn bán.";
                return RedirectBack(returnUrl);
            }
            price = chosen.Price;
            sizeLabel = chosen.Label;
        }

        quantity = Math.Clamp(quantity, 1, 99);

        _cart.Add(new CartItem
        {
            ItemId = item.Id,
            Name = item.Name,
            Price = price,
            ImageUrl = item.ImageUrl,
            Note = note?.Trim() ?? "",
            Size = sizeLabel,
            Quantity = quantity
        });

        if (buyNow) return RedirectToAction(nameof(Checkout));

        var sizeText = sizeLabel != null ? $" ({sizeLabel})" : "";
        TempData["Success"] = $"Đã thêm {quantity} {item.Name}{sizeText} vào giỏ hàng";
        return RedirectBack(returnUrl);
    }

    // POST /Cart/Update
    [HttpPost]
    public IActionResult Update(string key, int delta)
    {
        _cart.ChangeQuantity(key, Math.Clamp(delta, -1, 1));
        return RedirectToAction(nameof(Index));
    }

    // POST /Cart/Remove
    [HttpPost]
    public IActionResult Remove(string key)
    {
        _cart.Remove(key);
        return RedirectToAction(nameof(Index));
    }

    // GET /Cart/Checkout
    [Authorize(Roles = "Customer")]
    [HttpGet]
    public IActionResult Checkout()
    {
        var items = _cart.GetItems();
        if (items.Count == 0) return RedirectToAction(nameof(Index));

        if (!RefreshCart(items, out var pricesChanged))
            return RedirectToAction(nameof(Index));

        if (pricesChanged)
            TempData["Error"] = "Giá món đã thay đổi. Vui lòng kiểm tra tổng tiền trước khi đặt hàng.";

        return View(BuildCheckoutModel(new CheckoutViewModel(), items));
    }

    // POST /Cart/Checkout
    [Authorize(Roles = "Customer")]
    [HttpPost]
    public IActionResult Checkout(CheckoutViewModel model)
    {
        var items = _cart.GetItems();
        if (items.Count == 0)
        {
            TempData["Error"] = "Giỏ hàng của bạn đang trống.";
            return RedirectToAction(nameof(Index));
        }

        if (!RefreshCart(items, out var pricesChanged))
            return RedirectToAction(nameof(Index));

        if (pricesChanged)
            ModelState.AddModelError("", "Giá món đã thay đổi. Vui lòng kiểm tra tổng tiền và xác nhận lại.");

        if (!ModelState.IsValid)
            return View(BuildCheckoutModel(model, items));

        var now = DateTime.Now;
        var order = new Order
        {
            Id = Guid.NewGuid().ToString("N"),
            CustomerAccountId = CustomerIdentity.GetId(User),
            CustomerName = model.RecipientName.Trim(),
            CustomerPhone = model.RecipientPhone.Trim(),
            DeliveryAddress = model.Address.Trim(),
            Items = items.Select(i => new OrderItem
            {
                ItemId = i.ItemId,
                Name = i.Name,
                Price = i.Price,
                Quantity = i.Quantity,
                Note = i.Note,
                Size = i.Size
            }).ToList(),
            TotalAmount = items.Sum(i => i.LineTotal),
            Note = model.Note?.Trim() ?? "",
            PaymentMethod = model.PaymentMethod == "transfer" ? "transfer" : "cash",
            Status = OrderStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        _orders.Add(order);
        _cart.Clear();

        TempData["Success"] = "Đặt hàng thành công!";
        return RedirectToAction("Index", "Orders");
    }

    private bool RefreshCart(List<CartItem> items, out bool pricesChanged)
    {
        pricesChanged = false;
        var ids = items.Select(item => item.ItemId).Distinct().ToList();
        var menu = _database.MenuItems.AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .ToDictionary(item => item.Id);

        foreach (var item in items)
        {
            if (!menu.TryGetValue(item.ItemId, out var current) || current.IsSoldOut)
            {
                TempData["Error"] = $"Món {item.Name} hiện không thể đặt. Vui lòng xóa món khỏi giỏ.";
                return false;
            }

            int? price = current.HasSizes
                ? current.Sizes.FirstOrDefault(size => size.Label == item.Size)?.Price
                : item.Size == null ? current.Price : null;
            if (price == null)
            {
                TempData["Error"] = $"Kích cỡ của món {item.Name} đã thay đổi. Vui lòng chọn lại món.";
                return false;
            }

            pricesChanged |= item.Price != price.Value;
            item.Price = price.Value;
            item.Name = current.Name;
        }

        _cart.Save(items);
        return true;
    }

    private static CheckoutViewModel BuildCheckoutModel(CheckoutViewModel model, List<CartItem> items)
    {
        model.Items = items;
        model.Total = items.Sum(i => i.LineTotal);
        return model;
    }

    private IActionResult RedirectBack(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Menu");
}
