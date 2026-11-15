using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace CafeDuaHau.Services;

public static class ExternalAccounts
{
    public const string Cookie = "ExternalCustomer";
    public const string Google = "Google";
    public const string Facebook = "Facebook";
    public const string ProviderKey = "cafe-provider";
    public const string ReturnUrlKey = "cafe-return-url";

    public static bool Supports(string? provider) => provider is Google or Facebook;

    public static void AddExternalAccounts(this AuthenticationBuilder authentication, IConfiguration configuration)
    {
        authentication.AddCookie(Cookie, options =>
        {
            options.Cookie.Name = ".DuaHau.ExternalCustomer";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            options.SlidingExpiration = false;
        });

        var google = configuration.GetSection("Authentication:Google");
        if (HasKeys(google["ClientId"], google["ClientSecret"]))
        {
            authentication.AddGoogle(options =>
            {
                options.ClientId = google["ClientId"]!;
                options.ClientSecret = google["ClientSecret"]!;
                options.Scope.Remove("email");
                Configure(options);
            });
        }

        var facebook = configuration.GetSection("Authentication:Facebook");
        if (HasKeys(facebook["AppId"], facebook["AppSecret"]))
        {
            authentication.AddFacebook(options =>
            {
                options.AppId = facebook["AppId"]!;
                options.AppSecret = facebook["AppSecret"]!;
                options.Scope.Remove("email");
                options.Fields.Remove("email");
                Configure(options);
            });
        }
    }

    private static bool HasKeys(string? publicKey, string? secret) =>
        !string.IsNullOrWhiteSpace(publicKey) && !string.IsNullOrWhiteSpace(secret);

    private static void Configure(OAuthOptions options)
    {
        options.SignInScheme = Cookie;
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect(context.Request.PathBase + "/Account/Login?externalError=true");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    }
}
