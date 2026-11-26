using CafeDuaHau.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace do_an_1.Migrations;

[DbContext(typeof(CafeDbContext))]
[Migration("20261126110000_MenuCategories")]
public class MenuCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 32, nullable: false),
                Name = table.Column<string>(maxLength: 80, nullable: false, collation: "Latin1_General_100_CI_AS")
            },
            constraints: table => table.PrimaryKey("PK_Categories", category => category.Id));
        migrationBuilder.CreateIndex("IX_Categories_Name", "Categories", "Name", unique: true);
        migrationBuilder.InsertData("Categories", new[] { "Id", "Name" }, new[] { "nvarchar(32)", "nvarchar(80)" }, new object[,]
        {
            { "ca-phe", "Cà phê" },
            { "tra", "Trà & Trà sữa" },
            { "sinh-to", "Sinh tố & Nước ép" },
            { "banh", "Bánh ngọt" }
        });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Categories");
    }
}
