using System.Security.Claims;

namespace CafeDuaHau.Services;

public static class CustomerIdentity
{
    public static Guid GetId(ClaimsPrincipal customer) =>
        Guid.ParseExact(customer.FindFirstValue(ClaimTypes.NameIdentifier)!, "N");
}
