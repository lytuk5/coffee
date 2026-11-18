using CafeDuaHau.Models;
using CafeDuaHau.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CafeDuaHau.Controllers;

public partial class AccountController
{
    private const string ResetSessionKey = "CustomerPasswordReset";

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult ForgotPassword(string? userName, string? returnUrl, [FromServices] PasswordResetRequests requests)
    {
        if (User.Identity?.IsAuthenticated == true)
            return SignedInDestination(returnUrl, User.IsInRole("Admin"));

        var request = CurrentReset(requests);
        if (request?.Approved == true)
            return AccountView(nameof(ResetPassword), new ResetPasswordForm { ReturnUrl = returnUrl });

        ViewData["ResetReference"] = GetResetId() is var id && id != Guid.Empty ? id.ToString("N")[..8] : null;
        return AccountView(nameof(ForgotPassword), new ForgotPasswordForm { UserName = userName ?? "", ReturnUrl = returnUrl });
    }

    [HttpPost]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordForm form, [FromServices] PasswordResetRequests requests)
    {
        if (User.Identity?.IsAuthenticated == true)
            return SignedInDestination(form.ReturnUrl, User.IsInRole("Admin"));
        if (!ModelState.IsValid)
            return AccountView(nameof(ForgotPassword), form);

        var previousId = GetResetId();
        if (previousId != Guid.Empty)
            requests.Remove(previousId);

        var id = Guid.NewGuid();
        var normalized = form.UserName.Trim().ToUpperInvariant();
        var customer = await database.CustomerAccounts.AsNoTracking().SingleOrDefaultAsync(account =>
            account.NormalizedUserName == normalized && account.PhoneNumber == form.PhoneNumber &&
            account.ExternalProvider == null && account.PasswordHash != "");

        if (customer is not null)
            requests.Add(new(id, customer.Id, customer.UserName, form.PhoneNumber, customer.PasswordHash));

        HttpContext.Session.SetString(ResetSessionKey, id.ToString());
        return RedirectToAction(nameof(ForgotPassword), new { returnUrl = form.ReturnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordForm form, [FromServices] PasswordResetRequests requests)
    {
        if (User.Identity?.IsAuthenticated == true)
            return SignedInDestination(form.ReturnUrl, User.IsInRole("Admin"));

        if (CurrentReset(requests) is not { Approved: true } request)
            return RedirectToAction(nameof(ForgotPassword), new { returnUrl = form.ReturnUrl });

        if (!ModelState.IsValid)
            return AccountView(nameof(ResetPassword), form);

        var passwordHash = new PasswordHasher<CustomerAccount>().HashPassword(new CustomerAccount { Id = request.CustomerId }, form.Password);
        var updated = await database.CustomerAccounts
            .Where(account => account.Id == request.CustomerId && account.PasswordHash == request.PasswordHash &&
                account.PhoneNumber == request.PhoneNumber && account.ExternalProvider == null)
            .ExecuteUpdateAsync(change => change.SetProperty(account => account.PasswordHash, passwordHash));

        requests.Remove(request.Id);
        HttpContext.Session.Remove(ResetSessionKey);
        if (updated != 1)
            return RedirectToAction(nameof(ForgotPassword), new { returnUrl = form.ReturnUrl });

        TempData["PasswordResetSuccess"] = "Đã đổi mật khẩu. Hãy đăng nhập bằng mật khẩu mới.";
        return RedirectToAction(nameof(Login), new { returnUrl = form.ReturnUrl });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult PasswordRequests([FromServices] PasswordResetRequests requests) => View(requests.Pending());

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public IActionResult ApprovePasswordRequest(Guid id, [FromServices] PasswordResetRequests requests)
    {
        requests.Approve(id);
        return RedirectToAction(nameof(PasswordRequests));
    }

    private Guid GetResetId() =>
        Guid.TryParse(HttpContext.Session.GetString(ResetSessionKey), out var id) ? id : Guid.Empty;

    private PasswordResetRequests.Request? CurrentReset(PasswordResetRequests requests) => requests.Get(GetResetId());
}
