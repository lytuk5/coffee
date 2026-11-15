namespace CafeDuaHau.Models;

public class CartItem
{
    public string ItemId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Price { get; set; }
    public string ImageUrl { get; set; } = "";
    public string Note { get; set; } = "";
    public string? Size { get; set; }
    public int Quantity { get; set; }

    /// <summary>Hai dòng cùng món + cùng size + cùng ghi chú sẽ được gộp làm một.</summary>
    public string Key => $"{ItemId}::{Size}::{Note}";

    public int LineTotal => Price * Quantity;
}