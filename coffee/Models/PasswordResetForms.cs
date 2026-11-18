using System.ComponentModel.DataAnnotations;

namespace CafeDuaHau.Models;

public sealed class ForgotPasswordForm
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(64)]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(ExternalPhoneForm.PhonePattern, ErrorMessage = ExternalPhoneForm.PhoneError)]
    public string PhoneNumber { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public sealed class ResetPasswordForm
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(256, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có từ 8 đến 256 ký tự.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới.")]
    [StringLength(256)]
    [Compare(nameof(Password), ErrorMessage = "Hai mật khẩu không khớp.")]
    public string ConfirmPassword { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
