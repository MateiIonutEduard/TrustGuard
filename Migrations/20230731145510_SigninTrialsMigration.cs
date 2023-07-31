using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrustGuard.Migrations
{
    /// <inheritdoc />
    public partial class SigninTrialsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SigninTrials",
                table: "Account",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SigninTrials",
                table: "Account");
        }
    }
}
