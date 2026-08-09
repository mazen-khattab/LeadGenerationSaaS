using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTheUserSettingsColumnsName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OpenAiApiKeyEncrypted",
                table: "UserSettings",
                newName: "ScraperApiTokenEncrypted");

            migrationBuilder.RenameColumn(
                name: "ApifyApiTokenEncrypted",
                table: "UserSettings",
                newName: "AIApiKeyEncrypted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ScraperApiTokenEncrypted",
                table: "UserSettings",
                newName: "OpenAiApiKeyEncrypted");

            migrationBuilder.RenameColumn(
                name: "AIApiKeyEncrypted",
                table: "UserSettings",
                newName: "ApifyApiTokenEncrypted");
        }
    }
}
