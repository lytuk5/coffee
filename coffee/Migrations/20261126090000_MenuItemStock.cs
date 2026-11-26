using CafeDuaHau.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace do_an_1.Migrations;

[DbContext(typeof(CafeDbContext))]
[Migration("20261126090000_MenuItemStock")]
public class MenuItemStock : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("StockQuantity", "MenuItems", nullable: true);
        migrationBuilder.Sql("UPDATE MenuItems SET StockQuantity = 0 WHERE IsSoldOut = 1");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("StockQuantity", "MenuItems");
    }
}
