using System.Security.Claims;
using System.Text.RegularExpressions;
using CafeDuaHau.Data;
using CafeDuaHau.Models;
using CafeDuaHau.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CafeDuaHau.Controllers;

public class AccountController(CafeDbContext database, CartService cart, IAuthenticationSchemeProvider schemes) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl, bool externalError = false)
    {
        if (User.Identity?.IsAuthenticated == true)
            return SignedInDestination(returnUrl, User.IsInRole("Admin"));

        if (externalError)
            ModelState.AddModelError("", "Đăng nhập qua Google hoặc Facebook chưa hoàn tất. Vui lòng thử lại.");

        return AccountView(nameof(Login), new LoginForm { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (User.Identity?.IsAuthenticated == true)
            return SignedInDestination(form.ReturnUrl, User.IsInRole("Admin"));

        if (!ModelState.IsValid)
            return AccountView(nameof(Login), form);

        var normalized = form.UserName.Trim().ToUpperInvariant();
        if (normalized == AdminSetup.UserName.ToUpperInvariant())
        {
            var admin = await database.AdminAccounts.AsNoTracking().SingleOrDefaultAsync();
            if (admin is not null && PasswordMatches(admin, admin.PasswordHash, form.Password))
            {
                await SignIn(admin.UserName, "admin", "Admin");
                return SignedInDestination(null, isAdmin: true);
            }
        }
        else
        {
            var customer = await database.CustomerAccounts.AsNoTracking()
                .SingleOrDefaultAsync(account => account.NormalizedUserName == normalized);
            if (customer is not null && customer.PasswordHash.Length > 0 && PasswordMatches(customer, customer.PasswordHash, form.Password))
            {
                await SignIn(customer.UserName, customer.Id.ToString("N"), "Customer");
                return SignedInDestination(form.ReturnUrl, isAdmin: false);
            }
        }

        ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
        return AccountView(nameof(Login), form);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl) =>
        User.Identity?.IsAuthenticated == true
            ? SignedInDestination(returnUrl, User.IsInRole("Admin"))
            : AccountView(nameof(Register), new RegisterForm { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Register(RegisterForm form)
    {
        if (User.Identity?.IsAuthenticated == true)
            return SignedInDestination(form.ReturnUrl, User.IsInRole("Admin"));

        if (!ModelState.IsValid)
            return AccountView(nameof(Register), form);

        var name = form.UserName.Trim();
        var normalized = name.ToUpperInvariant();
        if (!Regex.IsMatch(name, @"\A[A-Za-z0-9_.-]{3,64}\z"))
            ModelState.AddModelError(nameof(form.UserName), "Tên đăng nhập phải có 3–64 ký tự gồm chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang.");
        if (form.Password.Length < 8 || string.IsNullOrWhiteSpace(form.Password))
            ModelState.AddModelError(nameof(form.Password), "Mật khẩu phải có ít nhất 8 ký tự.");
        if (normalized == AdminSetup.UserName.ToUpperInvariant())
            ModelState.AddModelError(nameof(form.UserName), "Tên đăng nhập đã được sử dụng.");
        if (!ModelState.IsValid)
            return AccountView(nameof(Register), form);

        var customer = new CustomerAccount
        {
            UserName = name,
            NormalizedUserName = normalized,
            PhoneNumber = form.PhoneNumber
        };
        customer.PasswordHash = new PasswordHasher<CustomerAccount>().HashPassword(customer, form.Password);
        database.CustomerAccounts.Add(customer);
        try
        {
            await database.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            ModelState.AddModelError(nameof(form.UserName), "Tên đăng nhập đã được sử dụng.");
            return AccountView(nameof(Register), form);
        }

        await SignIn(customer.UserName, customer.Id.ToString("N"), "Customer");
        return SignedInDestination(form.ReturnUrl, isAdmin: false);
    }

    [HttpPost]
    public async Task<IActionResult> ExternalLogin(string? provider, string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
            return SignedInDestination(returnUrl, User.IsInRole("Admin"));

        if (!Request.IsHttps || !ExternalAccounts.Supports(provider) ||
            await schemes.GetSchemeAsync(provider!) is null)
        {
            TempData["Error"] = "Đăng nhập qua nhà cung cấp này chưa khả dụng.";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        await HttpContext.SignOutAsync(ExternalAccounts.Cookie);
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(ExternalCallback))
        };
        properties.Items[ExternalAccounts.ProviderKey] = provider;
        properties.Items[ExternalAccounts.ReturnUrlKey] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        return Challenge(properties, provider!);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalCallback()
    {
        if (User.Identity?.IsAuthenticated == true)
            return SignedInDestination(null, User.IsInRole("Admin"));

        var identity = await ReadExternalIdentity();
        if (identity is null)
            return RedirectToAction(nameof(Login), new { externalError = true });

        var customer = await FindExternalCustomer(identity);
        return customer is null
            ? View(nameof(ExternalPhone), new ExternalPhoneForm())
            : await CompleteExternalLogin(customer, identity);
    }

    [HttpPost]
    public async Task<IActionResult> ExternalPhone(ExternalPhoneForm form)
    {
        if (User.Identity?.IsAuthenticated == true)
            return SignedInDestination(null, User.IsInRole("Admin"));

        var identity = await ReadExternalIdentity();
        if (identity is null)
            return RedirectToAction(nameof(Login), new { externalError = true });

        if (!ModelState.IsValid)
            return AccountView(nameof(ExternalPhone), form);

        var customer = await FindExternalCustomer(identity);
        if (customer is null)
        {
            customer = new CustomerAccount
            {
                UserName = identity.Name,
                NormalizedUserName = $"@{identity.Provider}:{Guid.NewGuid():N}",
                PhoneNumber = form.PhoneNumber,
                ExternalProvider = identity.Provider,
                ExternalSubject = identity.Subject
            };
            database.CustomerAccounts.Add(customer);
            try
            {
                await database.SaveChangesAsync();
            }
            catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
            {
                customer = await FindExternalCustomer(identity);
                if (customer is null)
                    throw;
            }
        }

        return await CompleteExternalLogin(customer, identity);
    }

    private async Task<ExternalIdentity?> ReadExternalIdentity()
    {
        var ticket = await HttpContext.AuthenticateAsync(ExternalAccounts.Cookie);
        var subject = ticket.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!ticket.Succeeded || string.IsNullOrEmpty(subject) || subject.Length > 255 ||
            !ticket.Properties!.Items.TryGetValue(ExternalAccounts.ProviderKey, out var provider) ||
            !ExternalAccounts.Supports(provider))
            return null;

        var name = ticket.Principal?.FindFirstValue(ClaimTypes.Name)?.Trim();
        name = string.IsNullOrWhiteSpace(name) ? $"Khách {provider}" : name;
        ticket.Properties.Items.TryGetValue(ExternalAccounts.ReturnUrlKey, out var returnUrl);
        return new ExternalIdentity(provider!, subject, name[..Math.Min(64, name.Length)], returnUrl);
    }

    private Task<CustomerAccount?> FindExternalCustomer(ExternalIdentity identity) =>
        database.CustomerAccounts.AsNoTracking().SingleOrDefaultAsync(customer =>
            customer.ExternalProvider == identity.Provider && customer.ExternalSubject == identity.Subject);

    private async Task<IActionResult> CompleteExternalLogin(CustomerAccount customer, ExternalIdentity identity)
    {
        await HttpContext.SignOutAsync(ExternalAccounts.Cookie);
        await SignIn(customer.UserName, customer.Id.ToString("N"), "Customer");
        return SignedInDestination(identity.ReturnUrl, isAdmin: false);
    }

    private sealed record ExternalIdentity(string Provider, string Subject, string Name, string? ReturnUrl);

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        cart.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Menu");
    }

    [HttpGet]
    public IActionResult Denied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    private IActionResult AccountView(string name, object form) =>
        Request.Headers["X-Account-Dialog"] == "1"
            ? PartialView($"_{name}Form", form)
            : View(name, form);

    private static bool PasswordMatches<TAccount>(TAccount account, string hash, string password)
        where TAccount : class =>
        new PasswordHasher<TAccount>().VerifyHashedPassword(account, hash, password)
            != PasswordVerificationResult.Failed;

    private Task SignIn(string userName, string accountId, string role)
    {
        var scheme = CookieAuthenticationDefaults.AuthenticationScheme;
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, userName), new Claim(ClaimTypes.NameIdentifier, accountId),
             new Claim(ClaimTypes.Role, role)], scheme);
        return HttpContext.SignInAsync(scheme, new ClaimsPrincipal(identity));
    }

    private IActionResult SignedInDestination(string? returnUrl, bool isAdmin)
    {
        if (isAdmin)
            return RedirectToAction("Index", "Admin");

        return Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl!)
            : RedirectToAction("Index", "Menu");
    }
}
