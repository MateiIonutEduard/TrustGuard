using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrustGuard.Migrations
{
    /// <inheritdoc />
    public partial class GuardMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                table: "KeyPair",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ValidateLifetime",
                table: "KeyPair",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRevoked",
                table: "KeyPair");

            migrationBuilder.DropColumn(
                name: "ValidateLifetime",
                table: "KeyPair");
        }
    }
}
