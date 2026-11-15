using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace do_an_1.Migrations
{
    /// <inheritdoc />
    public partial class MenuAndAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoryId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSoldOut = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuSizes",
                columns: table => new
                {
                    MenuItemId = table.Column<string>(type: "nvarchar(32)", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuSizes", x => new { x.MenuItemId, x.Id });
                    table.ForeignKey(
                        name: "FK_MenuSizes_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "CategoryId", "Description", "ImageUrl", "IsSoldOut", "Name", "Price" },
                values: new object[,]
                {
                    { "bac-xiu", "ca-phe", "Nhiều sữa, ít cà phê, vị nhẹ dễ uống.", "/images/menu/bac-xiu.jpg", false, "Bạc xỉu", 0 },
                    { "banh-croissant", "banh", "Bánh sừng bò bơ giòn xốp.", "/images/menu/banh-croissant.jpg", true, "Bánh croissant", 35000 },
                    { "ca-phe-den", "ca-phe", "Cà phê phin truyền thống, đậm đà.", "/images/menu/ca-phe-den.jpg", false, "Cà phê đen", 0 },
                    { "ca-phe-sua", "ca-phe", "Cà phê phin pha sữa đặc béo ngọt.", "/images/menu/ca-phe-sua.jpg", false, "Cà phê sữa", 0 },
                    { "cappuccino", "ca-phe", "Espresso với lớp bọt sữa mịn.", "/images/menu/cappuccino.jpg", false, "Cappuccino", 45000 },
                    { "nuoc-ep-cam", "sinh-to", "Cam tươi vắt nguyên chất.", "/images/menu/nuoc-ep-cam.jpg", false, "Nước ép cam", 40000 },
                    { "sinh-to-bo", "sinh-to", "Bơ sáp xay cùng sữa, béo mịn.", "/images/menu/sinh-to-bo.webp", false, "Sinh tố bơ", 45000 },
                    { "sinh-to-dua-hau", "sinh-to", "Dưa hấu xay mịn, mát lạnh giải nhiệt.", "/images/menu/sinh-to-dua-hau.jpg", false, "Sinh tố dưa hấu", 0 },
                    { "tiramisu", "banh", "Bánh tiramisu vị cà phê và cacao.", "/images/menu/tiramisu.jpg", false, "Tiramisu", 45000 },
                    { "tra-dao-cam-sa", "tra", "Trà đào thanh mát, thêm cam và sả thơm.", "/images/menu/tra-dao-cam-sa.jpg", false, "Trà đào cam sả", 0 },
                    { "tra-sua-tran-chau", "tra", "Trà sữa truyền thống với trân châu dai giòn.", "/images/menu/tra-sua-tran-chau.jpg", false, "Trà sữa trân châu", 0 },
                    { "tra-vai", "tra", "Trà xanh kết hợp vải thiều ngọt thanh.", "/images/menu/tra-vai.jpeg", false, "Trà vải", 38000 }
                });

            migrationBuilder.InsertData(
                table: "MenuSizes",
                columns: new[] { "Id", "MenuItemId", "Label", "Price" },
                values: new object[,]
                {
                    { 1, "bac-xiu", "M", 29000 },
                    { 2, "bac-xiu", "L", 35000 },
                    { 1, "ca-phe-den", "M", 25000 },
                    { 2, "ca-phe-den", "L", 30000 },
                    { 1, "ca-phe-sua", "M", 29000 },
                    { 2, "ca-phe-sua", "L", 35000 },
                    { 1, "sinh-to-dua-hau", "M", 35000 },
                    { 2, "sinh-to-dua-hau", "L", 42000 },
                    { 1, "tra-dao-cam-sa", "M", 39000 },
                    { 2, "tra-dao-cam-sa", "L", 45000 },
                    { 1, "tra-sua-tran-chau", "M", 35000 },
                    { 2, "tra-sua-tran-chau", "L", 42000 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAccounts");

            migrationBuilder.DropTable(
                name: "MenuSizes");

            migrationBuilder.DropTable(
                name: "MenuItems");
        }
    }
}
