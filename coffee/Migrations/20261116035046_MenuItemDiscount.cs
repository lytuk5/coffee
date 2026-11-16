using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace do_an_1.Migrations
{
    /// <inheritdoc />
    public partial class MenuItemDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscountPercent",
                table: "MenuItems",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "MenuItems");
        }
    }
}
