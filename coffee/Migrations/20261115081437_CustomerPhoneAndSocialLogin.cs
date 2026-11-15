using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace do_an_1.Migrations
{
    /// <inheritdoc />
    public partial class CustomerPhoneAndSocialLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalProvider",
                table: "CustomerAccounts",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSubject",
                table: "CustomerAccounts",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "CustomerAccounts",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_ExternalProvider_ExternalSubject",
                table: "CustomerAccounts",
                columns: new[] { "ExternalProvider", "ExternalSubject" },
                unique: true,
                filter: "[ExternalProvider] IS NOT NULL AND [ExternalSubject] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerAccounts_ExternalProvider_ExternalSubject",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "ExternalProvider",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "ExternalSubject",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "CustomerAccounts");
        }
    }
}
