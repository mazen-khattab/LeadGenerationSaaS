using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeysExpirationDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AIApiKeyExpirationDate",
                table: "UserSettings",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "DATEADD(day, 7, GETUTCDATE())");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScraperApiTokenExpirationDate",
                table: "UserSettings",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "DATEADD(day, 7, GETUTCDATE())");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AIApiKeyExpirationDate",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "ScraperApiTokenExpirationDate",
                table: "UserSettings");
        }
    }
}
