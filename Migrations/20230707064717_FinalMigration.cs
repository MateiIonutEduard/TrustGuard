using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrustGuard.Migrations
{
    /// <inheritdoc />
    public partial class FinalMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SolvedAt",
                table: "Demand",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalSeconds",
                table: "Demand",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SolvedAt",
                table: "Demand");

            migrationBuilder.DropColumn(
                name: "TotalSeconds",
                table: "Demand");
        }
    }
}
