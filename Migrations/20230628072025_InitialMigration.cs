using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TrustGuard.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Avatar = table.Column<string>(type: "text", nullable: true),
                    SecurityCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Domain",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    webcode = table.Column<string>(type: "text", nullable: false),
                    a = table.Column<string>(type: "text", nullable: false),
                    b = table.Column<string>(type: "text", nullable: false),
                    p = table.Column<string>(type: "text", nullable: false),
                    N = table.Column<string>(type: "text", nullable: false),
                    x = table.Column<string>(type: "text", nullable: false),
                    y = table.Column<string>(type: "text", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Domain", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Application",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppName = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    ClientSecret = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: true),
                    DomainId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    AppLogo = table.Column<string>(type: "text", nullable: true),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    AppType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Application", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Application_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Application_Domain_DomainId",
                        column: x => x.DomainId,
                        principalTable: "Domain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BasePoint",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    x = table.Column<string>(type: "text", nullable: false),
                    y = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: true),
                    ApplicationId = table.Column<int>(type: "integer", nullable: false),
                    DomainId = table.Column<int>(type: "integer", nullable: false),
                    IsSuspicious = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasePoint", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BasePoint_Application_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Application",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BasePoint_Domain_DomainId",
                        column: x => x.DomainId,
                        principalTable: "Domain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Application_AccountId",
                table: "Application",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Application_DomainId",
                table: "Application",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_BasePoint_ApplicationId",
                table: "BasePoint",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_BasePoint_DomainId",
                table: "BasePoint",
                column: "DomainId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BasePoint");

            migrationBuilder.DropTable(
                name: "Application");

            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "Domain");
        }
    }
}
