using System.ComponentModel.DataAnnotations;

namespace CafeDuaHau.Models;

public class AdminMenuForm
{
    [Required(ErrorMessage = "Vui lòng nhập tên món.")]
    [StringLength(120)]
    public string Name { get; set; } = "";

    [StringLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    public string CategoryId { get; set; } = "";

    [Range(1, 1000000, ErrorMessage = "Giá phải từ 1 đến 1.000.000 đ.")]
    public int? Price { get; set; }

    [Range(1, 1000000, ErrorMessage = "Giá size M phải từ 1 đến 1.000.000 đ.")]
    public int? MediumPrice { get; set; }

    [Range(1, 1000000, ErrorMessage = "Giá size L phải từ 1 đến 1.000.000 đ.")]
    public int? LargePrice { get; set; }

    [Range(1, 1000000, ErrorMessage = "Giá size XL phải từ 1 đến 1.000.000 đ.")]
    public int? ExtraLargePrice { get; set; }

    public List<IFormFile> Photos { get; set; } = [];

}

public record AdminMenuPhoto(Guid? Id, string Url, bool IsCover);
