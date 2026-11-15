using System.ComponentModel.DataAnnotations;

namespace CafeDuaHau.Models;

public class LoginForm
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(64)]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(256)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterForm : LoginForm
{
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(ExternalPhoneForm.PhonePattern, ErrorMessage = ExternalPhoneForm.PhoneError)]
    public string PhoneNumber { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
    [Compare(nameof(Password), ErrorMessage = "Hai mật khẩu không khớp.")]
    public string ConfirmPassword { get; set; } = "";
}

public class ExternalPhoneForm
{
    public const string PhonePattern = "0[0-9]{9}";
    public const string PhoneError = "Số điện thoại phải có 10 chữ số và bắt đầu bằng 0.";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(PhonePattern, ErrorMessage = PhoneError)]
    public string PhoneNumber { get; set; } = "";
}
