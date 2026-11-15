using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CafeDuaHau.Models;

public class MenuIndexViewModel
{
    public List<Category> Categories { get; set; } = new();
    public List<MenuItem> Items { get; set; } = new();
    public string ActiveCategory { get; set; } = "all";
    public string Keyword { get; set; } = "";
}

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
    [Display(Name = "Tên người nhận")]
    public string RecipientName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ (VD: 0912345678).")]
    [Display(Name = "Số điện thoại")]
    public string RecipientPhone { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")]
    [Display(Name = "Địa chỉ nhận hàng")]
    public string Address { get; set; } = "";

    [Display(Name = "Ghi chú cho đơn hàng")]
    public string? Note { get; set; }

    [Display(Name = "Phương thức thanh toán")]
    public string PaymentMethod { get; set; } = "cash";

    // Chỉ để hiển thị lại, không nhận từ form
    [BindNever] public List<CartItem> Items { get; set; } = new();
    [BindNever] public int Total { get; set; }
}