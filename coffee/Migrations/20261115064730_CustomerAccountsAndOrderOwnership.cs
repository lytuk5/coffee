using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace do_an_1.Migrations
{
    /// <inheritdoc />
    public partial class CustomerAccountsAndOrderOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerKey_CreatedAt",
                table: "Orders");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerAccountId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerAccountId_CreatedAt",
                table: "Orders",
                columns: new[] { "CustomerAccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_NormalizedUserName",
                table: "CustomerAccounts",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CustomerAccounts_CustomerAccountId",
                table: "Orders",
                column: "CustomerAccountId",
                principalTable: "CustomerAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CustomerAccounts_CustomerAccountId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "CustomerAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerAccountId_CreatedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerAccountId",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerKey_CreatedAt",
                table: "Orders",
                columns: new[] { "CustomerKey", "CreatedAt" });
        }
    }
}
