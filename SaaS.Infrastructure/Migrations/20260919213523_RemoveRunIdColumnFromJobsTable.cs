using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRunIdColumnFromJobsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Runs",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_RunId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RunId",
                table: "Jobs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RunId",
                table: "Jobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_RunId",
                table: "Jobs",
                column: "RunId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Runs_RunId",
                table: "Jobs",
                column: "RunId",
                principalTable: "Runs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
