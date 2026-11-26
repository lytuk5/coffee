namespace CafeDuaHau.Models;

public class MenuSize
{
    public string Label { get; set; } = "";
    public int Price { get; set; }
}

public class MenuItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string CategoryId { get; set; } = "";

    /// <summary>Giá (VNĐ) khi món không có size.</summary>
    public int Price { get; set; }

    /// <summary>Danh sách size. Nếu có thì dùng giá theo size.</summary>
    public List<MenuSize> Sizes { get; set; } = new();

    public string ImageUrl { get; set; } = "";
    public bool IsSoldOut { get; set; }
    public int? StockQuantity { get; set; }
    public bool IsHot { get; set; }
    public int? DiscountPercent { get; set; }

    public int SalePrice(int regularPrice) =>
        (int)Math.Round(regularPrice * (100m - (DiscountPercent ?? 0)) / 100m, MidpointRounding.AwayFromZero);

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool HasSizes => Sizes.Count > 0;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int BasePrice => HasSizes ? (Sizes.FirstOrDefault(s => s.Label == "M")?.Price ?? Sizes[0].Price) : Price;
}
