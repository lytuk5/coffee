using CafeDuaHau.Data;
using CafeDuaHau.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CafeDuaHau.Services;

public static class AdminSetup
{
    public const string UserName = "admin";

    public static int Run(CafeDbContext database)
    {
        if (database.AdminAccounts.Any())
        {
            Console.WriteLine("Tài khoản admin đã được thiết lập.");
            return 1;
        }

        if (Console.IsInputRedirected)
        {
            Console.WriteLine("Hãy chạy lệnh trong terminal để nhập mật khẩu.");
            return 1;
        }

        var password = ReadPassword("Mật khẩu admin (ít nhất 12 ký tự): ");
        var confirmation = ReadPassword("Nhập lại mật khẩu: ");
        if (password.Length < 12 || string.IsNullOrWhiteSpace(password) || password != confirmation)
        {
            Console.WriteLine("Mật khẩu chưa đủ dài hoặc hai lần nhập không khớp.");
            return 1;
        }

        var account = new AdminAccount { UserName = UserName };
        account.PasswordHash = new PasswordHasher<AdminAccount>().HashPassword(account, password);

        // Khóa phạm vi giữ việc kiểm tra và tạo tài khoản trong cùng một câu lệnh.
        var inserted = database.Database.ExecuteSqlInterpolated(
            $"INSERT INTO dbo.AdminAccounts (UserName, PasswordHash) SELECT {account.UserName}, {account.PasswordHash} WHERE NOT EXISTS (SELECT 1 FROM dbo.AdminAccounts WITH (UPDLOCK, HOLDLOCK))");
        if (inserted != 1)
        {
            Console.WriteLine("Tài khoản admin đã được thiết lập.");
            return 1;
        }

        Console.WriteLine("Đã thiết lập tài khoản admin. Đăng nhập tại /Account/Login.");
        return 0;
    }

    private static string ReadPassword(string prompt)
    {
        Console.Write(prompt);
        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
                break;
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                password.Length--;
            else if (!char.IsControl(key.KeyChar))
                password.Append(key.KeyChar);
        }
        Console.WriteLine();
        return password.ToString();
    }
}
