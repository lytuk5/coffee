using System.Globalization;

namespace CafeDuaHau.Helpers;

public static class Money
{
    /// <summary>25000 -> "25.000đ"</summary>
    public static string Format(int value) =>
        value.ToString("N0", CultureInfo.InvariantCulture).Replace(",", ".") + "đ";
}