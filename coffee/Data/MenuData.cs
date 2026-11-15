using CafeDuaHau.Models;

namespace CafeDuaHau.Data;

/// <summary>Dữ liệu mẫu dùng để khởi tạo thực đơn trong database.</summary>
public static class MenuData
{
    public const string PlaceholderImage = "/images/menu/placeholder.svg";

    private static string Img(string fileName) => "/images/menu/" + fileName;

    public static readonly List<Category> Categories = new()
    {
        new Category { Id = "ca-phe", Name = "Cà phê" },
        new Category { Id = "tra", Name = "Trà & Trà sữa" },
        new Category { Id = "sinh-to", Name = "Sinh tố & Nước ép" },
        new Category { Id = "banh", Name = "Bánh ngọt" },
    };

    public static readonly List<MenuItem> Items = new()
    {
        new MenuItem
        {
            Id = "ca-phe-den", Name = "Cà phê đen",
            Description = "Cà phê phin truyền thống, đậm đà.",
            CategoryId = "ca-phe",
            Sizes = { new MenuSize { Label = "M", Price = 25000 }, new MenuSize { Label = "L", Price = 30000 } },
            ImageUrl = Img("ca-phe-den.jpg"),
            IsSoldOut = false
        },
        new MenuItem
        {
            Id = "ca-phe-sua", Name = "Cà phê sữa",
            Description = "Cà phê phin pha sữa đặc béo ngọt.",
            CategoryId = "ca-phe",
            Sizes = { new MenuSize { Label = "M", Price = 29000 }, new MenuSize { Label = "L", Price = 35000 } },
            ImageUrl = Img("ca-phe-sua.jpg"),
            IsSoldOut = false
        },
        new MenuItem
        {
            Id = "bac-xiu", Name = "Bạc xỉu",
            Description = "Nhiều sữa, ít cà phê, vị nhẹ dễ uống.",
            CategoryId = "ca-phe",
            Sizes = { new MenuSize { Label = "M", Price = 29000 }, new MenuSize { Label = "L", Price = 35000 } },
            ImageUrl = Img("bac-xiu.jpg"),
            IsSoldOut = false
        },
        new MenuItem
        {
            Id = "cappuccino", Name = "Cappuccino",
            Description = "Espresso với lớp bọt sữa mịn.",
            CategoryId = "ca-phe",
            Price = 45000,
            ImageUrl = Img("cappuccino.jpg"),
            IsSoldOut = false
        },
        new MenuItem
        {
            Id = "tra-dao-cam-sa", Name = "Trà đào cam sả",
            Description = "Trà đào thanh mát, thêm cam và sả thơm.",
            CategoryId = "tra",
            Sizes = { new MenuSize { Label = "M", Price = 39000 }, new MenuSize { Label = "L", Price = 45000 } },
            ImageUrl = Img("tra-dao-cam-sa.jpg"),
            IsSoldOut = false
        },
        new MenuItem
        {
            Id = "tra-sua-tran-chau", Name = "Trà sữa trân châu",
            Description = "Trà sữa truyền thống với trân châu dai giòn.",
            CategoryId = "tra",
            Sizes = { new MenuSize { Label = "M", Price = 35000 }, new MenuSize { Label = "L", Price = 42000 } },
            ImageUrl = Img("tra-sua-tran-chau.jpg"),
            IsSoldOut = false
        },
        new MenuItem
        {
            Id = "tra-vai", Name = "Trà vải",
            Description = "Trà xanh kết hợp vải thiều ngọt thanh.",
            CategoryId = "tra",
            Price = 38000,
            ImageUrl = Img("tra-vai.jpeg"),
            IsSoldOut = false
        },
        new MenuItem
        {
            Id = "sinh-to-dua-hau", Name = "Sinh tố dưa hấu",
            Description = "Dưa hấu xay mịn, mát lạnh giải nhiệt.",
            CategoryId = "sinh-to",
            Sizes = { new MenuSize { Label = "M", Price = 35000 }, new MenuSize { Label = "L", Price = 42000 } },
            ImageUrl = Img("sinh-to-dua-hau.jpg"),
            IsSoldOut = false
        },
        new MenuItem
        {
            Id = "nuoc-ep-cam", Name = "Nước ép cam",
            Description = "Cam tươi vắt nguyên chất.",
            CategoryId = "sinh-to",
            Price = 40000,
            ImageUrl = Img("nuoc-ep-cam.jpg"),
            IsSoldOut = false
        },
        new MenuItem
        {
            Id = "sinh-to-bo", Name = "Sinh tố bơ",
            Description = "Bơ sáp xay cùng sữa, béo mịn.",
            CategoryId = "sinh-to",
            Price = 45000,
            ImageUrl = Img("sinh-to-bo.webp"),
            IsSoldOut = false
        },
        new MenuItem
        {
            Id = "tiramisu", Name = "Tiramisu",
            Description = "Bánh tiramisu vị cà phê và cacao.",
            CategoryId = "banh",
            Price = 45000,
            ImageUrl = Img("tiramisu.jpg"),
            IsSoldOut = false
        },
        new MenuItem
        {
            Id = "banh-croissant", Name = "Bánh croissant",
            Description = "Bánh sừng bò bơ giòn xốp.",
            CategoryId = "banh",
            Price = 35000,
            ImageUrl = Img("banh-croissant.jpg"),
            IsSoldOut = true
        },
    };

}
