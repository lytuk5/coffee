using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace do_an_1.Migrations
{
    /// <inheritdoc />
    public partial class MenuItemHotFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHot",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHot",
                table: "MenuItems");
        }
    }
}
