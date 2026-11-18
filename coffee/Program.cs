using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using CafeDuaHau.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using CafeDuaHau.Data;
using Microsoft.EntityFrameworkCore;
using CafeDuaHau.Services;
using Microsoft.AspNetCore.Mvc;

var setupAdmin = args is ["--setup-admin"];
var builder = WebApplication.CreateBuilder(setupAdmin ? [] : args);
var authentication = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Denied";
        options.Cookie.Name = ".DuaHau.Account";
    });
authentication.AddExternalAccounts(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddSingleton<PasswordResetRequests>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("password-reset", context =>
        RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5)
        }));
});
builder.Services.AddSignalR();

builder.Services.AddControllersWithViews(options =>
{
    // Tự động kiểm tra anti-forgery token cho mọi request POST
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddHttpContextAccessor();

// Giỏ hàng lưu trong Session (bộ nhớ của máy chủ)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".DuaHau.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services.AddScoped<CartService>();
builder.Services.AddDbContext<CafeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CafeDatabase")));
builder.Services.AddScoped<OrderRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
    database.Database.Migrate();
    if (setupAdmin)
    {
        Environment.ExitCode = AdminSetup.Run(database);
        return;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStatusCodePagesWithReExecute("/Home/Error404");

app.UseStaticFiles(new StaticFileOptions
{
    // Tắt cache khi phát triển để luôn thấy bản CSS/JS/ảnh mới nhất
    OnPrepareResponse = ctx =>
    {
        if (app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        }
    }
});

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/cafe-chat") &&
        context.Request.Headers.Origin.Count > 0 &&
        !string.Equals(context.Request.Headers.Origin.ToString(),
            $"{context.Request.Scheme}://{context.Request.Host}", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    await next(context);
});

app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<CafeChatHub>("/cafe-chat", options => options.CloseOnAuthenticationExpiration = true);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Menu}/{action=Index}/{id?}");

app.Run();
