using System.ComponentModel.DataAnnotations;

namespace CafeDuaHau.Models;

public class MenuReview
{
    public string MenuItemId { get; set; } = "";
    public Guid CustomerAccountId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class MenuReviewForm
{
    [Range(1, 5, ErrorMessage = "Vui lòng chọn từ 1 đến 5 sao.")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nhận xét.")]
    [StringLength(1000, ErrorMessage = "Nhận xét tối đa 1.000 ký tự.")]
    public string Comment { get; set; } = "";
}

public record MenuReviewEntry(string UserName, int Rating, string Comment, DateTime CreatedAt);

public record MenuDetailsViewModel(
    MenuItem Item, List<string> Photos, List<MenuReviewEntry> Reviews, bool CanReview);
