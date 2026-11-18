using CafeDuaHau.Models;
using Microsoft.AspNetCore.DataProtection;

namespace CafeDuaHau.Services;

public static class PasswordRecovery
{
    public static ITimeLimitedDataProtector ForAccount(IDataProtectionProvider protection, CustomerAccount customer) =>
        protection.CreateProtector("CustomerPasswordReset.Gmail.v1", customer.Id.ToString("N"), customer.PasswordHash)
            .ToTimeLimitedDataProtector();
}
