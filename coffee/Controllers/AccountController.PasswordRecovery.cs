using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Cryptography;
using CafeDuaHau.Models;
using CafeDuaHau.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeDuaHau.Controllers;

public partial class AccountController
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult ForgotPassword(string? returnUrl, string? userName, string? token)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (User.Identity?.IsAuthenticated == true)
            return SignedInDestination(returnUrl, User.IsInRole("Admin"));

        return AccountView(nameof(ForgotPassword), new ForgotPasswordForm
        {
            ReturnUrl = returnUrl,
            UserName = userName ?? "",
            ResetToken = token
        });
    }

    [HttpPost]
    [EnableRateLimiting("password-reset-mail")]
    public async Task<IActionResult> SendResetLink(string? userName, string? returnUrl,
        [FromServices] IConfiguration configuration, [FromServices] IDataProtectionProvider protection,
        [FromServices] ILogger<AccountController> logger)
    {
        if (User.Identity?.IsAuthenticated == true)
            return SignedInDestination(returnUrl, User.IsInRole("Admin"));

        var form = new ForgotPasswordForm { UserName = userName ?? "", ReturnUrl = returnUrl };
        ViewData["ResetRequestSent"] = true;
        var normalized = userName?.Trim().ToUpperInvariant();
        var settings = configuration.GetSection("PasswordReset");
        var sender = settings["Sender"];
        var password = settings["AppPassword"];
        if (normalized is { Length: > 0 and <= 64 } &&
            MailAddress.TryCreate(sender, out var from) &&
            !string.IsNullOrWhiteSpace(password) &&
            Uri.TryCreate(settings["BaseUrl"], UriKind.Absolute, out var baseUrl) &&
            baseUrl.Scheme == Uri.UriSchemeHttps)
        {
            var customer = await FindLocalCustomer(normalized);
            var email = settings.GetSection("Recipients")[normalized];
            if (customer is not null && MailAddress.TryCreate(email, out var recipient))
            {
                var token = PasswordRecovery.ForAccount(protection, customer).Protect("reset", TimeSpan.FromMinutes(20));
                var path = Url.Action(nameof(ForgotPassword), "Account", new { userName = customer.UserName, token })!;
                using var message = new MailMessage(from, recipient)
                {
                    Subject = "Đặt lại mật khẩu Dưa Hấu Coffee",
                    Body = $"Mở liên kết sau để đặt lại mật khẩu trong 20 phút:\n{new Uri(baseUrl, path)}\n\nNếu bạn không yêu cầu, hãy bỏ qua email này."
                };
                using var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(from.Address, password.Replace(" ", ""))
                };
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try
                {
                    await smtp.SendMailAsync(message, timeout.Token);
                }
                catch (Exception error) when (error is SmtpException or OperationCanceledException)
                {
                    logger.LogWarning("Không thể gửi email đặt lại mật khẩu qua Gmail. Loại lỗi: {ErrorType}", error.GetType().Name);
                }
            }
        }

        return AccountView(nameof(ForgotPassword), form);
    }

    [HttpPost]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordForm form, [FromServices] IDataProtectionProvider protection)
    {
        if (User.Identity?.IsAuthenticated == true)
            return SignedInDestination(form.ReturnUrl, User.IsInRole("Admin"));

        if (ModelState.IsValid && form.Password.Length < 8)
            ModelState.AddModelError(nameof(form.Password), "Mật khẩu phải có ít nhất 8 ký tự.");
        if (!ModelState.IsValid)
            return AccountView(nameof(ForgotPassword), form);

        var normalized = form.UserName.Trim().ToUpperInvariant();
        var customer = await FindLocalCustomer(normalized);

        if (customer is not null && form.ResetToken is { Length: > 0 and <= 512 } token && ValidRecoveryCode(protection, customer, token))
        {
            var newHash = new PasswordHasher<CustomerAccount>().HashPassword(customer, form.Password);
            var updated = await database.CustomerAccounts
                .Where(account => account.Id == customer.Id && account.PasswordHash == customer.PasswordHash)
                .ExecuteUpdateAsync(change => change.SetProperty(account => account.PasswordHash, newHash));

            if (updated == 1)
            {
                TempData["PasswordResetSuccess"] = "Đã đổi mật khẩu. Bạn có thể đăng nhập bằng mật khẩu mới.";
                return RedirectToAction(nameof(Login), new { returnUrl = form.ReturnUrl });
            }
        }

        ModelState.AddModelError("", "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        return AccountView(nameof(ForgotPassword), form);
    }

    private Task<CustomerAccount?> FindLocalCustomer(string normalized) =>
        database.CustomerAccounts.AsNoTracking().SingleOrDefaultAsync(account =>
            account.NormalizedUserName == normalized && account.ExternalProvider == null && account.PasswordHash != "");

    private static bool ValidRecoveryCode(IDataProtectionProvider protection, CustomerAccount customer, string code)
    {
        try
        {
            return PasswordRecovery.ForAccount(protection, customer).Unprotect(code) == "reset";
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
