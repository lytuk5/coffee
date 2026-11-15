using System.Text.Json;
using CafeDuaHau.Models;

namespace CafeDuaHau.Services;

/// <summary>Giỏ hàng lưu trong Session của người dùng.</summary>
public class CartService
{
    private const string SessionKey = "cart";
    private readonly IHttpContextAccessor _http;

    public CartService(IHttpContextAccessor http) => _http = http;

    private ISession Session => _http.HttpContext!.Session;

    public List<CartItem> GetItems()
    {
        var json = Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json)) return new List<CartItem>();
        try
        {
            return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }
        catch (JsonException)
        {
            return new List<CartItem>();
        }
    }

    public void Save(List<CartItem> items) =>
        Session.SetString(SessionKey, JsonSerializer.Serialize(items));

    public int Count => GetItems().Sum(i => i.Quantity);
    public int Total => GetItems().Sum(i => i.LineTotal);

    public void Add(CartItem newItem)
    {
        var items = GetItems();
        var existing = items.FirstOrDefault(i => i.Key == newItem.Key);
        if (existing != null)
        {
            existing.Name = newItem.Name;
            existing.Price = newItem.Price;
            existing.Quantity = Math.Min(99, existing.Quantity + newItem.Quantity);
        }
        else items.Add(newItem);
        Save(items);
    }

    public void ChangeQuantity(string key, int delta)
    {
        var items = GetItems();
        var target = items.FirstOrDefault(i => i.Key == key);
        if (target == null) return;

        target.Quantity = Math.Min(99, target.Quantity + delta);
        if (target.Quantity <= 0) items.Remove(target);
        Save(items);
    }

    public void Remove(string key)
    {
        var items = GetItems();
        items.RemoveAll(i => i.Key == key);
        Save(items);
    }

    public void Clear() => Session.Remove(SessionKey);
}
