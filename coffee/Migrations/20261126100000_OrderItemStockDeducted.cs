using CafeDuaHau.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace do_an_1.Migrations;

[DbContext(typeof(CafeDbContext))]
[Migration("20261126100000_OrderItemStockDeducted")]
public class OrderItemStockDeducted : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>("StockDeducted", "OrderItems", nullable: false, defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("StockDeducted", "OrderItems");
    }
}
